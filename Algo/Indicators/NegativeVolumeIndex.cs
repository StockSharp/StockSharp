﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Negative Volume Index (NVI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NVIKey,
	Description = LocalizedStrings.NegativeVolumeIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/negative_volume_index.html")]
public class NegativeVolumeIndex : BaseIndicator
{
	private decimal _prevClose;
	private decimal _prevVolume;
	private decimal _nvi;

	/// <summary>
	/// Initializes a new instance of the <see cref="NegativeVolumeIndex"/>.
	/// </summary>
	public NegativeVolumeIndex()
	{
		Reset();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose != 0 && _prevVolume != 0 && candle.TotalVolume != 0)
		{
			if (candle.TotalVolume < _prevVolume)
			{
				var priceChangePercent = (candle.ClosePrice - _prevClose) / _prevClose;
				_nvi += _nvi * priceChangePercent;
			}
		}

		if (input.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			_prevVolume = candle.TotalVolume;
			IsFormed = true;
		}

		return new DecimalIndicatorValue(this, _nvi, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		_prevVolume = 0;
		_nvi = 1000;

		base.Reset();
	}
}