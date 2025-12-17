namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// The interface for storage processor.
/// </summary>
public interface IStorageProcessor
{
	/// <summary>
	/// Storage settings.
	/// </summary>
	StorageCoreSettings Settings { get; }

	/// <summary>
	/// Candle builders provider.
	/// </summary>
	CandleBuilderProvider CandleBuilderProvider { get; }

	/// <summary>
	/// To reset the state.
	/// </summary>
	void Reset();

	/// <summary>
	/// Process <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="newOutMessage">New message event.</param>
	/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
	MarketDataMessage ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage);
}