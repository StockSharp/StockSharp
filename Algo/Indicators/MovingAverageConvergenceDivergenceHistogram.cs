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
[IndicatorOut(typeof(MovingAverageConvergenceDivergenceHistogramValue))]
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

		var value = new MovingAverageConvergenceDivergenceHistogramValue(this, input.Time);
		value.Add(Macd, macdValue);
		value.Add(SignalMa, signalValue);
		return value;
	}
	/// <inheritdoc />
	protected override ComplexIndicatorValue CreateValue(DateTimeOffset time)
		=> new MovingAverageConvergenceDivergenceHistogramValue(this, time);
}

/// <summary>
/// <see cref="MovingAverageConvergenceDivergenceHistogram"/> indicator value.
/// </summary>
public class MovingAverageConvergenceDivergenceHistogramValue : ComplexIndicatorValue<MovingAverageConvergenceDivergenceHistogram>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceHistogramValue"/>.
	/// </summary>
	/// <param name="indicator"><see cref="MovingAverageConvergenceDivergenceHistogram"/></param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public MovingAverageConvergenceDivergenceHistogramValue(MovingAverageConvergenceDivergenceHistogram indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Gets the MACD value.
	/// </summary>
	public decimal Macd => InnerValues[Indicator.Macd].ToDecimal();

	/// <summary>
	/// Gets the signal line value.
	/// </summary>
	public decimal Signal => InnerValues[Indicator.SignalMa].ToDecimal();
}
