namespace StockSharp.Algo.Candles
{
	/// <summary>
	/// Источник свечек для <see cref="ICandleManager"/>.
	/// </summary>
	public interface ICandleManagerSource : ICandleSource<Candle>
	{
		/// <summary>
		/// Менеджер свечек, которому принадлежит данный источник.
		/// </summary>
		ICandleManager CandleManager { get; set; }
	}
}