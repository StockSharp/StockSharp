namespace StockSharp.Algo.Indicators;

/// <summary>
/// Awesome Oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/ao.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AOKey,
	Description = LocalizedStrings.AwesomeOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/ao.html")]
public class AwesomeOscillator : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AwesomeOscillator"/>.
	/// </summary>
	public AwesomeOscillator()
		: this(new SimpleMovingAverage { Length = 34 }, new SimpleMovingAverage { Length = 5 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AwesomeOscillator"/>.
	/// </summary>
	/// <param name="longSma">Long moving average.</param>
	/// <param name="shortSma">Short moving average.</param>
	public AwesomeOscillator(SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
	{
		ShortMa = shortSma ?? throw new ArgumentNullException(nameof(shortSma));
		LongMa = longSma ?? throw new ArgumentNullException(nameof(longSma));
		MedianPrice = new();

		AddResetTracking(ShortMa);
		AddResetTracking(LongMa);
		AddResetTracking(MedianPrice);
	}

	/// <summary>
	/// Long moving average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongMaKey,
		Description = LocalizedStrings.LongMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SimpleMovingAverage LongMa { get; }

	/// <summary>
	/// Short moving average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortMaKey,
		Description = LocalizedStrings.ShortMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SimpleMovingAverage ShortMa { get; }

	/// <summary>
	/// Median price.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MedPriceKey,
		Description = LocalizedStrings.MedianPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public MedianPrice MedianPrice { get; }

	/// <inheritdoc />
	public override int NumValuesToInitialize => LongMa.NumValuesToInitialize.Max(ShortMa.NumValuesToInitialize.Max(MedianPrice.NumValuesToInitialize));

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => LongMa.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var mpValue = MedianPrice.Process(input);

		var sValue = ShortMa.Process(mpValue).ToDecimal();
		var lValue = LongMa.Process(mpValue).ToDecimal();

		return new DecimalIndicatorValue(this, sValue - lValue, input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		LongMa.LoadIfNotNull(storage, nameof(LongMa));
		ShortMa.LoadIfNotNull(storage, nameof(ShortMa));
		MedianPrice.LoadIfNotNull(storage, nameof(MedianPrice));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(LongMa), LongMa.Save());
		storage.SetValue(nameof(ShortMa), ShortMa.Save());
		storage.SetValue(nameof(MedianPrice), MedianPrice.Save());
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, L={LongMa}, S={ShortMa}, M={MedianPrice}";
}
