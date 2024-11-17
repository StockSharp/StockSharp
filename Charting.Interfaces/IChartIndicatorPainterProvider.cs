namespace StockSharp.Charting;

/// <summary>
/// The interface describing the <see cref="IChartIndicatorPainter"/> provider.
/// </summary>
public interface IChartIndicatorPainterProvider
{
	/// <summary>
	/// Initialize provider.
	/// </summary>
	void Init();

	/// <summary>
	/// Try get <see cref="IChartIndicatorPainter"/>.
	/// </summary>
	/// <param name="type"><see cref="IIndicator"/> type.</param>
	/// <returns><see cref="IChartIndicatorPainter"/></returns>
	Type TryGetPainter(Type type);
}