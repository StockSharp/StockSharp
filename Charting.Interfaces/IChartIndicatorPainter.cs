namespace StockSharp.Charting;

/// <summary>
/// The interface describing the indicator renderer on the chart (for example, lines, histograms, etc.).
/// </summary>
public interface IChartIndicatorPainter : IPersistable
{
	/// <summary>
	/// The chart element representing the indicator.
	/// </summary>
	IChartIndicatorElement Element { get; }

	/// <summary>
	/// Child elements.
	/// </summary>
	IReadOnlyList<IChartElement> InnerElements { get; }

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="data">New data.</param>
	/// <returns><see langword="true"/> if the data was successfully drawn, otherwise, returns <see langword="false"/>.</returns>
	bool Draw(IChartDrawData data);

	/// <summary>
	/// To reset painter child elements.
	/// </summary>
	void Reset();

	/// <summary>
	/// Called when this painter is attached to chart indicator element.
	/// </summary>
	void OnAttached(IChartIndicatorElement element);

	/// <summary>
	/// Called when this painter is detached from chart indicator element.
	/// </summary>
	void OnDetached();
}