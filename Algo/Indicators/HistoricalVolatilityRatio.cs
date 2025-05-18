namespace StockSharp.Algo.Indicators;

/// <summary>
/// Historical Volatility Ratio indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HVRKey,
	Description = LocalizedStrings.HistoricalVolatilityRatioKey)]
[Doc("topics/api/indicators/list_of_indicators/historical_volatility_ratio.html")]
public class HistoricalVolatilityRatio : BaseIndicator
{
	private readonly StandardDeviation _shortSd;
	private readonly StandardDeviation _longSd;

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoricalVolatilityRatio"/>.
	/// </summary>
	public HistoricalVolatilityRatio()
	{
		_shortSd = new() { Length = 5 };
		_longSd = new() { Length = 20 };
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _shortSd.NumValuesToInitialize.Max(_longSd.NumValuesToInitialize);

	/// <summary>
	/// Short period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortKey,
		Description = LocalizedStrings.ShortPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortPeriod
	{
		get => _shortSd.Length;
		set
		{
			_shortSd.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Long period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongKey,
		Description = LocalizedStrings.LongPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => _longSd.Length;
		set
		{
			_longSd.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _shortSd.IsFormed && _longSd.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var shortSdValue = _shortSd.Process(input);
		var longSdValue = _longSd.Process(input);

		if (_shortSd.IsFormed && _longSd.IsFormed)
		{
			var shortSd = shortSdValue.ToDecimal();
			var longSd = longSdValue.ToDecimal();
			var result = longSd != 0 ? shortSd / longSd : 0;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_shortSd.Reset();
		_longSd.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(ShortPeriod), ShortPeriod);
		storage.SetValue(nameof(LongPeriod), LongPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		ShortPeriod = storage.GetValue<int>(nameof(ShortPeriod));
		LongPeriod = storage.GetValue<int>(nameof(LongPeriod));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={LongPeriod} S={ShortPeriod}";
}