﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// On-Balance Volume (OBV).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/obv.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OBVKey,
	Description = LocalizedStrings.OnBalanceVolumeKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/obv.html")]
public class OnBalanceVolume : BaseIndicator
{
	private decimal _prevClosePrice;
	private decimal _currentValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="OnBalanceVolume"/>.
	/// </summary>
	public OnBalanceVolume()
	{
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClosePrice = 0;
		_currentValue = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClosePrice != 0)
		{
			if (candle.ClosePrice > _prevClosePrice)
				_currentValue += candle.TotalVolume;
			else if (candle.ClosePrice < _prevClosePrice)
				_currentValue -= candle.TotalVolume;
		}

		if (input.IsFinal)
		{
			_prevClosePrice = candle.ClosePrice;
			IsFormed = true;
		}

		return new DecimalIndicatorValue(this, _currentValue, input.Time);
	}
}