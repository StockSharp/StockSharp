namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Схождение/расхождение скользящих средних. Гистограмма.
	/// </summary>
	[DisplayName("MACD Histogram")]
	[DescriptionLoc(LocalizedStrings.Str802Key)]
	public class MovingAverageConvergenceDivergenceHistogram : MovingAverageConvergenceDivergenceSignal
	{
		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
		/// </summary>
		public MovingAverageConvergenceDivergenceHistogram()
		{
		}

		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergenceHistogram"/>.
		/// </summary>
		/// <param name="macd">Схождение/расхождение скользящих средних.</param>
		/// <param name="signalMa">Сигнальная скользящая средняя.</param>
		public MovingAverageConvergenceDivergenceHistogram(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
			: base(macd, signalMa)
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
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