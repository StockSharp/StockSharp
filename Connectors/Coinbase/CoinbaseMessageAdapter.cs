using System.Security;
using Ecng.Security;

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
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.CandleTimeFrame);

		this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
		this.AddSupportedResultMessage(MessageTypes.OrderStatus);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override bool IsSupportOrderBookIncrements => true;

	/// <inheritdoc />
	protected override IEnumerable<TimeSpan> TimeFrames { get; } = Extensions.TimeFrames.Keys.ToArray();

	private static readonly DataType _tf5min = DataType.TimeFrame(TimeSpan.FromMinutes(5));

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
	public override string[] AssociatedBoards { get; } = new[] { BoardCodes.Coinbase };

	private void SubscribePusherClient()
	{
		_socketClient.Connected += SessionOnSocketConnected;
		_socketClient.Disconnected += SessionOnSocketDisconnected;
		_socketClient.Error += SessionOnSocketError;
		_socketClient.HeartbeatReceived += SessionOnHeartbeatReceived;
		_socketClient.TickerReceived += SessionOnTickerChanged;
		_socketClient.OrderBookReceived += SessionOnOrderBookReceived;
		_socketClient.TradeReceived += SessionOnTradeReceived;
		_socketClient.CandleReceived += SessionOnCandleReceived;
		_socketClient.OrderReceived += SessionOnOrderReceived;
	}

	private void UnsubscribePusherClient()
	{
		_socketClient.Connected -= SessionOnSocketConnected;
		_socketClient.Disconnected -= SessionOnSocketDisconnected;
		_socketClient.Error -= SessionOnSocketError;
		_socketClient.HeartbeatReceived -= SessionOnHeartbeatReceived;
		_socketClient.TickerReceived -= SessionOnTickerChanged;
		_socketClient.OrderBookReceived -= SessionOnOrderBookReceived;
		_socketClient.TradeReceived -= SessionOnTradeReceived;
		_socketClient.CandleReceived -= SessionOnCandleReceived;
		_socketClient.OrderReceived -= SessionOnOrderReceived;
	}

	/// <inheritdoc />
	public override ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		if (_restClient != null)
		{
			try
			{
				_restClient.Dispose();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
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
				SendOutError(ex);
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
				SendOutError(ex);
			}

			_authenticator = null;
		}

		_candlesTransIds.Clear();

		SendOutMessage(new ResetMessage());
		return default;
	}

	/// <inheritdoc />
	public override async ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (Name.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.NameIsNotSpecified);

			if (Token.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Token);
		}

		// Convert Name to SecureString
		SecureString secureName = new SecureString();
		foreach (char c in Name)
		{
			secureName.AppendChar(c);
		}
		secureName.MakeReadOnly();

		_authenticator = new Authenticator(secureName, Token); // Assuming Authenticator expects SecureString for Name

		if (_restClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		if (_socketClient != null)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_restClient = new HttpClient(_authenticator) { Parent = this };

		_socketClient = new SocketClient(_authenticator, ReConnectionSettings.ReAttemptCount) { Parent = this };
		SubscribePusherClient();

		await _socketClient.Connect(cancellationToken);
		//await _pusherClient.SubscribeStatus(cancellationToken);
		SendOutMessage(new ConnectMessage());
	}

	/// <inheritdoc />
	public override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_restClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		if (_socketClient == null)
			throw new InvalidOperationException(LocalizedStrings.ConnectionNotOk);

		_restClient.Dispose();
		_restClient = null;

		//await _pusherClient.UnSubscribeStatus(cancellationToken);
		_socketClient.Disconnect();
		SendOutDisconnectMessage(true);
		return default;
	}

	/// <inheritdoc />
	public override ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		// can send pings to keep web socket alive
		return default;
	}

	private void SessionOnHeartbeatReceived(Heartbeat heartbeat)
	{

	}

	private void SessionOnSocketConnected()
	{
	}

	private void SessionOnSocketError(Exception exception)
	{
		SendOutError(exception);
	}

	private void SessionOnSocketDisconnected(bool expected)
	{
	}
}