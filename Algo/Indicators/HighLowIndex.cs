namespace StockSharp.Algo.Indicators;

/// <summary>
/// High Low Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HLIKey,
	Description = LocalizedStrings.HighLowIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/high_low_index.html")]
public class HighLowIndex : LengthIndicator<decimal>
{
	private readonly CircularBufferEx<decimal> _highBuffer;
	private readonly CircularBufferEx<decimal> _lowBuffer;

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
	protected override bool CalcIsFormed() => _highBuffer.Count == Length;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		decimal highestHigh;
		decimal lowestLow;

		if (input.IsFinal)
		{
			_highBuffer.PushBack(candle.HighPrice);
			_lowBuffer.PushBack(candle.LowPrice);

			highestHigh = _highBuffer.Max.Value;
			lowestLow = _lowBuffer.Min.Value;
		}
		else
		{
			if (_highBuffer.Count == 0 || _lowBuffer.Count == 0)
				return null;

			highestHigh = _highBuffer.Max.Value.Max(candle.HighPrice);
			lowestLow = _lowBuffer.Min.Value.Min(candle.LowPrice);
		}

		if (IsFormed)
		{
			var range = highestHigh - lowestLow;

			if (range == 0)
				return 50;

			var result = (candle.HighPrice - lowestLow) / range * 100;
			return result;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_highBuffer.Capacity = Length;
		_lowBuffer.Capacity = Length;
		base.Reset();
	}
}