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
[IndicatorOut(typeof(IAdaptivePriceZoneValue))]
public class AdaptivePriceZone : BaseComplexIndicator<IAdaptivePriceZoneValue>
{
	private readonly StandardDeviation _stdDev;

	/// <summary>
	/// Moving average.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> MovingAverage { get; }

	/// <summary>
	/// Upper band.
	/// </summary>
	[Browsable(false)]
	public AdaptivePriceZoneBand UpperBand { get; }

	/// <summary>
	/// Lower band.
	/// </summary>
	[Browsable(false)]
	public AdaptivePriceZoneBand LowerBand { get; }

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
		MovingAverage = ma ?? throw new ArgumentNullException(nameof(ma));
		_stdDev = new();

		UpperBand = new();
		LowerBand = new();

		AddInner(MovingAverage);
		AddInner(UpperBand);
		AddInner(LowerBand);

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
		get => MovingAverage.Length;
		set
		{
			MovingAverage.Length = value;
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
		var maValue = MovingAverage.Process(input);
		var stdDevValue = _stdDev.Process(input);

		var result = new AdaptivePriceZoneValue(this, input.Time);

		if (MovingAverage.IsFormed && _stdDev.IsFormed)
		{
			var ma = maValue.ToDecimal(Source);
			var stdDev = stdDevValue.ToDecimal(Source);

			var upperBand = ma + BandPercentage * stdDev;
			var lowerBand = ma - BandPercentage * stdDev;

			result.Add(MovingAverage, new DecimalIndicatorValue(this, ma, input.Time) { IsFinal = input.IsFinal });
			result.Add(UpperBand, UpperBand.Process(upperBand, input.Time, input.IsFinal));
			result.Add(LowerBand, LowerBand.Process(lowerBand, input.Time, input.IsFinal));
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

	/// <inheritdoc />
	protected override IAdaptivePriceZoneValue CreateValue(DateTimeOffset time)
		=> new AdaptivePriceZoneValue(this, time);
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

/// <summary>
/// <see cref="AdaptivePriceZone"/> indicator value.
/// </summary>
public interface IAdaptivePriceZoneValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the moving average value.
	/// </summary>
	IIndicatorValue MovingAverageValue { get; }

	/// <summary>
	/// Gets the upper band value.
	/// </summary>
	IIndicatorValue UpperBandValue { get; }

	/// <summary>
	/// Gets the lower band value.
	/// </summary>
	IIndicatorValue LowerBandValue { get; }

	/// <summary>
	/// Gets the moving average value.
	/// </summary>
	[Browsable(false)]
	decimal? MovingAverage { get; }

	/// <summary>
	/// Gets the upper band value.
	/// </summary>
	[Browsable(false)]
	decimal? UpperBand { get; }

	/// <summary>
	/// Gets the lower band value.
	/// </summary>
	[Browsable(false)]
	decimal? LowerBand { get; }
}

class AdaptivePriceZoneValue(AdaptivePriceZone indicator, DateTimeOffset time) : ComplexIndicatorValue<AdaptivePriceZone>(indicator, time), IAdaptivePriceZoneValue
{
	public IIndicatorValue MovingAverageValue => this[TypedIndicator.MovingAverage];
	public IIndicatorValue UpperBandValue => this[TypedIndicator.UpperBand];
	public IIndicatorValue LowerBandValue => this[TypedIndicator.LowerBand];

	public decimal? MovingAverage => MovingAverageValue.ToNullableDecimal(TypedIndicator.Source);
	public decimal? UpperBand => UpperBandValue.ToNullableDecimal(TypedIndicator.Source);
	public decimal? LowerBand => LowerBandValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"MovingAverage={MovingAverage}, UpperBand={UpperBand}, LowerBand={LowerBand}";
}