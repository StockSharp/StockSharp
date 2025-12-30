namespace StockSharp.Algo.Indicators;

using StockSharp.BusinessEntities;

/// <summary>
/// Provider <see cref="IndicatorType"/>.
/// </summary>
public interface IIndicatorProvider : ICustomProvider<IndicatorType>
{
	/// <summary>
	/// Initialize provider.
	/// </summary>
	void Init();
}