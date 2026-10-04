namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum absolute drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownKey,
	Description = LocalizedStrings.MaxDrawdownDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 4
)]
public class MaxDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxDrawdownParameter"/>.
	/// </summary>
	public MaxDrawdownParameter()
		: base(StatisticParameterTypes.MaxDrawdown)
	{
	}

	internal decimal MaxEquity;

	/// <inheritdoc />
	public override void Reset()
	{
		MaxEquity = 0m;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		// baseline cannot be below zero to properly account first negative pnl as drawdown from zero
		MaxEquity = MaxEquity.Max(pnl);
		Value = Value.Max(MaxEquity - pnl);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("MaxEquity", MaxEquity);
		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		MaxEquity = storage.GetValue<decimal>("MaxEquity");
		await base.LoadAsync(storage, cancellationToken);
	}
}
