namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Convergence/divergence of moving averages. Histogram.
	/// </summary>
	[DisplayName("MACD Histogram")]
	[DescriptionLoc(LocalizedStrings.Str802Key)]
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
		/// <param name="signalMa">Signalling Voving Average.</param>
		public MovingAverageConvergenceDivergenceHistogram(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
			: base(macd, signalMa)
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var macdValue = Macd.Process(input);
			var signalValue = Macd.IsFormed ? SignalMa.Process(macdValue) : new DecimalIndicatorValue(this, 0);

			var value = new ComplexIndicatorValue(this);
			value.InnerValues.Add(Macd, input.SetValue(this, macdValue.GetValue<decimal>() - signalValue.GetValue<decimal>()));
			value.InnerValues.Add(SignalMa, signalValue);
			return value;
		}
	}
}