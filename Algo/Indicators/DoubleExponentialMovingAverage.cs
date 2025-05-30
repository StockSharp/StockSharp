namespace StockSharp.Algo.Indicators;

/// <summary>
/// Double Exponential Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dema.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DEMAKey,
	Description = LocalizedStrings.DoubleExponentialMovingAverageKey)]
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
		_ema1 = new();
		_ema2 = new();

		Length = 32;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_ema2.Length = _ema1.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _ema2.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _ema1.NumValuesToInitialize + _ema2.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var ema1Value = _ema1.Process(input);

		if (!_ema1.IsFormed)
			return null;

		var ema2Value = _ema2.Process(ema1Value);

		return 2 * ema1Value.ToDecimal() - ema2Value.ToDecimal();
	}
}
