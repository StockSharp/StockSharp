namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	/// <summary>
	/// Интерфейс контейнера, хранящего данные свечек.
	/// </summary>
	public interface ICandleManagerContainer : IDisposable
	{
		/// <summary>
		/// Время хранения свечек в памяти. По-умолчанию равно 2-ум дням.
		/// </summary>
		/// <remarks>Если значение установлено в <see cref="TimeSpan.Zero"/>, то свечи не будут удаляться.</remarks>
		TimeSpan CandlesKeepTime { get; set; }

		/// <summary>
		/// Известить контейнер для начале получения свечек для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой будут получаться свечи.</param>
		/// <param name="to">Конечная дата, до которой будут получаться свечи.</param>
		void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// Добавить свечу для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <returns><see langword="true"/>, если свеча не ранее добавлена, иначе, <see langword="false"/>.</returns>
		bool AddCandle(CandleSeries series, Candle candle);

		/// <summary>
		/// Получить для серии все ассоциированные с ней свечи на период <paramref name="time"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="time">Период свечи.</param>
		/// <returns>Свечи.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, DateTime time);

		/// <summary>
		/// Получить для серии все ассоциированные с ней свечи.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series);

		/// <summary>
		/// Получить свечу по индексу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleIndex">Порядковый номер свечи с конца.</param>
		/// <returns>Найденная свеча. Если свечи не существует, то будет возвращено null.</returns>
		Candle GetCandle(CandleSeries series, int candleIndex);

		/// <summary>
		/// Получить свечи по серии и диапазону дат.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="timeRange">Диапазон дат, в которые должны входить свечи. Учитывается значение <see cref="Candle.OpenTime"/>.</param>
		/// <returns>Найденные свечи.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, Range<DateTimeOffset> timeRange);

		/// <summary>
		/// Получить свечи по серии и общему количеству.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleCount">Количество свечек, которое необходимо вернуть.</param>
		/// <returns>Найденные свечи.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, int candleCount);

		/// <summary>
		/// Получить количество свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Количество свечек.</returns>
		int GetCandleCount(CandleSeries series);
	}
}