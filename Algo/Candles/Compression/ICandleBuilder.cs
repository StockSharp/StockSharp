#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: ICandleBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// The candles builder interface.
	/// </summary>
	public interface ICandleBuilder : IDisposable
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		MarketDataTypes CandleType { get; }

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="transform">The data source transformation.</param>
		/// <returns>A new candles changes.</returns>
		IEnumerable<CandleMessage> Process(MarketDataMessage message, CandleMessage currentCandle, ICandleBuilderValueTransform transform);

		///// <summary>
		///// Reset state.
		///// </summary>
		//void Reset();
	}
}