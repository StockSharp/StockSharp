namespace StockSharp.Algo.Statistics;

/// <summary>
/// Date of maximum absolute drawdown during the whole period.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxDrawdownDateParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownDateKey,
	Description = LocalizedStrings.MaxDrawdownDateDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 6
)]
public class MaxDrawdownDateParameter(MaxDrawdownParameter underlying) : BasePnLStatisticParameter<DateTimeOffset>(StatisticParameterTypes.MaxDrawdownDate)
{
	private readonly MaxDrawdownParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
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
