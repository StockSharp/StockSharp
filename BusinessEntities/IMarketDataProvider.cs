namespace StockSharp.BusinessEntities;

/// <summary>
/// The market data by the instrument provider interface.
/// </summary>
public interface IMarketDataProvider
{
	/// <summary>
	/// Security changed.
	/// </summary>
	event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

	/// <summary>
	/// To get the value of market data for the instrument.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="field">Market-data field.</param>
	/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
	object GetSecurityValue(Security security, Level1Fields field);

	/// <summary>
	/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns>Possible fields.</returns>
	IEnumerable<Level1Fields> GetLevel1Fields(Security security);

	/// <summary>
	/// Get session state for required board.
	/// </summary>
	/// <param name="board">Electronic board.</param>
	/// <returns>Session state. If the information about session state does not exist, then <see langword="null" /> will be returned.</returns>
	[Obsolete("Use ISubscriptionProvider.BoardReceived event.")]
	SessionStates? GetSessionState(ExchangeBoard board);

	/// <summary>
	/// Lookup result <see cref="SecurityLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.SecurityReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult;

	/// <summary>
	/// Lookup result <see cref="SecurityLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.SecurityReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2;

	/// <summary>
	/// Lookup result <see cref="BoardLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.BoardReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult;

	/// <summary>
	/// Lookup result <see cref="BoardLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.BoardReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult2;

	/// <summary>
	/// Lookup result <see cref="DataTypeLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.DataTypeReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult;

	/// <summary>
	/// Lookup result <see cref="DataTypeLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.DataTypeReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult2;

	/// <summary>
	/// Session changed.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.BoardReceived event.")]	
	event Action<ExchangeBoard, SessionStates> SessionStateChanged;
}