namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// The interfaces describes candle pattern.
/// </summary>
public interface ICandlePattern : IPersistable
{
	/// <summary>
	/// Name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Try recognize pattern.
	/// </summary>
	/// <param name="candles"><see cref="ICandleMessage"/>. Number of candles must be equal to <see cref="CandlesCount"/>.</param>
	/// <returns>Check result.</returns>
	bool Recognize(ReadOnlySpan<ICandleMessage> candles);

	/// <summary>
	/// Candles in pattern.
	/// </summary>
	int CandlesCount { get; }
}
