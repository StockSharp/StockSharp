namespace StockSharp.Algo.Indicators;

/// <summary>
/// Percentage Price Oscillator with signal line (no histogram).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PPOSKey,
	Description = LocalizedStrings.PercentagePriceOscillatorSignalKey)]
[Doc("topics/api/indicators/list_of_indicators/percentage_price_oscillator_signal.html")]
[IndicatorOut(typeof(PercentagePriceOscillatorSignalValue))]
public class PercentagePriceOscillatorSignal : BaseComplexIndicator<PercentagePriceOscillatorSignalValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PercentagePriceOscillatorSignal"/>.
	/// </summary>
	public PercentagePriceOscillatorSignal()
		: this(new(), new() { Length = 9 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PercentagePriceOscillatorSignal"/>.
	/// </summary>
	/// <param name="ppo">Base PPO.</param>
	/// <param name="signalMa">Signal EMA.</param>
	public PercentagePriceOscillatorSignal(PercentagePriceOscillator ppo, ExponentialMovingAverage signalMa)
		: base(ppo, signalMa)
	{
		Ppo = ppo;
		SignalMa = signalMa;
		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <summary>
	/// Base PPO.
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

		var value = new PercentagePriceOscillatorSignalValue(this, input.Time);
		value.Add(Ppo, ppoValue);
		value.Add(SignalMa, signalValue);
		return value;
	}

	/// <inheritdoc />
	protected override PercentagePriceOscillatorSignalValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Value for <see cref="PercentagePriceOscillatorSignal"/>.
/// </summary>
/// <param name="indicator">Indicator.</param>
/// <param name="time">Time.</param>
public class PercentagePriceOscillatorSignalValue(PercentagePriceOscillatorSignal indicator, DateTimeOffset time) : ComplexIndicatorValue<PercentagePriceOscillatorSignal>(indicator, time)
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
	/// Gets signal value.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.SignalMa];

	/// <summary>
	/// Signal numeric value.
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"PPO={Ppo}, Signal={Signal}";
}