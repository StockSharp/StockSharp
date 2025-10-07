namespace StockSharp.Charting;

/// <summary>
/// Extension class for <see cref="IChart"/>.
/// </summary>
public static class ChartingInterfacesExtensions
{
	/// <summary>
	/// To draw the candle.
	/// </summary>
	/// <param name="chart">Chart.</param>
	/// <param name="element">The chart element representing a candle.</param>
	/// <param name="candle">Candle.</param>
	public static void Draw(this IChart chart, IChartCandleElement element, ICandleMessage candle)
	{
		if (element == null)
			throw new ArgumentNullException(nameof(element));

		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		var data = chart.CreateData();

		data
			.Group(candle.OpenTime)
				.Add(element, candle);

		chart.Draw(data);
	}

	/// <summary>
	/// Check the specified style is volume profile based.
	/// </summary>
	/// <param name="style">Style.</param>
	/// <returns>Check result.</returns>
	public static bool IsVolumeProfileChart(this ChartCandleDrawStyles style)
		=> style == ChartCandleDrawStyles.BoxVolume || style == ChartCandleDrawStyles.ClusterProfile;

	/// <summary>
	/// Create <see cref="IChartArea"/>.
	/// </summary>
	/// <param name="chart"><see cref="IChart"/></param>
	/// <returns><see cref="IChartArea"/></returns>
	public static IChartArea AddArea(this IChart chart)
	{
		if (chart is null)
			throw new ArgumentNullException(nameof(chart));

		var area = chart.CreateArea();
		chart.AddArea(area);
		return area;
	}

	/// <summary>
	/// Create <see cref="IChartCandleElement"/> element.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	public static IChartCandleElement AddCandles(this IChartArea area)
	{
		if (area is null)
			throw new ArgumentNullException(nameof(area));

		var elem = area.Chart.CreateCandleElement();
		area.Chart.AddElement(area, elem);
		return elem;
	}

	/// <summary>
	/// Create <see cref="IChartTradeElement"/> element.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartTradeElement"/></returns>
	public static IChartTradeElement AddTrades(this IChartArea area)
	{
		if (area is null)
			throw new ArgumentNullException(nameof(area));

		var elem = area.Chart.CreateTradeElement();
		area.Chart.AddElement(area, elem);
		return elem;
	}

	/// <summary>
	/// Create <see cref="IChartOrderElement"/> element.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartOrderElement"/></returns>
	public static IChartOrderElement AddOrders(this IChartArea area)
	{
		if (area is null)
			throw new ArgumentNullException(nameof(area));

		var elem = area.Chart.CreateOrderElement();
		area.Chart.AddElement(area, elem);
		return elem;
	}

	/// <summary>
	/// Create <see cref="IChartIndicatorElement"/> element.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <returns><see cref="IChartIndicatorElement"/></returns>
	public static IChartIndicatorElement AddIndicator(this IChartArea area, IIndicator indicator)
	{
		if (area is null)
			throw new ArgumentNullException(nameof(area));

		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		var elem = area.Chart.CreateIndicatorElement();
		elem.FullTitle = indicator.ToString();
		area.Chart.AddElement(area, elem);
		return elem;
	}

	/// <summary>
	/// Create <see cref="IChartIndicatorPainter"/> instance.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns><see cref="IChartIndicatorPainter"/></returns>
	public static IChartIndicatorPainter CreatePainter(this IndicatorType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return IChartExtensions.TryIndicatorPainterProvider?.TryGetPainter(type.Indicator)?.CreateInstance<IChartIndicatorPainter>();
	}
}