namespace StockSharp.MatchingEngine;

using StockSharp.Messages;

/// <summary>
/// Prices for a portfolio with no market behind it: nothing is quoted, so every position is worth
/// what it cost.
/// </summary>
public class NoMarkPrices : IMarkPrices
{
	private NoMarkPrices()
	{
	}

	/// <summary>The single instance.</summary>
	public static NoMarkPrices Instance { get; } = new();

	/// <inheritdoc />
	public decimal? TryGetClosePrice(SecurityId securityId, Sides closeSide) => null;
}
