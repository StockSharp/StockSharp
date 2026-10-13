namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for SubscriptionFinished FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="FinishMsg">Subscription finished message.</param>
public record struct FixSubscriptionFinished(
	FixId MdReqId,
	SubscriptionFinishedMessage FinishMsg);
