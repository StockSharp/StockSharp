namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Процентный диапазон Уильямса.
	/// </summary>
	/// <remarks>
	///  %R = (Highest High - Close)/(Highest High - Lowest Low) * -100
	/// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:williams_r
	/// http://www2.wealth-lab.com/WL5Wiki/WilliamsR.ashx
	/// </remarks>
	[DisplayName("%R")]
	[DescriptionLoc(LocalizedStrings.Str854Key)]
	public class WilliamsR : LengthIndicator<decimal>
	{
		// Текущее значение минимума
		private readonly Lowest _low;

		// Текущее значение максимума
		private readonly Highest _high;

		/// <summary>
		/// Создать <see cref="WilliamsR"/>.
		/// </summary>
		public WilliamsR()
		{
			_low = new Lowest();
			_high = new Highest();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _low.IsFormed; } }

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

			// Находим минимум и максимум для заданного периода
			var lowValue = _low.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();
			var highValue = _high.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();

			if ((highValue - lowValue) != 0)
				return new DecimalIndicatorValue(this, -100m * (highValue - candle.ClosePrice) / (highValue - lowValue));
				
			return new DecimalIndicatorValue(this);
		}
	}
}