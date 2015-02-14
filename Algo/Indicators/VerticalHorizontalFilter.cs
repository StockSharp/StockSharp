namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Вертиально-горизонтальный фильтр.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/VHF.ashx
	/// http://www.stator-afm.com/vertical-horizontal-filter.html
	/// http://www.incrediblecharts.com/indicators/vertical_horizontal_filter.php
	/// </remarks>
	[DisplayName("VHF")]
	[DescriptionLoc(LocalizedStrings.Str754Key)]
	public class VerticalHorizontalFilter : LengthIndicator<decimal>
	{
		// Текущее значение минимума
		private readonly Lowest _min = new Lowest();

		// Текущее значение максимума
		private readonly Highest _max = new Highest();

		// Текущее значение суммы
		private readonly Sum _sum = new Sum();

		// Цена закрытия предыдущего бара
		private decimal? _previousClosePrice;

		/// <summary>
		/// Создать <see cref="VolumeWeightedMovingAverage"/>.
		/// </summary>
		public VerticalHorizontalFilter()
			: base(typeof(Candle))
		{
			Length = 15;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return _sum.IsFormed && _max.IsFormed && _min.IsFormed; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_sum.Length = _min.Length = _max.Length = Length;
			_previousClosePrice = null;
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
			var minValue = _min.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();
			var maxValue = _max.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();

			var sumValue = 0m;

			// Вычисляем сумму модулей разности цен закрытия текущего и предыдущего дня для заданного периода
			if (_previousClosePrice != null)
				sumValue = _sum.Process(input.SetValue(this, Math.Abs(_previousClosePrice.Value - candle.ClosePrice))).GetValue<decimal>();

			if (input.IsFinal)
				_previousClosePrice = candle.ClosePrice;

			if (!IsFormed)
				return new DecimalIndicatorValue(this);

			// Вычисляем значение индикатора
			if (sumValue != 0)
				return new DecimalIndicatorValue(this, ((maxValue - minValue) / sumValue));

			return new DecimalIndicatorValue(this);
		}
	}
}