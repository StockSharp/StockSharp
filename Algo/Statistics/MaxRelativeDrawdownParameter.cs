namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum relative equity drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RelativeDrawdownKey,
	Description = LocalizedStrings.MaxRelativeDrawdownKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 7
)]
public class MaxRelativeDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxRelativeDrawdownParameter"/>.
	/// </summary>
	public MaxRelativeDrawdownParameter()
		: base(StatisticParameterTypes.MaxRelativeDrawdown)
	{
	}

	private decimal _maxEquity = decimal.MinValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_maxEquity = decimal.MinValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		_maxEquity = Math.Max(_maxEquity, pnl);

		var drawdown = _maxEquity - pnl;
		Value = Math.Max(Value, _maxEquity != 0 ? drawdown / _maxEquity : 0);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MaxEquity", _maxEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_maxEquity = storage.GetValue<decimal>("MaxEquity");
		base.Load(storage);
	}
}
