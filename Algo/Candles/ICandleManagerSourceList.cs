namespace StockSharp.Algo.Candles
{
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// Коллекция источников свечек.
	/// </summary>
	public interface ICandleManagerSourceList : IList<ICandleManagerSource>, ISynchronizedCollection
	{
	}
}