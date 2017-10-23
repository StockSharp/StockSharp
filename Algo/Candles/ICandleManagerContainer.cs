#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: ICandleManagerContainer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	/// <summary>
	/// The interface of the container that stores candles data.
	/// </summary>
	public interface ICandleManagerContainer : IDisposable
	{
		/// <summary>
		/// Candles storage time in memory. The default is 2 days.
		/// </summary>
		/// <remarks>
		/// If the value is set to <see cref="TimeSpan.Zero"/> then candles will not be deleted.
		/// </remarks>
		TimeSpan CandlesKeepTime { get; set; }

		/// <summary>
		/// To notify the container about the start of the candles getting for the series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which the candles will be get.</param>
		/// <param name="to">The final date by which the candles will be get.</param>
		void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to);

		/// <summary>
		/// To add a candle for the series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <returns><see langword="true" /> if the candle is not added previously, otherwise, <see langword="false" />.</returns>
		bool AddCandle(CandleSeries series, Candle candle);

		/// <summary>
		/// To get all associated with the series candles for the <paramref name="time" /> period.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="time">The candle period.</param>
		/// <returns>Candles.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, DateTimeOffset time);

		/// <summary>
		/// To get all associated with the series candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Candles.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series);

		/// <summary>
		/// To get a candle by the index.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candleIndex">The candle's position number from the end.</param>
		/// <returns>The found candle. If the candle does not exist, then <see langword="null" /> will be returned.</returns>
		Candle GetCandle(CandleSeries series, int candleIndex);

		/// <summary>
		/// To get candles by the series and date range.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="timeRange">The date range which should include candles. The <see cref="Candle.OpenTime"/> value is taken into consideration.</param>
		/// <returns>Found candles.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, Range<DateTimeOffset> timeRange);

		/// <summary>
		/// To get candles by the series and the total number.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candleCount">The number of candles that should be returned.</param>
		/// <returns>Found candles.</returns>
		IEnumerable<Candle> GetCandles(CandleSeries series, int candleCount);

		/// <summary>
		/// To get the number of candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Number of candles.</returns>
		int GetCandleCount(CandleSeries series);
	}
}