namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// The interfaces describes candle pattern.
/// </summary>
public interface ICandlePattern : IAsyncPersistable
{
	/// <summary>
	/// Name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Prepare the pattern for <see cref="Recognize"/>: compile what it is made of. Repeated calls do nothing.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask PrepareAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Try recognize pattern. The pattern has to be prepared by <see cref="PrepareAsync"/>.
	/// </summary>
	/// <param name="candles"><see cref="ICandleMessage"/>. Number of candles must be equal to <see cref="CandlesCount"/>.</param>
	/// <returns>Check result.</returns>
	bool Recognize(ReadOnlySpan<ICandleMessage> candles);

	/// <summary>
	/// Candles in pattern.
	/// </summary>
	int CandlesCount { get; }
}
