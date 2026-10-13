namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var parameters = storage.GetValue<SettingsStorage[]>(nameof(Parameters));

		if (parameters is not null)
		{
			// The storage may contain extra parameters that are added later; load only the known ones.
			foreach (var s in parameters)
			{
				if (Parameters.TryGetValue(s.GetValue<string>(nameof(IStrategyParam.Id)), out var param))
					await param.LoadAsync(s, cancellationToken);
			}
		}

		if (storage.ContainsKey(nameof(Name)))
			Name = storage.GetValue<string>(nameof(Name));

		await RiskManager.LoadIfNotNullAsync(storage, nameof(RiskManager), cancellationToken);

		if (!KeepStatistics)
			return;

		await PnLManager.LoadIfNotNullAsync(storage, nameof(PnLManager), cancellationToken);
		await StatisticManager.LoadIfNotNullAsync(storage, nameof(StatisticManager), cancellationToken);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public override Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
		=> SaveAsync(storage, KeepStatistics, true, cancellationToken);

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="saveStatistics"><see cref="KeepStatistics"/></param>
	/// <param name="saveSystemParameters">Save system parameters.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use SaveAsync instead.")]
	public void Save(SettingsStorage storage, bool saveStatistics, bool saveSystemParameters)
		=> AsyncHelper.Run(() => SaveAsync(storage, saveStatistics, saveSystemParameters, default).AsValueTask());

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="saveStatistics"><see cref="KeepStatistics"/></param>
	/// <param name="saveSystemParameters">Save system parameters.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task SaveAsync(SettingsStorage storage, bool saveStatistics, bool saveSystemParameters, CancellationToken cancellationToken)
	{
		var parameters = GetParameters();

		if (!saveSystemParameters)
			parameters = [.. parameters.Except(_systemParams)];

		storage
			.Set(nameof(Parameters), await parameters.SaveAllAsync(cancellationToken))
			.Set(nameof(RiskManager), await RiskManager.SaveAsync(cancellationToken))
		;

		// Only a name given by hand is the caller's to keep. A generated one is written down by
		// nobody: restoring it would fix the strategy under the instrument it happened to hold.
		if (!NameGenerator.AutoGenerateStrategyName)
			storage.Set(nameof(Name), Name);

		if (saveStatistics)
		{
			storage
				.Set(nameof(PnLManager), await PnLManager.SaveAsync(cancellationToken))
				.Set(nameof(StatisticManager), await StatisticManager.SaveAsync(cancellationToken))
			;
		}
	}
}
