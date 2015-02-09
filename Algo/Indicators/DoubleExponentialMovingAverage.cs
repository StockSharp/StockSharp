namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	/// <summary>
	/// Double Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// ((2 * EMA) – EMA of EMA)
	/// </remarks>
	[DisplayName("DEMA")]
	[Description("Double Exponential Moving Average")]
	public class DoubleExponentialMovingAverage : LengthIndicator<decimal>
	{
		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;

		/// <summary>
		/// Создать <see cref="DoubleExponentialMovingAverage"/>.
		/// </summary>
		public DoubleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _ema1.IsFormed && _ema2.IsFormed; }
		}

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return _ema1.CanProcess(input) && _ema2.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (!_ema1.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema2Value = _ema2.Process(ema1Value);

			return new DecimalIndicatorValue(this, 2 * ema1Value.GetValue<decimal>() - ema2Value.GetValue<decimal>());
		}
	}
}
