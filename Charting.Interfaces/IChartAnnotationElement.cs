namespace StockSharp.Charting;

/// <summary>
/// Annotation.
/// </summary>
public interface IChartAnnotationElement : IChartElement
{
	/// <summary>
	/// Annotation type.
	/// </summary>
	ChartAnnotationTypes Type { get; set; }
}