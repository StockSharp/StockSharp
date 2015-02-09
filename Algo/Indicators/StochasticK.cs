namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Стохастик %K.
	/// </summary>
	[DisplayName("Stochastic %K")]
	[DescriptionLoc(LocalizedStrings.Str774Key)]
	public class StochasticK : LengthIndicator<decimal>
	{
		// Минимальная цена за период.
		private readonly Lowest _low = new Lowest();

		// Максимальная цена за период.
		private readonly Highest _high = new Highest();

		/// <summary>
		/// Создать <see cref="StochasticK"/>.
		/// </summary>
		public StochasticK()
			: base(typeof(Candle))
		{
			Length = 14;
		}

		/// <summary>
		/// Индикатор сформирован.
		/// </summary>
		public override bool IsFormed { get { return _high.IsFormed; } }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_high.Length = _low.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			var highValue = _high.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();
			var lowValue = _low.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();

			var diff = highValue - lowValue;

			if (diff == 0)
				return new DecimalIndicatorValue(this, 0);

			return new DecimalIndicatorValue(this, 100 * (candle.ClosePrice - lowValue) / diff);
		}
	}
}