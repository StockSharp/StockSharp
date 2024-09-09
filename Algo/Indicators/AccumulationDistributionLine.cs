﻿namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Accumulation/Distribution Line (A/D Line).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ADLKey,
	Description = LocalizedStrings.AccumulationDistributionLineKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/accumulation_distribution_line.html")]
public class AccumulationDistributionLine : BaseIndicator
{
	private decimal _adLine;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var cl = candle.GetLength();

		if (cl != 0)
		{
			var mfm = ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / cl;
			var mfv = mfm * candle.TotalVolume;
			_adLine += mfv;
		}

		if (input.IsFinal)
			IsFormed = true;

		return new DecimalIndicatorValue(this, _adLine, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_adLine = 0;

		base.Reset();
	}
}
