namespace StockSharp.MatchingEngine;

/// <summary>
/// Result of trade processing.
/// </summary>
/// <param name="RealizedPnL">Realized PnL from the trade.</param>
/// <param name="PositionChange">Position change amount.</param>
/// <param name="Position">Updated position info.</param>
public record TradeProcessingResult(decimal RealizedPnL, decimal PositionChange, PositionInfo Position);
