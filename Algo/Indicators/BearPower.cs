namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bear Power indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/bear_power.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BearPowerKey,
	Description = LocalizedStrings.BearPowerDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/bear_power.html")]
public class BearPower : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage _ema;

	/// <summary>
	/// Initializes a new instance of the <see cref="BearPower"/>.
	/// </summary>
	public BearPower()
	{
		_ema = new() { Length = 13 };
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _ema.NumValuesToInitialize;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _ema.IsFormed;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var emaValue = _ema.Process(input);

		if (_ema.IsFormed && !emaValue.IsEmpty)
			return candle.LowPrice - emaValue.ToDecimal();

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_ema.Length = Length;
		base.Reset();
	}
}