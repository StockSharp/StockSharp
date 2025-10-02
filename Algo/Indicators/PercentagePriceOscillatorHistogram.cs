namespace StockSharp.Algo.Indicators;

/// <summary>
/// Percentage Price Oscillator with signal line (histogram painter will plot difference).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PPOHKey,
	Description = LocalizedStrings.PercentagePriceOscillatorHistogramKey)]
[Doc("topics/api/indicators/list_of_indicators/percentage_price_oscillator_histogram.html")]
[IndicatorOut(typeof(PercentagePriceOscillatorHistogramValue))]
public class PercentagePriceOscillatorHistogram : BaseComplexIndicator<PercentagePriceOscillatorHistogramValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PercentagePriceOscillatorHistogram"/>.
	/// </summary>
	public PercentagePriceOscillatorHistogram()
		: this(new(), new() { Length = 9 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PercentagePriceOscillatorHistogram"/>.
	/// </summary>
	/// <param name="ppo">Base Percentage Price Oscillator.</param>
	/// <param name="signalMa">Signal EMA (default 9).</param>
	public PercentagePriceOscillatorHistogram(PercentagePriceOscillator ppo, ExponentialMovingAverage signalMa)
		: base(ppo, signalMa)
	{
		Ppo = ppo;
		SignalMa = signalMa;
		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <summary>
	/// Base Percentage Price Oscillator.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PPOKey,
		Description = LocalizedStrings.PercentagePriceOscillatorKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public PercentagePriceOscillator Ppo { get; }

	/// <summary>
	/// Signal EMA.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SignalMaKey,
		Description = LocalizedStrings.SignalMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage SignalMa { get; }

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" S={Ppo.ShortPeriod} L={Ppo.LongPeriod} Sig={SignalMa.Length}";

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var ppoValue = Ppo.Process(input);
		var signalValue = Ppo.IsFormed ? SignalMa.Process(ppoValue) : new DecimalIndicatorValue(SignalMa, 0m, input.Time) { IsFinal = input.IsFinal };

		var value = new PercentagePriceOscillatorHistogramValue(this, input.Time);
		value.Add(Ppo, ppoValue);
		value.Add(SignalMa, signalValue);
		return value;
	}

	/// <inheritdoc />
	protected override PercentagePriceOscillatorHistogramValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="PercentagePriceOscillatorHistogram"/> indicator value.
/// </summary>
/// <param name="indicator">Indicator.</param>
/// <param name="time">Time.</param>
public class PercentagePriceOscillatorHistogramValue(PercentagePriceOscillatorHistogram indicator, DateTimeOffset time) : ComplexIndicatorValue<PercentagePriceOscillatorHistogram>(indicator, time)
{
	/// <summary>
	/// Gets PPO value.
	/// </summary>
	public IIndicatorValue PpoValue => this[TypedIndicator.Ppo];

	/// <summary>
	/// PPO numeric value.
	/// </summary>
	[Browsable(false)]
	public decimal? Ppo => PpoValue.ToNullableDecimal(TypedIndicator.Source);

	/// <summary>
	/// Gets signal EMA value.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.SignalMa];

	/// <summary>
	/// Signal EMA numeric value.
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"PPO={Ppo}, Signal={Signal}";
}