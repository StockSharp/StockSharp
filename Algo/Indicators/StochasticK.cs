namespace StockSharp.Algo.Indicators;

/// <summary>
/// Stochastic %K.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/stochastic_oscillator_k.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StochasticKKey,
	Description = LocalizedStrings.StochasticKDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/stochastic_oscillator_k.html")]
public class StochasticK : LengthIndicator<decimal>
{
	// Минимальная цена за период.
	private readonly Lowest _low = new();

	// Максимальная цена за период.
	private readonly Highest _high = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticK"/>.
	/// </summary>
	public StochasticK()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _high.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_high.Length = _low.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var highValue = _high.Process(input, candle.HighPrice).ToDecimal();
		var lowValue = _low.Process(input, candle.LowPrice).ToDecimal();

		var diff = highValue - lowValue;

		if (diff == 0)
			return 0;

		return 100 * (candle.ClosePrice - lowValue) / diff;
	}
}