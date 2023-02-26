namespace StockSharp.Algo.Candles.Patterns;

using StockSharp.Localization;

/// <summary>
/// Registry of basic patterns.
/// </summary>
public static class CandlePatternRegistry
{
	private static ICandlePattern Create(string expression, string name)
		=> new CandlePattern { Expression = expression, Name = name };

	/// <summary>
	/// Flat candle pattern.
	/// </summary>
	public static readonly ICandlePattern Flat = Create("O == C", LocalizedStrings.FlatCandle);

	/// <summary>
	/// White candle pattern.
	/// </summary>
	public static readonly ICandlePattern White = Create("O < C", LocalizedStrings.WhiteCandle);

	/// <summary>
	/// Black candle pattern.
	/// </summary>
	public static readonly ICandlePattern Black = Create("O > C", LocalizedStrings.BlackCandle);

	/// <summary>
	/// Marubozu candle pattern.
	/// </summary>
	public static readonly ICandlePattern Marubozu = Create("LEN == B", LocalizedStrings.Marubozu);

	/// <summary>
	/// Spinning top candle pattern.
	/// </summary>
	public static readonly ICandlePattern SpinningTop = Create("(LEN != B) && (BS == TS)", LocalizedStrings.SpinningTop);

	/// <summary>
	/// Hammer candle pattern.
	/// </summary>
	public static readonly ICandlePattern Hammer = Create("(LEN != B) && (TS == 0)", LocalizedStrings.Hammer);

	/// <summary>
	/// Inverted hummer candle pattern.
	/// </summary>
	public static readonly ICandlePattern InvertedHammer = Create("(LEN != B) && (BS == 0)", LocalizedStrings.InvertedHammer);

	/// <summary>
	/// Dragonfly candle pattern.
	/// </summary>
	public static readonly ICandlePattern Dragonfly = Create("(O == C) && (TS == 0)", LocalizedStrings.Dragonfly);

	/// <summary>
	/// Gravestone candle pattern.
	/// </summary>
	public static readonly ICandlePattern Gravestone = Create("(O == C) && (BS == 0)", LocalizedStrings.Gravestone);

	/// <summary>
	/// Bullish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bullish = Create("(O < C) && (BS >= B)", LocalizedStrings.BullishCandle);

	/// <summary>
	/// Bearish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bearish = Create("(O > C) && (TS >= B)", LocalizedStrings.BearishCandle);
}