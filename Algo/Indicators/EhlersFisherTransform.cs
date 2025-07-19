namespace StockSharp.Algo.Indicators;

/// <summary>
/// Ehlers Fisher Transform indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EFTKey,
	Description = LocalizedStrings.EhlersFisherTransformKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/ehlers_fisher_transform.html")]
[IndicatorOut(typeof(EhlersFisherTransformValue))]
public class EhlersFisherTransform : BaseComplexIndicator<EhlersFisherTransformValue>
{
	private readonly CircularBufferEx<decimal> _highBuffer;
	private readonly CircularBufferEx<decimal> _lowBuffer;
	private decimal _prevValue;
	private decimal _currValue;

	/// <summary>
	/// Main line.
	/// </summary>
	[Browsable(false)]
	public EhlersFisherTransformLine MainLine { get; }

	/// <summary>
	/// Trigger line.
	/// </summary>
	[Browsable(false)]
	public EhlersFisherTransformLine TriggerLine { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EhlersFisherTransform"/>.
	/// </summary>
	public EhlersFisherTransform()
	{
		_highBuffer = new(1) { MaxComparer = Comparer<decimal>.Default };
		_lowBuffer = new(1) { MinComparer = Comparer<decimal>.Default };

		MainLine = new();
		TriggerLine = new();

		AddInner(MainLine);
		AddInner(TriggerLine);

		Length = 10;
	}

	private int _length;

	/// <summary>
	/// Length of period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _length;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_length = value;

			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + Length - 1;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
		{
			_highBuffer.PushBack(candle.HighPrice);
			_lowBuffer.PushBack(candle.LowPrice);
		}

		var result = new EhlersFisherTransformValue(this, input.Time);

		if (_highBuffer.Count >= Length)
		{
			var maxHigh = _highBuffer.Max.Value;
			var minLow = _lowBuffer.Min.Value;

			var value = 0.5m * (((candle.HighPrice + candle.LowPrice) / 2 - minLow) / (maxHigh - minLow) - 0.5m);

			value = 0.66m * value + 0.67m * _prevValue;
			value = Math.Max(Math.Min(value, 0.999m), -0.999m);

			var fisherTransform = 0.5m * (decimal)Math.Log((double)((1 + value) / (1 - value)));

			result.Add(MainLine, MainLine.Process(input, fisherTransform));
			result.Add(TriggerLine, TriggerLine.Process(input, _currValue));

			if (input.IsFinal)
			{
				_currValue = fisherTransform;
				_prevValue = value;
			}
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_highBuffer.Capacity = Length;
		_lowBuffer.Capacity = Length;
		_prevValue = 0;
		_currValue = 0;

		base.Reset();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override EhlersFisherTransformValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Represents a single line of the Ehlers Fisher Transform indicator.
/// </summary>
[IndicatorHidden]
public class EhlersFisherTransformLine : BaseIndicator
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
/// <see cref="EhlersFisherTransform"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EhlersFisherTransformValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="EhlersFisherTransform"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class EhlersFisherTransformValue(EhlersFisherTransform indicator, DateTimeOffset time) : ComplexIndicatorValue<EhlersFisherTransform>(indicator, time)
{
	/// <summary>
	/// Gets the main line value.
	/// </summary>
	public IIndicatorValue MainLineValue => this[TypedIndicator.MainLine];

	/// <summary>
	/// Gets the main line value.
	/// </summary>
	[Browsable(false)]
	public decimal? MainLine => MainLineValue.ToNullableDecimal();

	/// <summary>
	/// Gets the trigger line value.
	/// </summary>
	public IIndicatorValue TriggerLineValue => this[TypedIndicator.TriggerLine];

	/// <summary>
	/// Gets the trigger line value.
	/// </summary>
	[Browsable(false)]
	public decimal? TriggerLine => TriggerLineValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"MainLine={MainLine}, TriggerLine={TriggerLine}";
}
