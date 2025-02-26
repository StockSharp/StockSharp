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
public class EhlersFisherTransform : BaseComplexIndicator
{
	private readonly CircularBufferEx<decimal> _highBuffer;
	private readonly CircularBufferEx<decimal> _lowBuffer;
	private decimal _prevValue;
	private decimal _currValue;

	private readonly EhlersFisherTransformLine _mainLine;
	private readonly EhlersFisherTransformLine _triggerLine;

	/// <summary>
	/// Initializes a new instance of the <see cref="EhlersFisherTransform"/>.
	/// </summary>
	public EhlersFisherTransform()
	{
		_highBuffer = new(1) { MaxComparer = Comparer<decimal>.Default };
		_lowBuffer = new(1) { MinComparer = Comparer<decimal>.Default };

		_mainLine = new();
		_triggerLine = new();

		AddInner(_mainLine);
		AddInner(_triggerLine);

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
			_length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
		{
			_highBuffer.PushBack(candle.HighPrice);
			_lowBuffer.PushBack(candle.LowPrice);
		}

		if (_highBuffer.Count >= Length)
		{
			var maxHigh = _highBuffer.Max.Value;
			var minLow = _lowBuffer.Min.Value;

			var value = 0.5m * (((candle.HighPrice + candle.LowPrice) / 2 - minLow) / (maxHigh - minLow) - 0.5m);

			value = 0.66m * value + 0.67m * _prevValue;
			value = Math.Max(Math.Min(value, 0.999m), -0.999m);

			var fisherTransform = 0.5m * (decimal)Math.Log((double)((1 + value) / (1 - value)));

			var result = new ComplexIndicatorValue(this, input.Time);
			result.Add(_mainLine, _mainLine.Process(input, fisherTransform));
			result.Add(_triggerLine, _triggerLine.Process(input, _currValue));

			if (input.IsFinal)
			{
				_currValue = fisherTransform;
				_prevValue = value;
			}

			return result;
		}

		return new ComplexIndicatorValue(this, input.Time);
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