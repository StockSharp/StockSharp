namespace StockSharp.Algo.Indicators;

/// <summary>
/// Know Sure Thing (KST) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KSTKey,
	Description = LocalizedStrings.KnowSureThingKey)]
[Doc("topics/api/indicators/list_of_indicators/kst.html")]
[IndicatorOut(typeof(KnowSureThingValue))]
public class KnowSureThing : BaseComplexIndicator<KnowSureThingValue>
{
	private readonly RateOfChange _roc1 = new() { Length = 10 };
	private readonly RateOfChange _roc2 = new() { Length = 15 };
	private readonly RateOfChange _roc3 = new() { Length = 20 };
	private readonly RateOfChange _roc4 = new() { Length = 30 };
	private readonly SimpleMovingAverage _sma1 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma2 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma3 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma4 = new() { Length = 15 };

	/// <summary>
	/// Signal line.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Signal { get; } = new() { Length = 9 };

	/// <summary>
	/// KST line.
	/// </summary>
	[Browsable(false)]
	public KnowSureThingLine KstLine { get; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="KnowSureThing"/>.
	/// </summary>
	public KnowSureThing()
	{
		AddInner(KstLine);
		AddInner(Signal);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _roc4.NumValuesToInitialize + _sma4.NumValuesToInitialize + Signal.NumValuesToInitialize - 2;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Signal.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new KnowSureThingValue(this, input.Time);

		var roc1Value = _roc1.Process(input);
		var roc2Value = _roc2.Process(input);
		var roc3Value = _roc3.Process(input);
		var roc4Value = _roc4.Process(input);

		if (!_roc4.IsFormed)
			return result;

		var sma1Value = _sma1.Process(roc1Value);
		var sma2Value = _sma2.Process(roc2Value);
		var sma3Value = _sma3.Process(roc3Value);
		var sma4Value = _sma4.Process(roc4Value);

		if (!_sma4.IsFormed)
			return result;

		var kst = sma1Value.ToDecimal() + 2 * sma2Value.ToDecimal() + 3 * sma3Value.ToDecimal() + 4 * sma4Value.ToDecimal();

		result.Add(KstLine, KstLine.Process(kst, input.Time, input.IsFinal));
		result.Add(Signal, Signal.Process(kst, input.Time, input.IsFinal));

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_roc1.Reset();
		_roc2.Reset();
		_roc3.Reset();
		_roc4.Reset();
		_sma1.Reset();
		_sma2.Reset();
		_sma3.Reset();
		_sma4.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	protected override KnowSureThingValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// KST line.
/// </summary>
[IndicatorHidden]
public class KnowSureThingLine : BaseIndicator
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
/// <see cref="KnowSureThing"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowSureThingValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="KnowSureThing"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class KnowSureThingValue(KnowSureThing indicator, DateTimeOffset time) : ComplexIndicatorValue<KnowSureThing>(indicator, time)
{
	/// <summary>
	/// Gets the KST line value.
	/// </summary>
	public IIndicatorValue KstLineValue => this[TypedIndicator.KstLine];

	/// <summary>
	/// Gets the KST line value.
	/// </summary>
	[Browsable(false)]
	public decimal? KstLine => KstLineValue.ToNullableDecimal();

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"KstLine={KstLine}, Signal={Signal}";
}
