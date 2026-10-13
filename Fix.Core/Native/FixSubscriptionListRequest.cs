namespace StockSharp.Fix.Native;

/// <summary>
/// Data for SubscriptionListRequest message.
/// </summary>
/// <param name="MdReqId">Request identifier.</param>
/// <param name="SubscriptionRequestType">Subscription request type.</param>
public record struct FixSubscriptionListRequest(
	FixId MdReqId,
	char? SubscriptionRequestType);
