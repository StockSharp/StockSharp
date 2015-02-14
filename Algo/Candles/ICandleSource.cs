namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	/// <summary>
	/// Источник данных.
	/// </summary>
	/// <typeparam name="TValue">Тип данных.</typeparam>
	public interface ICandleSource<TValue> : IDisposable
	{
		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		int SpeedPriority { get; }

		/// <summary>
		/// Событие появления нового значения для обработки.
		/// </summary>
		event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		event Action<CandleSeries> Stopped;

		/// <summary>
		/// Событие ошибки транслирования данных.
		/// </summary>
		event Action<Exception> ProcessDataError;

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// Запросить получение данных.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать данные.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		void Stop(CandleSeries series);
	}
}