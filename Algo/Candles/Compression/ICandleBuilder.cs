namespace StockSharp.Algo.Candles.Compression
{
	using System;

	/// <summary>
	/// Интерфейс построителя свечек.
	/// </summary>
	public interface ICandleBuilder : ICandleManagerSource
	{
		/// <summary>
		/// Тип свечи.
		/// </summary>
		Type CandleType { get; }

		/// <summary>
		/// Источники данных.
		/// </summary>
		ICandleBuilderSourceList Sources { get; }

		/// <summary>
		/// Kонтейнер данных.
		/// </summary>
		ICandleBuilderContainer Container { get; }

		/// <summary>
		/// Обработать новые данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="currentCandle">Текущая свеча.</param>
		/// <param name="value">Новые данные, с помощью которых принимается решение о необходимости начала или окончания формирования текущей свечи.</param>
		/// <returns>Новая свеча. Если новую свечу нет необходимости создавать, то возвращается <paramref name="currentCandle"/>.
		/// Если новую свечу создать невозможно (<paramref name="value"/> не может быть применено к свечам), то возвращается <see langword="null"/>.</returns>
		Candle ProcessValue(CandleSeries series, Candle currentCandle, ICandleBuilderSourceValue value);
	}
}