namespace StockSharp.Algo.Candles
{
	using System.Collections.Generic;

	/// <summary>
	/// The candles manager interface.
	/// </summary>
	public interface ICandleManager : ICandleSource<Candle>
	{
		/// <summary>
		/// The data container.
		/// </summary>
		ICandleManagerContainer Container { get; }

		/// <summary>
		/// All currently active candles series started via <see cref="ICandleSource{T}.Start"/>.
		/// </summary>
		IEnumerable<CandleSeries> Series { get; }

		/// <summary>
		/// Candles sources.
		/// </summary>
		ICandleManagerSourceList Sources { get; }
	}
}