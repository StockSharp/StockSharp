#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: ICandleSource.cs
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
	/// Market-data source.
	/// </summary>
	/// <typeparam name="TValue">Data type.</typeparam>
	public interface ICandleSource<TValue> : IDisposable
	{
		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		int SpeedPriority { get; }

		/// <summary>
		/// A new value for processing occurrence event.
		/// </summary>
		event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		event Action<CandleSeries> Stopped;

		/// <summary>
		/// The data transfer error event.
		/// </summary>
		event Action<Exception> Error;

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to);

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		void Stop(CandleSeries series);
	}
}