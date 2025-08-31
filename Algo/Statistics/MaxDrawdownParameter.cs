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
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		// baseline cannot be below zero to properly account first negative pnl as drawdown from zero
		MaxEquity = Math.Max(MaxEquity, pnl);
		Value = Math.Max(Value, MaxEquity - pnl);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MaxEquity", MaxEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		MaxEquity = storage.GetValue<decimal>("MaxEquity");
		base.Load(storage);
	}
}
