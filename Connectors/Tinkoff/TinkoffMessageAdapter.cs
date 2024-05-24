namespace StockSharp.Tinkoff;

public partial class TinkoffMessageAdapter
{
	private GrpcChannel _channel;
	private InvestApiClient _service;
	private AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> _mdStream;

	/// <summary>
	/// Initializes a new instance of the <see cref="TinkoffMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public TinkoffMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);

		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

		this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
		this.AddSupportedResultMessage(MessageTypes.OrderStatus);
	}

	/// <inheritdoc />
	public override bool IsAutoReplyOnTransactonalUnsubscription => false;

	/// <inheritdoc />
	public override bool IsReplaceCommandEditCurrent => true;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	protected override IEnumerable<TimeSpan> TimeFrames => TinkoffExtensions.TimeFrames;

	/// <inheritdoc />
	public override bool IsNativeIdentifiers => true;

	/// <inheritdoc />
	public override bool IsNativeIdentifiersPersistable => true;

	/// <inheritdoc />
	public override string StorageName => nameof(Tinkoff);

	/// <inheritdoc />
	public override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Token.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.TokenNotSpecified);
		}

		if (_channel != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		var credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor((_, metadata) =>
		{
			metadata.Add("Authorization", $"Bearer {Token.UnSecure()}");
			metadata.Add("x-app-name", nameof(StockSharp));
			return Task.CompletedTask;
		}));

		var prefix = IsDemo ? "sandbox-" : string.Empty;
		_channel = GrpcChannel.ForAddress($"https://{prefix}invest-public-api.tinkoff.ru:443", new()
		{
			Credentials = credentials,
			MaxReceiveMessageSize = null,
			MaxRetryBufferPerCallSize = null,
			MaxRetryBufferSize = null,
			MaxRetryAttempts = null,
		});
		_service = new(_channel.CreateCallInvoker());

		// validate access
		await _service.Users.GetUserTariffAsync(cancellationToken);

		_mdStream = _service.MarketDataStream.MarketDataStream(cancellationToken: cancellationToken);

		if (this.IsMarketData())
			StartMarketDataStreaming(cancellationToken);

		SendOutMessage(new ConnectMessage());
	}

	/// <inheritdoc />
	public override ValueTask DisconnectAsync(DisconnectMessage msg, CancellationToken cancellationToken)
	{
		if (_channel is null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_channel.Dispose();
		_channel = null;

		SendOutMessage(new DisconnectMessage());

		return default;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage msg, CancellationToken cancellationToken)
	{
		_mdTransIds.Clear();

		if (_channel != null)
		{
			try
			{
				_channel.Dispose();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			_channel = null;
		}

		static void reset(SynchronizedDictionary<long, CancellationTokenSource> dict)
		{
			foreach (var (_, cts) in dict.CopyAndClear())
				cts.Cancel();
		}

		reset(_ordersCts);
		reset(_pfCts);

		_service = default;
		_accountIds.Clear();

		SendOutMessage(new ResetMessage());

		return default;
	}
}