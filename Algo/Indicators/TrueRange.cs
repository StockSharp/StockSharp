namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Истинный диапазон.
	/// </summary>
	[DisplayName("TR")]
	[DescriptionLoc(LocalizedStrings.Str775Key)]
	public class TrueRange : BaseIndicator<decimal>
	{
		private Candle _prevCandle;

		/// <summary>
		/// Создать <see cref="TrueRange"/>.
		/// </summary>
		public TrueRange()
			: base(typeof(Candle))
		{
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_prevCandle = null;
		}

		/// <summary>
		/// Получить компоненты цен для выбора максимального значения.
		/// </summary>
		/// <param name="currentCandle">Текущая свеча.</param>
		/// <param name="prevCandle">Предыдущая свеча.</param>
		/// <returns>Компоненты цен.</returns>
		protected virtual decimal[] GetPriceMovements(Candle currentCandle, Candle prevCandle)
		{
			return new[]
			{
				Math.Abs(currentCandle.HighPrice - currentCandle.LowPrice),
				Math.Abs(prevCandle.ClosePrice - currentCandle.HighPrice),
				Math.Abs(prevCandle.ClosePrice - currentCandle.LowPrice)
			};
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (_prevCandle != null)
			{
				IsFormed = true;

				var priceMovements = GetPriceMovements(candle, _prevCandle);

				if(input.IsFinal)
					_prevCandle = candle;

				return new DecimalIndicatorValue(this, priceMovements.Max());
			}

			if (input.IsFinal)
				_prevCandle = candle;

			return new DecimalIndicatorValue(this, candle.HighPrice - candle.LowPrice);
		}
	}
}