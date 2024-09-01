namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Ease of Movement (EMV).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EMVKey,
	Description = LocalizedStrings.EaseOfMovementKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/ease_of_movement.html")]
public class EaseOfMovement : LengthIndicator<decimal>
{
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// Initializes a new instance of the <see cref="EaseOfMovement"/>.
	/// </summary>
	public EaseOfMovement()
	{
		Length = 14;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		if (_prevHigh != 0 && _prevLow != 0 && cl != 0)
		{
			var midPointMove = ((candle.HighPrice + candle.LowPrice) / 2) - ((_prevHigh + _prevLow) / 2);
			var boxRatio = candle.TotalVolume / cl;
			var emv = midPointMove / boxRatio;

			decimal sum;

			if (input.IsFinal)
			{
				Buffer.AddEx(emv);
				sum = Buffer.Sum;
			}
			else
				sum = Buffer.Sum + emv;

			if (IsFormed)
				return new DecimalIndicatorValue(this, sum / Length, input.Time);
		}

		if (input.IsFinal)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevHigh = 0;
		_prevLow = 0;

		base.Reset();
	}
}
