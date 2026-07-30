namespace StockSharp.Fix.Native;

/// <summary>
/// Data from SecurityStatusRequest FIX message.
/// </summary>
/// <param name="SecurityStatusReqId">Security status request identifier.</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="SecurityType">Security type.</param>
public record struct FixSecurityStatusRequest(
	FixId SecurityStatusReqId,
	char? SubscriptionRequestType,
	string Symbol,
	string SecurityExchange,
	string SecurityType);
