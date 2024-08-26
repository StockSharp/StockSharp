namespace StockSharp.Algo.Indicators;

/// <summary>
/// Convergence/divergence of moving averages. Histogram.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/macd_histogram.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MACDHistogramKey,
	Description = LocalizedStrings.HistogramDescKey)]
[Doc("topics/api/indicators/list_of_indicators/macd_histogram.html")]
public class MovingAverageConvergenceDivergenceHistogram : MovingAverageConvergenceDivergenceSignal
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
	/// </summary>
	public MovingAverageConvergenceDivergenceHistogram()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
	/// </summary>
	/// <param name="macd">Convergence/divergence of moving averages.</param>
	/// <param name="signalMa">Signaling Moving Average.</param>
	public MovingAverageConvergenceDivergenceHistogram(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
		: base(macd, signalMa)
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var macdValue = Macd.Process(input);
		var signalValue = Macd.IsFormed ? SignalMa.Process(macdValue) : new DecimalIndicatorValue(SignalMa, 0, input.Time);

		var value = new ComplexIndicatorValue(this, input.Time);
		//value.InnerValues.Add(Macd, input.SetValue(this, macdValue.GetValue<decimal>() - signalValue.GetValue<decimal>()));
		value.InnerValues.Add(Macd, macdValue);
		value.InnerValues.Add(SignalMa, signalValue);
		return value;
	}
}