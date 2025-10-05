namespace StockSharp.Algo.Indicators;

/// <summary>
/// Know Sure Thing (KST) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KSTKey,
	Description = LocalizedStrings.KnowSureThingKey)]
[Doc("topics/api/indicators/list_of_indicators/kst.html")]
[IndicatorOut(typeof(IKnowSureThingValue))]
public class KnowSureThing : BaseComplexIndicator<IKnowSureThingValue>
{
	/// <summary>
	/// Rate of Change #1.
	/// </summary>
	[Browsable(false)]
	public RateOfChange Roc1 { get; } = new() { Length = 10 };

	/// <summary>
	/// Rate of Change #2.
	/// </summary>
	[Browsable(false)]
	public RateOfChange Roc2 { get; } = new() { Length = 15 };

	/// <summary>
	/// Rate of Change #3.
	/// </summary>
	[Browsable(false)]
	public RateOfChange Roc3 { get; } = new() { Length = 20 };

	/// <summary>
	/// Rate of Change #4.
	/// </summary>
	[Browsable(false)]
	public RateOfChange Roc4 { get; } = new() { Length = 30 };

	/// <summary>
	/// Simple Moving Average applied to ROC #1.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma1 { get; } = new() { Length = 10 };

	/// <summary>
	/// Simple Moving Average applied to ROC #2.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma2 { get; } = new() { Length = 10 };

	/// <summary>
	/// Simple Moving Average applied to ROC #3.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma3 { get; } = new() { Length = 10 };

	/// <summary>
	/// Simple Moving Average applied to ROC #4.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage Sma4 { get; } = new() { Length = 15 };

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
		=> Roc4.NumValuesToInitialize + Sma4.NumValuesToInitialize + Signal.NumValuesToInitialize - 2;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Signal.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new KnowSureThingValue(this, input.Time);

		var roc1Value = Roc1.Process(input);
		var roc2Value = Roc2.Process(input);
		var roc3Value = Roc3.Process(input);
		var roc4Value = Roc4.Process(input);

		if (!Roc4.IsFormed)
			return result;

		var sma1Value = Sma1.Process(roc1Value);
		var sma2Value = Sma2.Process(roc2Value);
		var sma3Value = Sma3.Process(roc3Value);
		var sma4Value = Sma4.Process(roc4Value);

		if (!Sma4.IsFormed)
			return result;

		var kst = sma1Value.ToDecimal(Source) + 2 * sma2Value.ToDecimal(Source) + 3 * sma3Value.ToDecimal(Source) + 4 * sma4Value.ToDecimal(Source);

		result.Add(KstLine, KstLine.Process(kst, input.Time, input.IsFinal));
		result.Add(Signal, Signal.Process(kst, input.Time, input.IsFinal));

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		Roc1.Reset();
		Roc2.Reset();
		Roc3.Reset();
		Roc4.Reset();
		Sma1.Reset();
		Sma2.Reset();
		Sma3.Reset();
		Sma4.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	protected override IKnowSureThingValue CreateValue(DateTimeOffset time)
		=> new KnowSureThingValue(this, time);
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
public interface IKnowSureThingValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the KST line value.
	/// </summary>
	IIndicatorValue KstLineValue { get; }

	/// <summary>
	/// Gets the KST line value.
	/// </summary>
	[Browsable(false)]
	decimal? KstLine { get; }

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	IIndicatorValue SignalValue { get; }

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	[Browsable(false)]
	decimal? Signal { get; }
}

/// <summary>
/// KnowSureThing indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowSureThingValue"/> class.
/// </remarks>
/// <param name="indicator">The parent KnowSureThing indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class KnowSureThingValue(KnowSureThing indicator, DateTimeOffset time) : ComplexIndicatorValue<KnowSureThing>(indicator, time), IKnowSureThingValue
{
	/// <inheritdoc />
	public IIndicatorValue KstLineValue => this[TypedIndicator.KstLine];
	/// <inheritdoc />
	public decimal? KstLine => KstLineValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];
	/// <inheritdoc />
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"KstLine={KstLine}, Signal={Signal}";
}
