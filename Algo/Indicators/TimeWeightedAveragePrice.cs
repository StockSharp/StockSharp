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
	private decimal _cumulativePrice;
	private int _count;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeWeightedAveragePrice"/>.
	/// </summary>
	public TimeWeightedAveragePrice()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;

		decimal twap;

		if (input.IsFinal)
		{
			_cumulativePrice += typicalPrice;
			_count++;
			IsFormed = true;
			twap = _cumulativePrice / _count;
		}
		else
		{
			twap = (_cumulativePrice + typicalPrice) / (_count + 1);
		}

		return new DecimalIndicatorValue(this, twap, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_cumulativePrice = 0;
		_count = 0;
		base.Reset();
	}
}