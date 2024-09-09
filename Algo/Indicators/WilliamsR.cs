﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Williams Percent Range.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/%r.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WRKey,
	Description = LocalizedStrings.WilliamsRKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/%r.html")]
public class WilliamsR : LengthIndicator<decimal>
{
	private readonly Lowest _low;
	private readonly Highest _high;

	/// <summary>
	/// Initializes a new instance of the <see cref="WilliamsR"/>.
	/// </summary>
	public WilliamsR()
	{
		_low = new Lowest();
		_high = new Highest();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _low.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_high.Length = _low.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var lowValue = _low.Process(input, candle.LowPrice).ToDecimal();
		var highValue = _high.Process(input, candle.HighPrice).ToDecimal();

		var diff = highValue - lowValue;

		if (diff != 0)
			return new DecimalIndicatorValue(this, -100m * (highValue - candle.ClosePrice) / diff, input.Time);
			
		return new DecimalIndicatorValue(this, input.Time);
	}
}