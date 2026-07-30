namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for TradingSessionStatus FIX message.
/// </summary>
/// <param name="TradSesReqId">Trading session request identifier.</param>
/// <param name="StateMsg">Board state message.</param>
public record struct FixTradingSessionStatus(
	string TradSesReqId,
	BoardStateMessage StateMsg);
