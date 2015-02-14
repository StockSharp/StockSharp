namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Внешний источник свечек (например, подключение <see cref="IConnector"/>, предоставляющее возможность получения готовых свечек).
	/// </summary>
	public interface IExternalCandleSource
	{
		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles"/>.
		/// </summary>
		event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		void UnSubscribeCandles(CandleSeries series);
	}
}