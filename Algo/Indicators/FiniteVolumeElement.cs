namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Finite Volume Element (FVE) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FVEKey,
	Description = LocalizedStrings.FiniteVolumeElementKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/finite_volume_element.html")]
public class FiniteVolumeElement : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FiniteVolumeElement"/>.
	/// </summary>
	public FiniteVolumeElement()
	{
		Length = 22;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		decimal fve;
		if (cl != 0 && candle.TotalVolume != 0)
		{
			var vf = candle.TotalVolume * (2 * ((candle.ClosePrice - candle.LowPrice) / cl) - 1);
			fve = vf / candle.TotalVolume;
		}
		else
		{
			fve = 0;
		}

		if (input.IsFinal)
			Buffer.PushBack(fve);

		if (IsFormed)
		{
			var result = (Buffer.Sum + (input.IsFinal ? 0 : fve - Buffer.Back())) / Length;
			return result * 100;
		}

		return null;
	}
}