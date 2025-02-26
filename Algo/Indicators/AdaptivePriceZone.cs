namespace StockSharp.Algo.Indicators;

/// <summary>
/// Adaptive Price Zone (APZ) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.APZKey,
	Description = LocalizedStrings.AdaptivePriceZoneKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/adaptive_price_zone.html")]
public class AdaptivePriceZone : BaseComplexIndicator
{
	private readonly LengthIndicator<decimal> _ma;
	private readonly StandardDeviation _stdDev;
	private readonly AdaptivePriceZoneBand _upperBand;
	private readonly AdaptivePriceZoneBand _lowerBand;

	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptivePriceZone"/>.
	/// </summary>
	public AdaptivePriceZone()
		: this(new ExponentialMovingAverage())
	{

	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptivePriceZone"/>.
	/// </summary>
	/// <param name="ma">Moving Average.</param>
	public AdaptivePriceZone(LengthIndicator<decimal> ma)
	{
		_ma = ma ?? throw new ArgumentNullException(nameof(ma));
		_stdDev = new();

		_upperBand = new();
		_lowerBand = new();

		AddInner(_ma);
		AddInner(_upperBand);
		AddInner(_lowerBand);

		Period = 5;
		BandPercentage = 2;
	}

	/// <summary>
	/// Period for MA and Standard Deviation calculations.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.PeriodDescriptionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Period
	{
		get => _ma.Length;
		set
		{
			_ma.Length = value;
			_stdDev.Length = value;
		}
	}

	private decimal _bandPercentage;

	/// <summary>
	/// Band percentage for upper and lower bands calculation.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BandKey,
		Description = LocalizedStrings.BandPercentageDescriptionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal BandPercentage
	{
		get => _bandPercentage;
		set
		{
			_bandPercentage = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var maValue = _ma.Process(input);
		var stdDevValue = _stdDev.Process(input);

		var result = new ComplexIndicatorValue(this, input.Time);

		if (_ma.IsFormed && _stdDev.IsFormed)
		{
			var ma = maValue.ToDecimal();
			var stdDev = stdDevValue.ToDecimal();

			var upperBand = ma + BandPercentage * stdDev;
			var lowerBand = ma - BandPercentage * stdDev;

			result.Add(_ma, new DecimalIndicatorValue(this, ma, input.Time));
			result.Add(_upperBand, _upperBand.Process(upperBand, input.Time, input.IsFinal));
			result.Add(_lowerBand, _lowerBand.Process(lowerBand, input.Time, input.IsFinal));
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_stdDev.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Period), Period);
		storage.SetValue(nameof(BandPercentage), BandPercentage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Period = storage.GetValue<int>(nameof(Period));
		BandPercentage = storage.GetValue<decimal>(nameof(BandPercentage));
	}
}

/// <summary>
/// Represents a band (upper or lower) of the Adaptive Price Zone indicator.
/// </summary>
[IndicatorHidden]
public class AdaptivePriceZoneBand : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}