namespace StockSharp.BitStamp;

[OrderCondition(typeof(BitStampOrderCondition))]
public partial class BitStampMessageAdapter : AsyncMessageAdapter
{
	private long _lastMyTradeId;
	private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new();
	
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private DateTimeOffset? _lastTimeBalanceCheck;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BitStampMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public BitStampMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = DefaultHeartbeatInterval;

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		//this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.OrderLog);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override string[] AssociatedBoards { get; } = new[] { BoardCodes.BitStamp };

	/// <summary>
	/// Possible time-frames.
	/// </summary>
	public static IEnumerable<TimeSpan> AllTimeFrames { get; } = new[]
	{
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(3),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(2),
		TimeSpan.FromHours(4),
		TimeSpan.FromHours(6),
		TimeSpan.FromHours(12),
		TimeSpan.FromDays(1),
		TimeSpan.FromDays(3),
	};

	private void SubscribePusherClient()
	{
		_pusherClient.StateChanged += SendOutConnectionState;
		_pusherClient.Error += SendOutError;
		_pusherClient.NewOrderBook += SessionOnNewOrderBook;
		_pusherClient.NewOrderLog += SessionOnNewOrderLog;
		_pusherClient.NewTrade += SessionOnNewTrade;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.StateChanged -= SendOutConnectionState;
		_pusherClient.Error -= SendOutError;
		_pusherClient.NewOrderBook -= SessionOnNewOrderBook;
		_pusherClient.NewOrderLog -= SessionOnNewOrderLog;
		_pusherClient.NewTrade -= SessionOnNewTrade;
	}

	/// <inheritdoc />
	public override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		}

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_httpClient = new(Key, Secret) { Parent = this };
		_pusherClient = new(ReConnectionSettings.ReAttemptCount) { Parent = this };

		SubscribePusherClient();

		await _pusherClient.Connect(cancellationToken);
	}

	/// <inheritdoc />
	public override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_httpClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_pusherClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_httpClient.Dispose();
		_httpClient = null;

		_pusherClient.Disconnect();

		return default;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		_lastMyTradeId = 0;
		_lastTimeBalanceCheck = null;

		if (_httpClient != null)
		{
			try
			{
				_httpClient.Dispose();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			_httpClient = null;
		}

		if (_pusherClient != null)
		{
			try
			{
				UnsubscribePusherClient();
				_pusherClient.Disconnect();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			_pusherClient = null;
		}

		SendOutMessage(new ResetMessage());

		return default;
	}

	/// <inheritdoc />
	public override async ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		if (_orderInfo.Count > 0)
		{
			await OrderStatusAsync(null, cancellationToken);
			await PortfolioLookupAsync(null, cancellationToken);
		}
		else if (BalanceCheckInterval > TimeSpan.Zero &&
			(_lastTimeBalanceCheck == null || (CurrentTime - _lastTimeBalanceCheck) > BalanceCheckInterval))
		{
			await PortfolioLookupAsync(null, cancellationToken);
		}

		if (_pusherClient is not null)
			await _pusherClient.ProcessPing(cancellationToken);
	}
}