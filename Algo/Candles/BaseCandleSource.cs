#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: BaseCandleSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// The base interface <see cref="ICandleSource{T}"/> implementation.
	/// </summary>
	/// <typeparam name="TValue">Data type.</typeparam>
	public abstract class BaseCandleSource<TValue> : Disposable, ICandleSource<TValue>
	{
		/// <summary>
		/// Initialize <see cref="BaseCandleSource{T}"/>.
		/// </summary>
		protected BaseCandleSource()
		{
		}

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public abstract int SpeedPriority { get; }

		/// <summary>
		/// A new value for processing occurrence event.
		/// </summary>
		public event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// The data transfer error event.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public abstract IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public abstract void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to);

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public abstract void Stop(CandleSeries series);

		/// <summary>
		/// To call the event <see cref="Processing"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="values">New data.</param>
		protected virtual void RaiseProcessing(CandleSeries series, TValue values)
		{
			Processing?.Invoke(series, values);
		}

		/// <summary>
		/// To call the event <see cref="Error"/>.
		/// </summary>
		/// <param name="error">Error details.</param>
		protected void RaiseError(Exception error)
		{
			Error?.Invoke(error);
		}

		/// <summary>
		/// To call the event <see cref="Stopped"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		protected void RaiseStopped(CandleSeries series)
		{
			Stopped?.Invoke(series);
		}
	}
}