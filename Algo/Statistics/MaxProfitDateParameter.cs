namespace StockSharp.Algo.Statistics;

/// <summary>
/// Date of maximum profit value for the entire period.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxProfitDateParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxProfitParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxProfitDateKey,
	Description = LocalizedStrings.MaxProfitDateDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 3
)]
public class MaxProfitDateParameter(MaxProfitParameter underlying) : BasePnLStatisticParameter<DateTimeOffset>(StatisticParameterTypes.MaxProfitDate)
{
	private readonly MaxProfitParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	private decimal _prevValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevValue = default;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_prevValue < _underlying.Value)
		{
			_prevValue = _underlying.Value;
			Value = marketTime;
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("PrevValue", _prevValue);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_prevValue = storage.GetValue<decimal>("PrevValue");
		base.Load(storage);
	}
}
