namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		var parameters = storage.GetValue<SettingsStorage[]>(nameof(Parameters));

		if (parameters is not null)
		{
			// The storage may contain extra parameters that are added later; load only the known ones.
			foreach (var s in parameters)
			{
				if (Parameters.TryGetValue(s.GetValue<string>(nameof(IStrategyParam.Id)), out var param))
					param.Load(s);
			}
		}

		RiskManager.LoadIfNotNull(storage, nameof(RiskManager));

		if (!KeepStatistics)
			return;

		PnLManager.LoadIfNotNull(storage, nameof(PnLManager));
		StatisticManager.LoadIfNotNull(storage, nameof(StatisticManager));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		Save(storage, KeepStatistics, true);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="saveStatistics"><see cref="KeepStatistics"/></param>
	/// <param name="saveSystemParameters">Save system parameters.</param>
	public void Save(SettingsStorage storage, bool saveStatistics, bool saveSystemParameters)
	{
		var parameters = GetParameters();

		if (!saveSystemParameters)
			parameters = [.. parameters.Except(_systemParams)];

		storage
			.Set(nameof(Parameters), parameters.Select(p => p.Save()).ToArray())
			.Set(nameof(RiskManager), RiskManager.Save())
		;

		if (saveStatistics)
		{
			storage
				.Set(nameof(PnLManager), PnLManager.Save())
				.Set(nameof(StatisticManager), StatisticManager.Save())
			;
		}
	}
}
