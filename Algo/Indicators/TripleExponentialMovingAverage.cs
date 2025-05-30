namespace StockSharp.Algo.Indicators;

/// <summary>
/// Triple Exponential Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/tema.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TEMAKey,
	Description = LocalizedStrings.TripleExponentialMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/tema.html")]
public class TripleExponentialMovingAverage : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage _ema1;
	private readonly ExponentialMovingAverage _ema2;
	private readonly ExponentialMovingAverage _ema3;

	/// <summary>
	/// Initializes a new instance of the <see cref="TripleExponentialMovingAverage"/>.
	/// </summary>
	public TripleExponentialMovingAverage()
	{
		_ema1 = new ExponentialMovingAverage();
		_ema2 = new ExponentialMovingAverage();
		_ema3 = new ExponentialMovingAverage();

		Length = 32;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _ema3.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _ema1.NumValuesToInitialize + _ema2.NumValuesToInitialize + _ema3.NumValuesToInitialize - 2;

	/// <inheritdoc />
	public override void Reset()
	{
		_ema3.Length = _ema2.Length = _ema1.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var ema1Value = _ema1.Process(input);

		if (!_ema1.IsFormed)
			return null;

		var ema2Value = _ema2.Process(ema1Value);

		if (!_ema2.IsFormed)
			return null;

		var ema3Value = _ema3.Process(ema2Value);

		return 3 * ema1Value.ToDecimal() - 3 * ema2Value.ToDecimal() + ema3Value.ToDecimal();
	}
}