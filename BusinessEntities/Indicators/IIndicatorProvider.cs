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

	/// <summary>
	/// Determines whether the <see cref="IndicatorType"/> is custom (user-defined).
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns>Check result.</returns>
	bool IsCustom(IndicatorType type);

	/// <summary>
	/// Save <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns><see cref="SettingsStorage"/></returns>
	SettingsStorage Save(IndicatorType type);

	/// <summary>
	/// Load <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="id"><see cref="IndicatorType.Id"/></param>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <returns><see cref="IndicatorType"/></returns>
	IndicatorType Load(string id, SettingsStorage storage);
}