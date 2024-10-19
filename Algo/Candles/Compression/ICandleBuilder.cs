namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// The candles builder interface.
/// </summary>
public interface ICandleBuilder : IDisposable
{
	/// <summary>
	/// The candle type.
	/// </summary>
	Type CandleType { get; }

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns>A new candles changes.</returns>
	IEnumerable<CandleMessage> Process(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform);
}