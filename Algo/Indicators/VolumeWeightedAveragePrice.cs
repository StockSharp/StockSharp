namespace StockSharp.Algo.Indicators;

/// <summary>
/// Volume Weighted Average Price (VWAP).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VWAPKey,
	Description = LocalizedStrings.VolumeWeightedAveragePriceKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/volume_weighted_average_price.html")]
public class VolumeWeightedAveragePrice : BaseIndicator
{
	private decimal _cumulativePriceVolume;
	private decimal _cumulativeVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeWeightedAveragePrice"/>.
	/// </summary>
	public VolumeWeightedAveragePrice()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var priceVolume = typicalPrice * candle.TotalVolume;

		decimal cumulativePriceVolume;
		decimal cumulativeVolume;

		if (input.IsFinal)
		{
			_cumulativePriceVolume += priceVolume;
			_cumulativeVolume += candle.TotalVolume;

			cumulativePriceVolume = _cumulativePriceVolume;
			cumulativeVolume = _cumulativeVolume;

			IsFormed = true;
		}
		else
		{
			cumulativePriceVolume = _cumulativePriceVolume + priceVolume;
			cumulativeVolume = _cumulativeVolume + candle.TotalVolume;
		}

		if (cumulativeVolume > 0)
		{
			var vwap = cumulativePriceVolume / cumulativeVolume;
			return new DecimalIndicatorValue(this, vwap, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;
		base.Reset();
	}
}