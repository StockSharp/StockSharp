namespace StockSharp.MatchingEngine;

using StockSharp.Messages;

/// <summary>
/// What an open position can be closed at right now.
/// </summary>
public interface IMarkPrices
{
	/// <summary>
	/// The price a position in <paramref name="securityId"/> can be closed at by an order of
	/// <paramref name="closeSide"/>.
	/// </summary>
	/// <param name="securityId">Instrument the position is held in.</param>
	/// <param name="closeSide">Side of the order that would close the position.</param>
	/// <returns>The price, or <see langword="null"/> when the market has not named one.</returns>
	decimal? TryGetClosePrice(SecurityId securityId, Sides closeSide);
}
