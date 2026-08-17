namespace StockSharp.MatchingEngine;

/// <summary>
/// Converts between a trailing stop's market-price watermark and stop price.
/// </summary>
public static class TrailingStopPriceCalculator
{
	/// <summary>
	/// Whether a percentage trailing offset can produce a stop price the market is able to reach.
	/// </summary>
	/// <param name="offset">Trailing distance, as a percentage of the watermark.</param>
	/// <param name="side">Stop order side.</param>
	/// <returns><see langword="true"/> when the offset yields a reachable stop price.</returns>
	/// <remarks>
	/// A sell trails below its watermark, so at 100 percent its stop is zero and beyond that negative.
	/// </remarks>
	public static bool IsPercentOffsetReachable(decimal offset, Sides side)
		=> offset > 0m && (side != Sides.Sell || offset < 100m);

	/// <summary>
	/// The watermark a trailing stop starts from, before it has seen any price.
	/// </summary>
	/// <remarks>
	/// Taking the one its level implies keeps the first price seen from walking the stop away from it.
	/// </remarks>
	/// <param name="stopPrice">Level the stop was registered at.</param>
	/// <param name="offset">Trailing distance. A <see langword="null"/> value is treated as zero.</param>
	/// <param name="isPercent">Whether <paramref name="offset"/> is a percentage of the watermark.</param>
	/// <param name="side">Stop order side.</param>
	/// <returns>The implied watermark, or <see langword="null"/> when the level implies none.</returns>
	public static decimal? InitialWatermark(decimal stopPrice, decimal? offset, bool isPercent, Sides side)
		=> stopPrice > 0m ? ReconstructWatermark(stopPrice, offset, isPercent, side) : null;

	/// <summary>
	/// Calculates the stop price for a market-price watermark.
	/// </summary>
	/// <param name="watermark">Best market price seen by the trailing stop.</param>
	/// <param name="offset">Trailing distance. A <see langword="null"/> value is treated as zero.</param>
	/// <param name="isPercent">Whether <paramref name="offset"/> is a percentage of the watermark.</param>
	/// <param name="side">Stop order side.</param>
	/// <returns>The stop price corresponding to the watermark.</returns>
	public static decimal CalculateStopPrice(decimal watermark, decimal? offset, bool isPercent, Sides side)
	{
		var value = offset ?? 0m;

		if (side == Sides.Sell)
			return isPercent ? watermark * (1m - value / 100m) : watermark - value;

		return isPercent ? watermark * (1m + value / 100m) : watermark + value;
	}

	/// <summary>
	/// Reconstructs the market-price watermark from a persisted stop price.
	/// </summary>
	/// <param name="stopPrice">Persisted stop price.</param>
	/// <param name="offset">Trailing distance. A <see langword="null"/> value is treated as zero.</param>
	/// <param name="isPercent">Whether <paramref name="offset"/> is a percentage of the watermark.</param>
	/// <param name="side">Stop order side.</param>
	/// <returns>The reconstructed watermark.</returns>
	/// <remarks>
	/// At a 100% offset for a sell the forward map sends every watermark to zero and has no inverse,
	/// so the level is returned unchanged.
	/// </remarks>
	public static decimal ReconstructWatermark(decimal stopPrice, decimal? offset, bool isPercent, Sides side)
	{
		var value = offset ?? 0m;

		if (isPercent)
		{
			var factor = side == Sides.Sell ? 1m - value / 100m : 1m + value / 100m;
			return factor == 0m ? stopPrice : stopPrice / factor;
		}

		return side == Sides.Sell ? stopPrice + value : stopPrice - value;
	}
}
