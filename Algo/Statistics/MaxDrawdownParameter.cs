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

	internal decimal MaxEquity = decimal.MinValue;

	/// <inheritdoc />
	public override void Reset()
	{
		MaxEquity = decimal.MinValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
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
