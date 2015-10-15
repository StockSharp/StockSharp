namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface of indicator, built as combination of several indicators.
	/// </summary>
	public interface IComplexIndicator : IIndicator
	{
		/// <summary>
		/// Embedded indicators.
		/// </summary>
		IEnumerable<IIndicator> InnerIndicators { get; }
	}
}