namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for Board FIX message.
/// </summary>
/// <param name="TradSesReqId">Trading session request identifier.</param>
/// <param name="BoardMsg">Board message.</param>
public record struct FixBoard(
	string TradSesReqId,
	BoardMessage BoardMsg);
