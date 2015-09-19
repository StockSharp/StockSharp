namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Корреляция.
	/// </summary>
	/// <remarks>
	/// https://en.wikipedia.org/wiki/Correlation_and_dependence
	/// </remarks>
	[DisplayName("COR")]
	[DescriptionLoc(LocalizedStrings.CorrelationKey, true)]
	public class Correlation : Covariance
	{
		private readonly StandardDeviation _source;
		private readonly StandardDeviation _other;

		/// <summary>
		/// Создать <see cref="Correlation"/>.
		/// </summary>
		public Correlation()
		{
			_source = new StandardDeviation();
			_other = new StandardDeviation();

			Length = 20;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_source.Length = _other.Length = Length;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var cov = base.OnProcess(input);

			var value = input.GetValue<Tuple<decimal, decimal>>();

			var sourceDev = _source.Process(value.Item1);
			var otherDev = _other.Process(value.Item2);

			return new DecimalIndicatorValue(this, cov.GetValue<decimal>() / (sourceDev.GetValue<decimal>() * otherDev.GetValue<decimal>()));
		}
	}
}