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
public class MaxProfitDateParameter(MaxProfitParameter underlying) : BasePnLStatisticParameter<DateTime>(StatisticParameterTypes.MaxProfitDate)
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
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		if (_prevValue < _underlying.Value)
		{
			_prevValue = _underlying.Value;
			Value = marketTime;
		}
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("PrevValue", _prevValue);
		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_prevValue = storage.GetValue<decimal>("PrevValue");
		await base.LoadAsync(storage, cancellationToken);
	}
}
