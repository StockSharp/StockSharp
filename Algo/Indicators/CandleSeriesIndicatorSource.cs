namespace StockSharp.Algo.Indicators
{
	using System;
	using StockSharp.Algo.Candles;

	/// <summary>
	/// Источник данных для индикаторов, которые строяться на основе объектов <see cref="Candle"/>, поступающих из <see cref="CandleSeries"/>.
	/// </summary>
	public class CandleSeriesIndicatorSource : BaseCandleIndicatorSource
	{
		private readonly CandleSeries _series;

		/// <summary>
		/// Создать <see cref="CandleManagerIndicatorSource"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public CandleSeriesIndicatorSource(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			_series = series;
			_series.ProcessCandle += OnProcessCandle;
		}

		/// <summary>
		/// Создать <see cref="CandleManagerIndicatorSource"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="getPart">Конвертер свечки, через которую можно получить ее параметр (цену закрытия <see cref="Candle.ClosePrice"/>, цену открытия <see cref="Candle.OpenPrice"/> и т.д.).</param>
		public CandleSeriesIndicatorSource(CandleSeries series, Func<Candle, decimal> getPart)
			: base(getPart)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			_series = series;
			_series.ProcessCandle += OnProcessCandle;
		}

		private void OnProcessCandle(Candle candle)
		{
			if (candle.State == CandleStates.Finished)
				NewCandle(candle);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_series.ProcessCandle -= OnProcessCandle;
			base.DisposeManaged();
		}

		/// <summary>
		/// Проверяет равен ли текущий источник переданному.
		/// </summary>
		/// <returns>true, если текущий источник равен <paramref name="other"/> в противном случае false.</returns>
		/// <param name="other">Источник для сравнения.</param>
		public override bool Equals(IIndicatorSource other)
		{
			return base.Equals(other) && _series.Equals(((CandleSeriesIndicatorSource)other)._series);
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ _series.GetHashCode();
			}
		}
	}
}