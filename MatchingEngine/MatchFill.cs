namespace StockSharp.MatchingEngine;

/// <summary>
/// Volume one match took from a single resting order.
/// </summary>
/// <param name="Order">Resting order the volume was taken from.</param>
/// <param name="Volume">Volume taken from <paramref name="Order"/> by this match.</param>
/// <param name="Remaining">Volume left of <paramref name="Order"/> right after this match took its part.</param>
public record MatchFill(EmulatorOrder Order, decimal Volume, decimal Remaining);
