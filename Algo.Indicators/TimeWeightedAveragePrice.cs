namespace StockSharp.Algo.Indicators;

/// <summary>
/// Time Weighted Average Price (TWAP) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TWAPKey,
	Description = LocalizedStrings.TimeWeightedAveragePriceKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/time_weighted_average_price.html")]
public class TimeWeightedAveragePrice : BaseIndicator
{
	private decimal _weightedPrice;
	private decimal _totalWeight;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeWeightedAveragePrice"/>.
	/// </summary>
	public TimeWeightedAveragePrice()
	{
	}

	// How long the candle's price stood, in seconds. A candle that reports no duration still stands
	// for one observation of its price, so it weighs one unit.
	private static decimal GetWeight(ICandleMessage candle)
	{
		var duration = candle.CloseTime - candle.OpenTime;

		return duration > TimeSpan.Zero ? (decimal)duration.TotalSeconds : 1;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var typicalPrice = candle.GetTypicalPrice();
		var weight = GetWeight(candle);

		decimal twap;

		if (input.IsFinal)
		{
			_weightedPrice += typicalPrice * weight;
			_totalWeight += weight;
			IsFormed = true;
			twap = _weightedPrice / _totalWeight;
		}
		else
		{
			twap = (_weightedPrice + typicalPrice * weight) / (_totalWeight + weight);
		}

		return new DecimalIndicatorValue(this, twap, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_weightedPrice = 0;
		_totalWeight = 0;
		base.Reset();
	}
}