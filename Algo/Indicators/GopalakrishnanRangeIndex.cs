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
public class GopalakrishnanRangeIndex : LengthIndicator<(decimal high, decimal low)>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GopalakrishnanRangeIndex"/>.
	/// </summary>
	public GopalakrishnanRangeIndex()
	{
		Length = 14;

		Buffer.MaxComparer = Comparer<(decimal high, decimal low)>.Create((x, y) => x.high.CompareTo(y.high));
		Buffer.MinComparer = Comparer<(decimal high, decimal low)>.Create((x, y) => x.low.CompareTo(y.low));
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
			Buffer.PushBack((candle.HighPrice, candle.LowPrice));

		if (IsFormed)
		{
			var currentRange = candle.GetLength();

			var highestHigh = input.IsFinal ? Buffer.Max.Value.high : Math.Max(Buffer.Max.Value.high, candle.HighPrice);
			var lowestLow = input.IsFinal ? Buffer.Min.Value.low : Math.Min(Buffer.Min.Value.low, candle.LowPrice);

			var gapo = currentRange > 0
				? (decimal)Math.Log((double)((highestHigh - lowestLow) / currentRange)) / (decimal)Math.Log(Length)
				: 0;

			return gapo;
		}

		return null;
	}
}


