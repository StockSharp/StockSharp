namespace StockSharp.Algo.Indicators;

/// <summary>
/// Detrended Synthetic Price indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DSPKey,
	Description = LocalizedStrings.DetrendedSyntheticPriceKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/detrended_synthetic_price.html")]
public class DetrendedSyntheticPrice : BaseIndicator
{
	private readonly Highest _highest;
	private readonly Lowest _lowest;

	/// <summary>
	/// Initializes a new instance of the <see cref="DetrendedSyntheticPrice"/>.
	/// </summary>
	public DetrendedSyntheticPrice()
	{
		_highest = new();
		_lowest = new();

		Length = 14;
	}

	/// <summary>
	/// Length of the indicator.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _highest.Length;
		set
		{
			_highest.Length = value;
			_lowest.Length = value;

			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _highest.NumValuesToInitialize;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var highestValue = _highest.Process(input, candle.HighPrice);
		var lowestValue = _lowest.Process(input, candle.LowPrice);

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			var highestHigh = highestValue.ToDecimal();
			var lowestLow = lowestValue.ToDecimal();
			var dsp = (highestHigh + lowestLow) / 2;
			return new DecimalIndicatorValue(this, dsp, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _highest.IsFormed && _lowest.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_highest.Reset();
		_lowest.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}
