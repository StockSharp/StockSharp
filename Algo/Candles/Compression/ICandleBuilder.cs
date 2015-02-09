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
	}
}