namespace StockSharp.Algo.Candles.Compression
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Базовый источник данных для <see cref="ICandleBuilder"/>.
	/// </summary>
	public abstract class BaseCandleBuilderSource : BaseCandleSource<IEnumerable<ICandleBuilderSourceValue>>, ICandleBuilderSource
	{
		/// <summary>
		/// Инициализировать <see cref="BaseCandleBuilderSource"/>.
		/// </summary>
		protected BaseCandleBuilderSource()
		{
		}

		/// <summary>
		/// Вызвать событие <see cref="BaseCandleSource{TValue}.Processing"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="values">Новые данные.</param>
		protected override void RaiseProcessing(CandleSeries series, IEnumerable<ICandleBuilderSourceValue> values)
		{
			base.RaiseProcessing(series, values.Where(v => series.CheckTime(v.Time)));
		}
	}
}