namespace StockSharp.Algo.Candles.Patterns;

using Ecng.Serialization;

using StockSharp.Messages;

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
	/// Reset.
	/// </summary>
	void Reset();

	/// <summary>
	/// Try recognize pattern.
	/// </summary>
	/// <param name="candle"><see cref="ICandleMessage"/>.</param>
	/// <returns>Check result.</returns>
	bool Recognize(ICandleMessage candle);

	/// <summary>
	/// Candles in pattern.
	/// </summary>
	int CandlesCount { get; }

	/// <summary>
	/// Validate settings.
	/// </summary>
	void Validate();
}