namespace StockSharp.Fix.Native;

/// <summary>
/// Data from OrderStatusRequest FIX message.
/// </summary>
/// <param name="OrdStatusReqId">Order status request identifier.</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
/// <param name="OrderId">Our numeric order primary key (null when the order is identified by its exchange id).</param>
/// <param name="OrderStringId">Exchange order identifier (string), when identified by it instead of the PK.</param>
/// <param name="ClOrdId">Client order identifier.</param>
public record struct FixOrderStatusRequest(
	FixId OrdStatusReqId,
	char? SubscriptionRequestType,
	long? OrderId,
	string OrderStringId,
	string ClOrdId)
{
	/// <summary>
	/// Owning user's numeric id (broker extension, not a standard FIX tag). Carried over the
	/// SBE wire so a targeted per-user status request scopes the StopOrder snapshot to that
	/// user (0 = the OrderRouter's mass feed).
	/// </summary>
	public long UserId { get; set; }
}
