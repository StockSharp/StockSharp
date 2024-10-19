namespace StockSharp.Charting;

/// <summary>
/// The chart element representing the indicator.
/// </summary>
public interface IChartIndicatorElement : IChartElement
{
	/// <summary>
	/// The indicator renderer.
	/// </summary>
	IChartIndicatorPainter IndicatorPainter { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.Color"/>.</summary>
	Color Color { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.AdditionalColor"/>.</summary>
	Color AdditionalColor { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.StrokeThickness"/>.</summary>
	int StrokeThickness { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.AntiAliasing"/>.</summary>
	bool AntiAliasing { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.Style"/>.</summary>
	DrawStyles DrawStyle { get; set; }

	/// <summary>Compatibility property for <see cref="IChartLineElement.ShowAxisMarker"/>.</summary>
	bool ShowAxisMarker { get; set; }

	/// <summary>
	/// Auto assign Y axis when the element is being added to chart.
	/// </summary>
	bool AutoAssignYAxis { get; set; }
}
