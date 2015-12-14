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

	/// <summary>
	/// The candles builder interface.
	/// </summary>
	public interface ICandleBuilder : ICandleManagerSource
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		Type CandleType { get; }

		/// <summary>
		/// Data sources.
		/// </summary>
		ICandleBuilderSourceList Sources { get; }

		/// <summary>
		/// The data container.
		/// </summary>
		ICandleBuilderContainer Container { get; }

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		Candle ProcessValue(CandleSeries series, Candle currentCandle, ICandleBuilderSourceValue value);
	}
}