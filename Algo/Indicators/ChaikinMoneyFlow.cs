namespace StockSharp.Algo.Indicators;

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
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, low, close, volume) = input.GetOhlcv();

		var hl = high - low;

		var moneyFlowMultiplier = hl != 0
			? ((close - low) - (high - close)) / hl
			: 0;

		var moneyFlowVolume = moneyFlowMultiplier * volume;

		decimal moneyFlowVolumeSum;
		decimal volumeSum;

		if (input.IsFinal)
		{
			_moneyFlowVolumeSum += moneyFlowVolume;
			_volumeSum += volume;

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
			moneyFlowVolumeSum = _moneyFlowVolumeSum - Buffer.Front() + moneyFlowVolume;
			volumeSum = _volumeSum - Buffer.Front() + volume;
		}

		if (IsFormed)
		{
			return new DecimalIndicatorValue(this, volumeSum != 0 ? moneyFlowVolumeSum / volumeSum : 0, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}
