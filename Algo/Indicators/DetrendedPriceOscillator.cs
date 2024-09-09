﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Price oscillator without trend.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dpo.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DPOKey,
	Description = LocalizedStrings.DetrendedPriceOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/dpo.html")]
public class DetrendedPriceOscillator : LengthIndicator<decimal>
{
	private readonly SimpleMovingAverage _sma;
	private int _lookBack;

	/// <summary>
	/// Initializes a new instance of the <see cref="DetrendedPriceOscillator"/>.
	/// </summary>
	public DetrendedPriceOscillator()
	{
		_sma = new SimpleMovingAverage();
		Length = 3;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		_sma.Length = Length;
		_lookBack = Length / 2 + 1;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var smaValue = _sma.Process(input);

		if (_sma.IsFormed && input.IsFinal)
			Buffer.PushBack(smaValue.ToDecimal());

		if (!IsFormed)
			return new DecimalIndicatorValue(this, input.Time);

		return new DecimalIndicatorValue(this, input.ToDecimal() - Buffer[Math.Max(0, Buffer.Count - 1 - _lookBack)], input.Time);
	}
}