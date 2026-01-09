namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Gopalakrishnan Range Index (GAPO) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GAPOKey,
	Description = LocalizedStrings.GopalakrishnanRangeIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/gopalakrishnan_range_index.html")]
public class GopalakrishnanRangeIndex : DecimalLengthIndicator
{
	private readonly DecimalBuffer _high = new(14) { Stats = CircularBufferStats.Max };
	private readonly DecimalBuffer _low = new(14) { Stats = CircularBufferStats.Min };

	/// <summary>
	/// Initializes a new instance of the <see cref="GopalakrishnanRangeIndex"/>.
	/// </summary>
	public GopalakrishnanRangeIndex()
	{
#if !NET7_0_OR_GREATER
		_high.Operator = new DecimalOperator();
		_low.Operator = new DecimalOperator();
#endif

		Length = 14;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_high.Capacity = _low.Capacity = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _high.Count >= Length;

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
		{
			_high.PushBack(candle.HighPrice);
			_low.PushBack(candle.LowPrice);
		}

		if (IsFormed)
		{
			var currentRange = candle.GetLength();

			var highestHigh = input.IsFinal ? _high.Max.Value : Math.Max(_high.Max.Value, candle.HighPrice);
			var lowestLow = input.IsFinal ? _low.Min.Value : Math.Min(_low.Min.Value, candle.LowPrice);

			var gapo = currentRange > 0
				? (decimal)Math.Log((double)((highestHigh - lowestLow) / currentRange)) / (decimal)Math.Log(Length)
				: 0;

			return gapo;
		}

		return null;
	}
}


