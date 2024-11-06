namespace StockSharp.Algo.Indicators;

/// <summary>
/// Provider <see cref="IndicatorType"/>.
/// </summary>
public interface IIndicatorProvider
{
	/// <summary>
	/// Initialize provider.
	/// </summary>
	void Init();

	/// <summary>
	/// All indicator types.
	/// </summary>
	IEnumerable<IndicatorType> All { get; }

	/// <summary>
	/// Add <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	void Add(IndicatorType type);

	/// <summary>
	/// Remove <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	void Remove(IndicatorType type);
}