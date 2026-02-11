namespace StockSharp.Coinbase;

using StockSharp.Coinbase.Native;
using StockSharp.Coinbase.Native.Model;

[OrderCondition(typeof(CoinbaseOrderCondition))]
public partial class CoinbaseMessageAdapter
{
	private Authenticator _authenticator;
	private HttpClient _restClient;
	private SocketClient _socketClient;

	/// <summary>
	/// Initializes a new instance of the <see cref="CoinbaseMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public CoinbaseMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = TimeSpan.FromSeconds(5);

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedCandleTimeFrames(AllTimeFrames);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || dataType == DataType.Transactions || dataType == DataType.PositionChanges || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public static IEnumerable<TimeSpan> AllTimeFrames => Extensions.TimeFrames.Keys;

	private static readonly DataType _tf5min = TimeSpan.FromMinutes(5).TimeFrame();

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription)
	{
		// Coinbase supports 5min tf live updates
		// So build from ticks other time-frames (compression will be done internally in S# core)
		return subscription.DataType2 == _tf5min;
	}

	/// <inheritdoc />
	public override bool IsReplaceCommandEditCurrent => true;

	/// <inheritdoc />
	public override string[] AssociatedBoards { get; } = [BoardCodes.Coinbase];

	private void SubscribePusherClient()
	{
		_socketClient.StateChanged += SendOutConnectionStateAsync;
		_socketClient.Error += SendOutErrorAsync;
		_socketClient.HeartbeatReceived += SessionOnHeartbeatReceived;
		_socketClient.TickerReceived += SessionOnTickerChanged;
		_socketClient.OrderBookReceived += SessionOnOrderBookReceived;
		_socketClient.TradeReceived += SessionOnTradeReceived;
		_socketClient.CandleReceived += SessionOnCandleReceived;
		_socketClient.OrderReceived += SessionOnOrderReceived;
	}

	private void UnsubscribePusherClient()
	{
		_socketClient.StateChanged -= SendOutConnectionStateAsync;
		_socketClient.Error -= SendOutErrorAsync;
		_socketClient.HeartbeatReceived -= SessionOnHeartbeatReceived;
		_socketClient.TickerReceived -= SessionOnTickerChanged;
		_socketClient.OrderBookReceived -= SessionOnOrderBookReceived;
		_socketClient.TradeReceived -= SessionOnTradeReceived;
		_socketClient.CandleReceived -= SessionOnCandleReceived;
		_socketClient.OrderReceived -= SessionOnOrderReceived;
	}

	/// <inheritdoc />
	protected override async ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		if (_restClient != null)
		{
			try
			{
				_restClient.Dispose();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_restClient = null;
		}

		if (_socketClient != null)
		{
			try
			{
				UnsubscribePusherClient();
				_socketClient.Disconnect();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_socketClient = null;
		}

		if (_authenticator != null)
		{
			try
			{
				_authenticator.Dispose();
			}
			catch (Exception ex)
			{
				await SendOutErrorAsync(ex, cancellationToken);
			}

			_authenticator = null;
		}

		_candlesTransIds.Clear();

		await SendOutMessageAsync(new ResetMessage(), cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.KeyNotSpecified);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.SecretNotSpecified);
		}

		_authenticator = new(this.IsTransactional(), Key, Secret, Passphrase);

		if (_restClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_socketClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_restClient = new(_authenticator) { Parent = this };

		_socketClient = new(_authenticator, ReConnectionSettings.ReAttemptCount, ReConnectionSettings.WorkingTime) { Parent = this };
		SubscribePusherClient();

		await _socketClient.Connect(cancellationToken);
		//await _socketClient.SubscribeStatus(cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_restClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_socketClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_restClient.Dispose();
		_restClient = null;

		//await _socketClient.UnSubscribeStatus(cancellationToken);
		_socketClient.Disconnect();
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		// can send pings to keep web socket alive
		return default;
	}

	private ValueTask SessionOnHeartbeatReceived(Heartbeat heartbeat, CancellationToken cancellationToken)
	{
		return default;
	}
}
