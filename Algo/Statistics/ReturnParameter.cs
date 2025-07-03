namespace StockSharp.Algo.Statistics;

/// <summary>
/// Relative income for the whole time period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RelativeIncomeKey,
	Description = LocalizedStrings.RelativeIncomeWholePeriodKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 8
)]
public class ReturnParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="ReturnParameter"/>.
	/// </summary>
	public ReturnParameter()
		: base(StatisticParameterTypes.Return)
	{
	}

	private decimal _minEquity = decimal.MaxValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_minEquity = decimal.MaxValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		_minEquity = Math.Min(_minEquity, pnl);

		var profit = pnl - _minEquity;
		var denom = Math.Abs(_minEquity);
		Value = Math.Max(Value, denom != 0 ? profit / denom : 0);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MinEquity", _minEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_minEquity = storage.GetValue<decimal>("MinEquity");
		base.Load(storage);
	}
}
