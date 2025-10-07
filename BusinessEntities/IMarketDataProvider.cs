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
	/// Lookup result <see cref="SecurityLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.SecurityReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult;

	/// <summary>
	/// Lookup result <see cref="SecurityLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.SecurityReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2;
}