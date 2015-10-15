namespace StockSharp.Algo.Candles
{
	/// <summary>
	/// The candles source for <see cref="ICandleManager"/>.
	/// </summary>
	public interface ICandleManagerSource : ICandleSource<Candle>
	{
		/// <summary>
		/// The candles manager which owns this source.
		/// </summary>
		ICandleManager CandleManager { get; set; }
	}
}