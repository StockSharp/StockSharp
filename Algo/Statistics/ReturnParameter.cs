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
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		_minEquity = _minEquity.Min(pnl);

		var profit = pnl - _minEquity;
		var denom = _minEquity.Abs();
		Value = Value.Max(denom != 0 ? profit / denom : 0);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("MinEquity", _minEquity);
		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_minEquity = storage.GetValue<decimal>("MinEquity");
		await base.LoadAsync(storage, cancellationToken);
	}
}
