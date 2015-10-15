namespace StockSharp.Algo.Candles
{
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// The candles sources collection.
	/// </summary>
	public interface ICandleManagerSourceList : IList<ICandleManagerSource>, ISynchronizedCollection
	{
	}
}