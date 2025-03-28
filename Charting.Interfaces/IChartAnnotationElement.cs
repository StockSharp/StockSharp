namespace StockSharp.Charting;

/// <summary>
/// Annotation.
/// </summary>
public interface IChartAnnotationElement : IChartElement
{
	/// <summary>
	/// Annotation type.
	/// </summary>
	public ChartAnnotationTypes Type { get; set; }
}