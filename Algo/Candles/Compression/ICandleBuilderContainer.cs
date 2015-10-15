namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The interface of the container storing data.
	/// </summary>
	public interface ICandleBuilderContainer : IDisposable
	{
		/// <summary>
		/// The time of <see cref="ICandleBuilderSourceValue"/> storage in memory. The default is zero (no storage).
		/// </summary>
		TimeSpan ValuesKeepTime { get; set; }

		/// <summary>
		/// To notify the container about the start of the data getting for the series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which data will be get.</param>
		/// <param name="to">The final date by which data will be get.</param>
		void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// To add data for the candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">The candle for which you need to add data.</param>
		/// <param name="value">New data.</param>
		void AddValue(CandleSeries series, Candle candle, ICandleBuilderSourceValue value);

		/// <summary>
		/// To get all data by the candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">The candle for which you need to find data.</param>
		/// <returns>Found data.</returns>
		IEnumerable<ICandleBuilderSourceValue> GetValues(CandleSeries series, Candle candle);
	}
}