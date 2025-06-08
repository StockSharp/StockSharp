namespace StockSharp.Charting;

/// <summary>
/// Interface for working with charts.
/// </summary>
public interface IChart : IChartBuilder, IThemeableChart
{
	/// <summary>
	/// Chart areas.
	/// </summary>
	IEnumerable<IChartArea> Areas { get; }

	/// <summary>
	/// <see cref="Areas"/> added event.
	/// </summary>
	event Action<IChartArea> AreaAdded;

	/// <summary>
	/// <see cref="Areas"/> removed event.
	/// </summary>
	event Action<IChartArea> AreaRemoved;

	/// <summary>
	/// To scroll <see cref="Areas"/> areas automatically when new data occurred. The default is enabled.
	/// </summary>
	bool IsAutoScroll { get; set; }

	/// <summary>
	/// To use automatic range for the X-axis. The default is off.
	/// </summary>
	bool IsAutoRange { get; set; }

	/// <summary>
	/// The list of available indicators types.
	/// </summary>
	IList<IndicatorType> IndicatorTypes { get; }

	/// <summary>
	/// Show non formed indicators values.
	/// </summary>
	bool ShowNonFormedIndicators { get; set; }

	/// <summary>
	/// Show FPS.
	/// </summary>
	bool ShowPerfStats { get; set; }

	/// <summary>
	/// To show the legend.
	/// </summary>
	bool ShowLegend { get; set; }

	/// <summary>
	/// To show the preview area.
	/// </summary>
	bool ShowOverview { get; set; }

	/// <summary>
	/// The interactive mode. The default is off.
	/// </summary>
	bool IsInteracted { get; set; }

	/// <summary>
	/// Crosshair.
	/// </summary>
	bool CrossHair { get; set; }

	/// <summary>
	/// To show the prompt message for the crosshair.
	/// </summary>
	bool CrossHairTooltip { get; set; }

	/// <summary>
	/// To show values on the axis for the crosshair.
	/// </summary>
	bool CrossHairAxisLabels { get; set; }

	/// <summary>
	/// The order creation mode. The default is off.
	/// </summary>
	bool OrderCreationMode { get; set; }

	/// <summary>
	/// Local time zone for all <see cref="DateTimeOffset"/> values conversion.
	/// </summary>
	TimeZoneInfo TimeZone { get; set; }

	/// <summary>
	/// To add an area to the chart.
	/// </summary>
	/// <param name="area">Chart area.</param>
	void AddArea(IChartArea area);

	/// <summary>
	/// To remove the area from the chart.
	/// </summary>
	/// <param name="area">Chart area.</param>
	void RemoveArea(IChartArea area);

	/// <summary>
	/// To add an element to the chart.
	/// </summary>
	/// <param name="area">Chart area.</param>
	/// <param name="element">The chart element.</param>
	void AddElement(IChartArea area, IChartElement element);

	/// <summary>
	/// To remove the element from the chart.
	/// </summary>
	/// <param name="area">Chart area.</param>
	/// <param name="element">The chart element.</param>
	void RemoveElement(IChartArea area, IChartElement element);

	/// <summary>
	/// To reset the chart elements values drawn previously.
	/// </summary>
	/// <param name="elements">Chart elements.</param>
	void Reset(IEnumerable<IChartElement> elements);
}