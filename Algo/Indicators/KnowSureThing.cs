namespace StockSharp.Algo.Indicators;

/// <summary>
/// Know Sure Thing (KST) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KSTKey,
	Description = LocalizedStrings.KnowSureThingKey)]
[Doc("topics/api/indicators/list_of_indicators/kst.html")]
public class KnowSureThing : BaseComplexIndicator
{
	private readonly RateOfChange _roc1 = new() { Length = 10 };
	private readonly RateOfChange _roc2 = new() { Length = 15 };
	private readonly RateOfChange _roc3 = new() { Length = 20 };
	private readonly RateOfChange _roc4 = new() { Length = 30 };
	private readonly SimpleMovingAverage _sma1 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma2 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma3 = new() { Length = 10 };
	private readonly SimpleMovingAverage _sma4 = new() { Length = 15 };
	private readonly SimpleMovingAverage _signal = new() { Length = 9 };
	private readonly KnowSureThingLine _kstLine = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="KnowSureThing"/>.
	/// </summary>
	public KnowSureThing()
	{
		AddInner(_kstLine);
		AddInner(_signal);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _roc4.Length + _sma4.Length + _signal.Length;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _kstLine.IsFormed && _signal.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ComplexIndicatorValue(this, input.Time);

		var roc1Value = _roc1.Process(input);
		var roc2Value = _roc2.Process(input);
		var roc3Value = _roc3.Process(input);
		var roc4Value = _roc4.Process(input);

		if (!_roc1.IsFormed)
			return result;

		var sma1Value = _sma1.Process(roc1Value);
		var sma2Value = _sma2.Process(roc2Value);
		var sma3Value = _sma3.Process(roc3Value);
		var sma4Value = _sma4.Process(roc4Value);

		if (!_sma1.IsFormed)
			return result;

		var kst = sma1Value.ToDecimal() + 2 * sma2Value.ToDecimal() + 3 * sma3Value.ToDecimal() + 4 * sma4Value.ToDecimal();
		var signalValue = _signal.Process(kst, input.Time);

		result.Add(this, new DecimalIndicatorValue(this, kst, input.Time));
		result.Add(_signal, signalValue);

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