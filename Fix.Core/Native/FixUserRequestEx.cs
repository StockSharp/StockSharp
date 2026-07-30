namespace StockSharp.Fix.Native;

/// <summary>
/// Data from extended UserRequest FIX message.
/// </summary>
/// <param name="UserRequestId">User request identifier.</param>
/// <param name="Id">Entity identifier.</param>
/// <param name="UserName">User name filter.</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
public record struct FixUserRequestEx(
	FixId UserRequestId,
	long? Id,
	string UserName,
	char? SubscriptionRequestType);
