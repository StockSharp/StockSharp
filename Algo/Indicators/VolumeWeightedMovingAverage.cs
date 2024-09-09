﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Volume weighted moving average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/volume_weighted_ma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VMAKey,
	Description = LocalizedStrings.VolumeWeightedMovingAverageKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/volume_weighted_ma.html")]
public class VolumeWeightedMovingAverage : LengthIndicator<decimal>
{
	// Текущее значение числителя
	private readonly Sum _nominator = new();

	// Текущее значение знаменателя
	private readonly Sum _denominator = new();

	/// <summary>
	/// To create the indicator <see cref="VolumeWeightedMovingAverage"/>.
	/// </summary>
	public VolumeWeightedMovingAverage()
	{
		Length = 32;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_denominator.Length = _nominator.Length = Length;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _nominator.IsFormed && _denominator.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var shValue = _nominator.Process(input, candle.ClosePrice * candle.TotalVolume).ToDecimal();
		var znValue = _denominator.Process(input, candle.TotalVolume).ToDecimal();

		return znValue != 0 
			? new DecimalIndicatorValue(this, shValue / znValue, input.Time) 
			: new DecimalIndicatorValue(this, input.Time);
	}
}