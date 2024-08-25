namespace StockSharp.DarkHorse;
using StockSharp.Messages;
using StockSharp.Localization;

using StockSharp.Algo.Storages.Csv;
using StockSharp.BusinessEntities;


[OrderCondition(typeof(DarkHorseCondition))]
public partial class DarkHorseMessageAdapter
{
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private DateTimeOffset? _lastTimeBalanceCheck;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="DarkHorseMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public DarkHorseMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = DefaultHeartbeatInterval;

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);

		//this.AddSupportedMarketDataType(DataType.Ticks);
		//this.AddSupportedMarketDataType(DataType.MarketDepth);
		//this.AddSupportedMarketDataType(DataType.Level1);
        //this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

        //this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		//this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
		//this.AddSupportedResultMessage(MessageTypes.OrderStatus);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	public override string[] AssociatedBoards { get; } = new[] { BoardCodes.Btce };

	private void SubscribePusherClient()
	{
		_pusherClient.Connected += SessionOnPusherConnected;
		_pusherClient.Disconnected += SessionOnPusherDisconnected;
		_pusherClient.Error += SessionOnPusherError;
		_pusherClient.OrderBookChanged += SessionOnOrderBookChanged;
		_pusherClient.NewTrades += SessionOnNewTrades;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.Connected -= SessionOnPusherConnected;
		_pusherClient.Disconnected -= SessionOnPusherDisconnected;
		_pusherClient.Error -= SessionOnPusherError;
		_pusherClient.OrderBookChanged -= SessionOnOrderBookChanged;
		_pusherClient.NewTrades -= SessionOnNewTrades;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		_orderBooks.Clear();

		_orderInfo.Clear();
		//_unkOrds.Clear();
		_positions.Clear();

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
	public override ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
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

		_httpClient = new HttpClient(Address, Key, Secret);

		_pusherClient = new PusherClient { Parent = this };
		SubscribePusherClient();
		return _pusherClient.Connect(cancellationToken);
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
	public override async ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		//if (_orderInfo.Count > 0/* || _unkOrds.Count > 0*/)
		//{
		//	await OrderStatusAsync(null, cancellationToken);
		//	await PortfolioLookupAsync(null, cancellationToken);
		//}

		//if (BalanceCheckInterval > TimeSpan.Zero &&
		//	(_lastTimeBalanceCheck == null || (CurrentTime - _lastTimeBalanceCheck) > BalanceCheckInterval))
		//{
		//	await PortfolioLookupAsync(null, cancellationToken);
		//}
	}

	private void SessionOnPusherConnected()
	{
		SendOutMessage(new ConnectMessage());
	}

	private void SessionOnPusherError(Exception exception)
	{
		SendOutError(exception);
	}

	private void SessionOnPusherDisconnected(bool expected)
	{
		SendOutDisconnectMessage(expected);
	}
}