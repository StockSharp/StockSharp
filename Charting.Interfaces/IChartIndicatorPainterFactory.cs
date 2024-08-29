namespace StockSharp.Charting;

/// <summary>
/// <see cref="IChartIndicatorPainter"/> factory.
/// </summary>
public interface IChartIndicatorPainterFactory
{
	/// <summary>
	/// Create a new instance of <see cref="IChartIndicatorPainter"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <returns><see cref="IChartIndicatorPainter"/></returns>
	IChartIndicatorPainter Create(IIndicator indicator);
}