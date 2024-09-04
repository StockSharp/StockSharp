namespace StockSharp.Algo.Indicators;

/// <summary>
/// The interface of indicator, built as combination of several indicators.
/// </summary>
public interface IComplexIndicator : IIndicator
{
	/// <summary>
	/// Embedded indicators.
	/// </summary>
	IReadOnlyList<IIndicator> InnerIndicators { get; }
}