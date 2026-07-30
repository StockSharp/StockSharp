namespace StockSharp.Fix.Native;

/// <summary>
/// Data from TradingSessionStatusRequest FIX message.
/// </summary>
/// <param name="SessionId">Trading session identifier.</param>
/// <param name="TradSesReqId">Trading session request identifier.</param>
/// <param name="ResponseId">Response destination identifier.</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
public record struct FixTradingSessionStatusRequest(
	string SessionId,
	FixId TradSesReqId,
	FixId ResponseId,
	char? SubscriptionRequestType);
