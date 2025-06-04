namespace StockSharp.BusinessEntities;

/// <summary>
/// The main interface providing the connection to the trading systems.
/// </summary>
public interface IConnector : IMessageChannel, IPersistable, ILogReceiver,
	IMarketDataProvider, ITransactionProvider, ISecurityProvider,
	ISubscriptionProvider, ITimeProvider,
	IPortfolioProvider, IPositionProvider
{
	/// <summary>
	/// Message processed <see cref="Message"/>.
	/// </summary>
	event Action<Message> NewMessage;

	/// <summary>
	/// Connected.
	/// </summary>
	event Action Connected;

	/// <summary>
	/// Disconnected.
	/// </summary>
	event Action Disconnected;

	/// <summary>
	/// Connection error (for example, the connection was aborted by server).
	/// </summary>
	event Action<Exception> ConnectionError;

	/// <summary>
	/// Connected.
	/// </summary>
	event Action<IMessageAdapter> ConnectedEx;

	/// <summary>
	/// Disconnected.
	/// </summary>
	event Action<IMessageAdapter> DisconnectedEx;

	/// <summary>
	/// Connection error (for example, the connection was aborted by server).
	/// </summary>
	event Action<IMessageAdapter, Exception> ConnectionErrorEx;

	/// <summary>
	/// Connection lost.
	/// </summary>
	event Action<IMessageAdapter> ConnectionLost;

	/// <summary>
	/// Connection restored.
	/// </summary>
	event Action<IMessageAdapter> ConnectionRestored;

	/// <summary>
	/// Data process error.
	/// </summary>
	event Action<Exception> Error;

	/// <summary>
	/// Change password result.
	/// </summary>
	event Action<long, Exception> ChangePasswordResult;

	/// <summary>
	/// Server time changed <see cref="ILogSource.CurrentTime"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
	/// </summary>
	[Obsolete("Use ITimeProvider.CurrentTimeChanged event.")]
	event Action<TimeSpan> MarketTimeChanged;

	/// <summary>
	/// List of all exchange boards, for which instruments are loaded <see cref="Securities"/>.
	/// </summary>
	IEnumerable<ExchangeBoard> ExchangeBoards { get; }

	/// <summary>
	/// List of all loaded instruments. It should be called after event <see cref="ISubscriptionProvider.SecurityReceived"/> arisen. Otherwise the empty set will be returned.
	/// </summary>
	IEnumerable<Security> Securities { get; }

	/// <summary>
	/// Determines this connector is ready for establish connection.
	/// </summary>
	bool CanConnect { get; }

	/// <summary>
	/// Connection state.
	/// </summary>
	ConnectionStates ConnectionState { get; }

	/// <summary>
	/// Transactional adapter.
	/// </summary>
	IMessageAdapter TransactionAdapter { get; }

	/// <summary>
	/// Market-data adapter.
	/// </summary>
	IMessageAdapter MarketDataAdapter { get; }

	/// <summary>
	/// Connect to trading system.
	/// </summary>
	void Connect();

	/// <summary>
	/// Disconnect from trading system.
	/// </summary>
	void Disconnect();

	/// <summary>
	/// Get <see cref="SecurityId"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns>Security ID.</returns>
	SecurityId GetSecurityId(Security security);

	/// <summary>
	/// Get security by identifier.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Security.</returns>
	Security GetSecurity(SecurityId securityId);

	/// <summary>
	/// Send outgoing message.
	/// </summary>
	/// <param name="message">Message.</param>
	void SendOutMessage(Message message);
}