namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Chaikin Money Flow (CMF) indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/chaikin_money_flow.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CMFKey,
	Description = LocalizedStrings.ChaikinMoneyFlowKey)]
[Doc("topics/api/indicators/list_of_indicators/chaikin_money_flow.html")]
[IndicatorIn(typeof(CandleIndicatorValue))]
public class ChaikinMoneyFlow : LengthIndicator<decimal>
{
	private decimal _moneyFlowVolumeSum;
	private decimal _volumeSum;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChaikinMoneyFlow"/>.
	/// </summary>
	public ChaikinMoneyFlow()
	{
		Length = 20;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_moneyFlowVolumeSum = 0;
		_volumeSum = 0;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var hl = candle.GetLength();

		var moneyFlowMultiplier = hl != 0
			? ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / hl
			: 0;

		var moneyFlowVolume = moneyFlowMultiplier * candle.TotalVolume;

		decimal moneyFlowVolumeSum;
		decimal volumeSum;

		if (input.IsFinal)
		{
			_moneyFlowVolumeSum += moneyFlowVolume;
			_volumeSum += candle.TotalVolume;

			if (Buffer.Count == Length)
			{
				var oldValue = Buffer.Front();
				_moneyFlowVolumeSum -= oldValue;
				_volumeSum -= oldValue;
			}

			Buffer.PushBack(moneyFlowVolume);

			moneyFlowVolumeSum = _moneyFlowVolumeSum;
			volumeSum = _volumeSum;
		}
		else
		{
			if (Buffer.Count == 0)
				return null;

			var front = Buffer.Front();

			moneyFlowVolumeSum = _moneyFlowVolumeSum - front + moneyFlowVolume;
			volumeSum = _volumeSum - front + candle.TotalVolume;
		}

		if (IsFormed)
		{
			return volumeSum != 0 ? moneyFlowVolumeSum / volumeSum : 0;
		}

		return null;
	}
}
