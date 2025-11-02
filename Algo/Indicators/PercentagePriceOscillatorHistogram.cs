namespace StockSharp.Algo.Indicators;

/// <summary>
/// Percentage Price Oscillator with signal line (histogram painter will plot difference).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PPOHKey,
	Description = LocalizedStrings.PercentagePriceOscillatorHistogramKey)]
[Doc("topics/api/indicators/list_of_indicators/percentage_price_oscillator_histogram.html")]
[IndicatorOut(typeof(IPercentagePriceOscillatorHistogramValue))]
public class PercentagePriceOscillatorHistogram : BaseComplexIndicator<IPercentagePriceOscillatorHistogramValue>
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
	protected override IPercentagePriceOscillatorHistogramValue CreateValue(DateTime time)
		=> new PercentagePriceOscillatorHistogramValue(this, time);
}

/// <summary>
/// <see cref="PercentagePriceOscillatorHistogram"/> indicator value.
/// </summary>
public interface IPercentagePriceOscillatorHistogramValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets PPO value.
	/// </summary>
	IIndicatorValue PpoValue { get; }

	/// <summary>
	/// Gets PPO value.
	/// </summary>
	[Browsable(false)]
	decimal? Ppo { get; }

	/// <summary>
	/// Gets signal EMA value.
	/// </summary>
	IIndicatorValue SignalValue { get; }

	/// <summary>
	/// Signal EMA numeric value.
	/// </summary>
	[Browsable(false)]
	decimal? Signal { get; }
}

/// <summary>
/// PercentagePriceOscillatorHistogram indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PercentagePriceOscillatorHistogramValue"/> class.
/// </remarks>
/// <param name="indicator">The parent PercentagePriceOscillatorHistogram indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class PercentagePriceOscillatorHistogramValue(PercentagePriceOscillatorHistogram indicator, DateTime time) : ComplexIndicatorValue<PercentagePriceOscillatorHistogram>(indicator, time), IPercentagePriceOscillatorHistogramValue
{
	/// <inheritdoc />
	public IIndicatorValue PpoValue => this[TypedIndicator.Ppo];
	/// <inheritdoc />
	public decimal? Ppo => PpoValue.ToNullableDecimal(TypedIndicator.Ppo.Source);

	/// <inheritdoc />
	public IIndicatorValue SignalValue => this[TypedIndicator.SignalMa];
	/// <inheritdoc />
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.SignalMa.Source);

	/// <inheritdoc />
	public override string ToString() => $"PPO={Ppo}, Signal={Signal}";
}