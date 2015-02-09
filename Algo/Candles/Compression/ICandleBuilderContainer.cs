namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс контейнера, хранящего данные.
	/// </summary>
	public interface ICandleBuilderContainer : IDisposable
	{
		/// <summary>
		/// Время хранения <see cref="ICandleBuilderSourceValue"/> в памяти. По-умолчанию равно нулю (хранение отсутствует).
		/// </summary>
		TimeSpan ValuesKeepTime { get; set; }

		/// <summary>
		/// Известить контейнер для начале получения данных для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой будут получаться данные.</param>
		/// <param name="to">Конечная дата, до которой будут получаться данные.</param>
		void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// Добавить данные для свечи.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча, для которой нужно добавить данные.</param>
		/// <param name="value">Новые данные.</param>
		void AddValue(CandleSeries series, Candle candle, ICandleBuilderSourceValue value);

		/// <summary>
		/// Получить все данные по свече.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча, по которой нужно найти данные.</param>
		/// <returns>Найденные данные.</returns>
		IEnumerable<ICandleBuilderSourceValue> GetValues(CandleSeries series, Candle candle);
	}
}