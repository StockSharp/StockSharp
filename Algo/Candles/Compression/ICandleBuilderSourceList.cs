namespace StockSharp.Algo.Candles.Compression
{
	using Ecng.Collections;

	/// <summary>
	/// Коллекция источников данных.
	/// </summary>
	public interface ICandleBuilderSourceList : INotifyList<ICandleBuilderSource>, ISynchronizedCollection
	{
	}
}