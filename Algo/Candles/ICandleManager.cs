namespace StockSharp.Algo.Candles
{
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс менеджера свечек.
	/// </summary>
	public interface ICandleManager : ICandleSource<Candle>
	{
		/// <summary>
		/// Kонтейнер данных.
		/// </summary>
		ICandleManagerContainer Container { get; }

		/// <summary>
		/// Все активные на текущий момент серии свечек, запущенные через <see cref="ICandleSource{T}.Start"/>.
		/// </summary>
		IEnumerable<CandleSeries> Series { get; }

		/// <summary>
		/// Источники свечек.
		/// </summary>
		ICandleManagerSourceList Sources { get; }
	}
}