namespace StockSharp.Fix.Native;

/// <summary>
/// Data from OrderMassStatusRequest FIX message.
/// </summary>
/// <param name="MassStatusReqId">Mass status request identifier.</param>
/// <param name="OrigClOrdId">Original client order identifier filter.</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
/// <param name="OrdStatus">Order status filter.</param>
/// <param name="Symbol">Security code.</param>
/// <param name="SecurityExchange">Exchange/board code.</param>
/// <param name="Account">Account identifier.</param>
/// <param name="IsIncremental">Request incremental updates instead of snapshots.</param>
/// <param name="UserId">User identifier for data filtering.</param>
/// <param name="SecurityIds">Specific security identifiers for filtering.</param>
/// <param name="Skip">Last-seen upstream sequence number — request only data after this point. Used by IMEX dialect to populate ResendRequest.FromSeq, replacing Forwarder-local file persistence with Router-DB persistence.</param>
public record struct FixOrderMassStatusRequest(
	FixId MassStatusReqId,
	string OrigClOrdId,
	char? SubscriptionRequestType,
	string OrdStatus,
	string Symbol,
	string SecurityExchange,
	string Account,
	bool? IsIncremental,
	string UserId,
	SecurityId[] SecurityIds,
	long? Skip)
{
	/// <summary>
	/// Owning user's numeric id (broker extension, distinct from the string <see cref="UserId"/>
	/// data filter). Carried over the SBE wire so a per-client status request scopes the
	/// StopOrder snapshot to that user (0 = the OrderRouter's trusted mass feed).
	/// </summary>
	public long OwnerUserId { get; set; }
}
