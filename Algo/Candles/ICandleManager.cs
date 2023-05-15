namespace StockSharp.Algo.Candles;

using System;
using System.Collections.Generic;

using StockSharp.Messages;

/// <summary>
/// The candles manager interface.
/// </summary>
/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
public interface ICandleManager<TCandle> : IDisposable
	where TCandle : ICandleMessage
{
	/// <summary>
	/// A new value for processing occurrence event.
	/// </summary>
	event Action<CandleSeries, TCandle> Processing;

	/// <summary>
	/// The series processing end event.
	/// </summary>
	event Action<CandleSeries> Stopped;

	/// <summary>
	/// All currently active candles series started via <see cref="Start"/>.
	/// </summary>
	IEnumerable<CandleSeries> Series { get; }

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