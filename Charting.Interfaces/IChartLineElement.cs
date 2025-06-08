namespace StockSharp.Charting;

/// <summary>
/// The chart element representing a line.
/// </summary>
public interface IChartLineElement : IChartElement
{
	/// <summary>
	/// Line color (candles, etc.), with which it will be drawn on chart.
	/// </summary>
	Color Color { get; set; }

	/// <summary>
	/// Additional line color (candles, etc.), with which it will be drawn on the chart.
	/// </summary>
	Color AdditionalColor { get; set; }

	/// <summary>
	/// The thickness of the line (candle, etc.) with which it will be drawn on the chart. The default is 1.
	/// </summary>
	int StrokeThickness { get; set; }

	/// <summary>
	/// The smoothing of the line drawing. The default is enabled.
	/// </summary>
	bool AntiAliasing { get; set; }

	/// <summary>
	/// The line drawing style. The default is <see cref="DrawStyles.Line"/>.
	/// </summary>
	DrawStyles Style { get; set; }

	/// <summary>
	/// Show Y-axis marker.
	/// </summary>
	bool ShowAxisMarker { get; set; }
}