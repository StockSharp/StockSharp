namespace StockSharp.Algo.Candles.Compression
{
	using Ecng.Collections;

	/// <summary>
	/// The data sources collection.
	/// </summary>
	public interface ICandleBuilderSourceList : INotifyList<ICandleBuilderSource>, ISynchronizedCollection
	{
	}
}