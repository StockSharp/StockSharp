#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: IExternalCandleSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The external candles source (for example, connection <see cref="IConnector"/> which provides the possibility of ready candles getting).
	/// </summary>
	public interface IExternalCandleSource
	{
		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// Event of new candles occurring, that are received after the subscription by <see cref="SubscribeCandles"/>.
		/// </summary>
		event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		event Action<CandleSeries> Stopped;

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		void SubscribeCandles(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to);

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		void UnSubscribeCandles(CandleSeries series);
	}
}