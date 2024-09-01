namespace StockSharp.Algo.Indicators;

/// <summary>
/// High Low Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HLIKey,
	Description = LocalizedStrings.HighLowIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/high_low_index.html")]
public class HighLowIndex : LengthIndicator<decimal>
{
	private readonly LengthIndicatorBuffer<decimal> _highBuffer;
	private readonly LengthIndicatorBuffer<decimal> _lowBuffer;

	/// <summary>
	/// Initializes a new instance of the <see cref="HighLowIndex"/>.
	/// </summary>
	public HighLowIndex()
	{
		_highBuffer = new(1) { MaxComparer = Comparer<decimal>.Default };
		_lowBuffer = new(1) { MinComparer = Comparer<decimal>.Default };

		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int Length
	{
		get => base.Length;
		set
		{
			base.Length = value;

			_highBuffer.Capacity = value;
			_lowBuffer.Capacity = value;
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _highBuffer.Count == Length;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		decimal highestHigh;
		decimal lowestLow;

		if (input.IsFinal)
		{
			_highBuffer.AddEx(candle.HighPrice);
			_lowBuffer.AddEx(candle.LowPrice);

			highestHigh = _highBuffer.Max.Value;
			lowestLow = _lowBuffer.Min.Value;
		}
		else
		{
			highestHigh = _highBuffer.Max.Value.Max(candle.HighPrice);
			lowestLow = _lowBuffer.Min.Value.Min(candle.LowPrice);
		}

		if (IsFormed)
		{
			var range = highestHigh - lowestLow;

			if (range == 0)
				return new DecimalIndicatorValue(this, 50, input.Time);

			var result = (candle.HighPrice - lowestLow) / range * 100;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_highBuffer.Clear();
		_lowBuffer.Clear();
		base.Reset();
	}
}