﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Force Index indicator, also known as Elder's Force Index (EFI).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FIKey,
	Description = LocalizedStrings.ForceIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/force_index.html")]
public class ForceIndex : ExponentialMovingAverage
{
	private decimal _prevClosePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ForceIndex"/>.
	/// </summary>
	public ForceIndex()
		: base()
	{
		Length = 13;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClosePrice = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClosePrice == 0)
		{
			if (input.IsFinal)
				_prevClosePrice = candle.ClosePrice;

			return new DecimalIndicatorValue(this, input.Time);
		}

		var force = (candle.ClosePrice - _prevClosePrice) * candle.TotalVolume;

		var emaValue = base.OnProcess(input.SetValue(this, force));

		if (input.IsFinal)
			_prevClosePrice = candle.ClosePrice;

		return emaValue;
	}
}
