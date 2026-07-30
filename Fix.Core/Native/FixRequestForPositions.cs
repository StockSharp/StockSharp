namespace StockSharp.Fix.Native;

/// <summary>
/// Data from RequestForPositions FIX message.
/// </summary>
/// <param name="PosReqId">Position request identifier.</param>
/// <param name="Account">Account identifier (portfolio name only — ClientCode / BrokerCode now ride in <see cref="Parties"/>).</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
/// <param name="ResponseId">Response destination identifier.</param>
/// <param name="Side">Side filter (1=Buy, 2=Sell).</param>
/// <param name="IsIncremental">Request incremental updates instead of snapshots.</param>
/// <param name="UserId">User identifier for data filtering.</param>
/// <param name="SecurityIds">Specific security identifiers for filtering.</param>
/// <param name="Parties">Party information (ClientCode, BrokerCode).</param>
public record struct FixRequestForPositions(
	FixId PosReqId,
	string Account,
	char? SubscriptionRequestType,
	FixId ResponseId,
	char? Side,
	bool? IsIncremental,
	string UserId,
	SecurityId[] SecurityIds,
	FixParty[] Parties)
{
	/// <summary>
	/// Owning user's numeric id (broker extension, distinct from the legacy string
	/// <see cref="UserId"/> FIX field). The gate stamps it from the authenticated session;
	/// the Router scopes the positions snapshot to the owner by PK instead of by the
	/// login / portfolio name. 0 when unknown (older writer / unauthenticated path).
	/// </summary>
	public long OwnerUserId { get; set; }
}
