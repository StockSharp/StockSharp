namespace StockSharp.Algo.Indicators;

using Ecng.ComponentModel;

/// <summary>
/// Double Exponential Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dema.html
/// </remarks>
[DisplayName("DEMA")]
[Description("Double Exponential Moving Average")]
[Doc("topics/api/indicators/list_of_indicators/dema.html")]
public class DoubleExponentialMovingAverage : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage _ema1;
	private readonly ExponentialMovingAverage _ema2;

	/// <summary>
	/// Initializes a new instance of the <see cref="DoubleExponentialMovingAverage"/>.
	/// </summary>
	public DoubleExponentialMovingAverage()
	{
		_ema1 = new ExponentialMovingAverage();
		_ema2 = new ExponentialMovingAverage();

		Length = 32;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_ema2.Length = _ema1.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _ema1.IsFormed && _ema2.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var ema1Value = _ema1.Process(input);

		if (!_ema1.IsFormed)
			return new DecimalIndicatorValue(this);

		var ema2Value = _ema2.Process(ema1Value);

		return new DecimalIndicatorValue(this, 2 * ema1Value.GetValue<decimal>() - ema2Value.GetValue<decimal>());
	}
}
