namespace StockSharp.Tinkoff;

using Ecng.ComponentModel;
using Ecng.Net;

using Nito.AsyncEx;

public partial class TinkoffMessageAdapter
{
	private GrpcChannel _channel;
	private InvestApiClient _service;
	private AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> _mdStream;
	private const string _domainAddr = "invest-public-api.tinkoff.ru";

	private static readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan _maxDelay = TimeSpan.FromMinutes(5);

	private static TimeSpan GetCurrentDelay(TimeSpan currentDelay)
		=> currentDelay.Multiply(2).Min(_maxDelay);

	private readonly AsyncLock _lock = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="TinkoffMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public TinkoffMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);

		MaxParallelMessages = 1;

		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsReplaceCommandEditCurrent => true;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <summary>
	/// All possible time frames.
	/// </summary>
	public static IEnumerable<TimeSpan> AllTimeFrames => TinkoffExtensions.TimeFrames;

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription) => true;

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
			metadata.Add(HttpHeaders.Authorization, AuthSchemas.Bearer.FormatAuth(Token));
			metadata.Add("x-app-name", nameof(StockSharp));
			return Task.CompletedTask;
		}));

		var prefix = IsDemo ? "sandbox-" : string.Empty;
		_channel = GrpcChannel.ForAddress($"https://{prefix}{_domainAddr}", new()
		{
			Credentials = credentials,
			MaxReceiveMessageSize = null,
			MaxRetryBufferPerCallSize = null,
			MaxRetryBufferSize = null,
			MaxRetryAttempts = null,
			MaxSendMessageSize = null,
		});
		_service = new(_channel.CreateCallInvoker());

		// validate access
		await _service.Users.GetUserTariffAsync(cancellationToken);

		_mdStream = _service.MarketDataStream.MarketDataStream(cancellationToken: cancellationToken);

		if (this.IsMarketData())
			StartMarketDataStreaming(cancellationToken);

		_historyClient = new();
		_historyClient.SetBearer(Token);

		SendOutMessage(new ConnectMessage());

		async Task monitorConnection()
		{
			await Task.Yield();

			var currState = ConnectivityState.Idle;
			var isInFailure = false;

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					if (currState != _channel.State)
					{
						currState = _channel.State;

						switch (currState)
						{
							//case ConnectivityState.Idle:
							//case ConnectivityState.Connecting:
							//case ConnectivityState.Shutdown:
							//	break;

							case ConnectivityState.Ready:
								if (isInFailure)
								{
									isInFailure = false;
									SendOutConnectionState(ConnectionStates.Restored);
								}

								break;
							case ConnectivityState.TransientFailure:
								if (!isInFailure)
								{
									isInFailure = true;
									SendOutConnectionState(ConnectionStates.Reconnecting);
								}
								
								break;
						}
					}

					await _baseDelay.Delay(cancellationToken);
				}
				catch (Exception ex)
				{
					if (!cancellationToken.IsCancellationRequested)
						LogError(ex);
				}
			}
		}

		_ = monitorConnection();
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
		try
		{
			_mdTransIds.Clear();
			_mdSubs.Clear();

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

			_orderUids.Clear();

			_historyClient?.Dispose();
			_historyClient = null;

			SendOutMessage(new ResetMessage());
		}
		catch (Exception ex)
		{
			SendOutError(ex);
		}

		return default;
	}
}