namespace StockSharp.Charting
{
	/// <summary>
	/// Annotation.
	/// </summary>
	public interface IChartAnnotation : IChartElement
	{
		/// <summary>
		/// Annotation type.
		/// </summary>
		public ChartAnnotationTypes Type { get; set; }
	}
}