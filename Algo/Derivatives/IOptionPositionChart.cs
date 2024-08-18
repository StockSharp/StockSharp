namespace StockSharp.Algo.Derivatives;

using StockSharp.Charting;

/// <summary>
/// The chart showing the position and options Greeks regarding to the underlying asset.
/// </summary>
public interface IOptionPositionChart : IThemeableChart
{
	/// <summary>
	/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
	/// </summary>
	BasketBlackScholes Model { get; set; }

	/// <summary>
	/// To redraw the chart.
	/// </summary>
	/// <param name="assetPrice">The current price of the underlying asset.</param>
	/// <param name="currentTime">The current time.</param>
	/// <param name="expiryDate">The expiration date.</param>
	public void Refresh(decimal? assetPrice = default, DateTimeOffset? currentTime = default, DateTimeOffset? expiryDate = default);
}