namespace StockSharp.Charting;

/// <summary>
/// The interface that describes the chart element (indicator, candle, etc.).
/// </summary>
public interface IChartElement : IChartPart<IChartElement>
{
	/// <summary>
	/// The full series title.
	/// If this property is undefined, auto-generated title will be used instead.
	/// </summary>
	string FullTitle { get; set; }

	/// <summary>
	/// Whether this element is visible on chart.
	/// </summary>
	bool IsVisible { get; set; }

	/// <summary>
	/// Should this element be shown in the legend.
	/// </summary>
	bool IsLegend { get; set; }

	/// <summary>
	/// X-axis.
	/// </summary>
	string XAxisId { get; set; }

	/// <summary>
	/// Y-axis.
	/// </summary>
	string YAxisId { get; set; }

	/// <summary>
	/// Custom elements colorer.
	/// </summary>
	Func<IComparable, Color?> Colorer { get; set; }

	/// <summary>
	/// The chart area on which the element is drawn.
	/// </summary>
	IChartArea ChartArea { get; }

	/// <summary>
	/// The chart area on which the element is drawn.
	/// </summary>
	IChartArea PersistentChartArea { get; }
}