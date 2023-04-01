using System.Collections.Generic;
using System.Linq;

using StockSharp.Localization;

namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Registry of basic patterns.
/// </summary>
public static class CandlePatternRegistry
{
	private static ExpressionCandlePattern Create(string name, params string[] expressions) => new(name, expressions.Select(e => new CandleExpressionCondition(e))) { IsRegistry = true };

	/// <summary>
	/// Flat candle pattern.
	/// </summary>
	public static readonly ICandlePattern Flat = Create(LocalizedStrings.FlatCandle, "O == C");

	/// <summary>
	/// White candle pattern.
	/// </summary>
	public static readonly ICandlePattern White = Create(LocalizedStrings.WhiteCandle, "O < C");

	/// <summary>
	/// Black candle pattern.
	/// </summary>
	public static readonly ICandlePattern Black = Create(LocalizedStrings.BlackCandle, "O > C");

	/// <summary>
	/// White Marubozu candle pattern.
	/// </summary>
	public static readonly ICandlePattern WhiteMarubozu = Create(LocalizedStrings.WhiteMarubozu, "(LEN == B) && (O < C)");

	/// <summary>
	/// Back Marubozu candle pattern.
	/// </summary>
	public static readonly ICandlePattern BlackMarubozu = Create(LocalizedStrings.BlackMarubozu, "(LEN == B) && (O > C)");

	/// <summary>
	/// Spinning top candle pattern.
	/// </summary>
	public static readonly ICandlePattern SpinningTop = Create(LocalizedStrings.SpinningTop, "(LEN != B) && (BS == TS)");

	/// <summary>
	/// Hammer candle pattern.
	/// </summary>
	public static readonly ICandlePattern Hammer = Create(LocalizedStrings.Hammer, "(O < C) && (LEN != B) && (TS == 0)");

	/// <summary>
	/// Inverted hummer candle pattern.
	/// </summary>
	public static readonly ICandlePattern InvertedHammer = Create(LocalizedStrings.InvertedHammer, "(O < C) && (LEN != B) && (BS == 0)");

	/// <summary>
	/// Dragonfly candle pattern.
	/// </summary>
	public static readonly ICandlePattern Dragonfly = Create(LocalizedStrings.Dragonfly, "(O == C) && (TS == 0)");

	/// <summary>
	/// Gravestone candle pattern.
	/// </summary>
	public static readonly ICandlePattern Gravestone = Create(LocalizedStrings.Gravestone, "(O == C) && (BS == 0)");

	/// <summary>
	/// Bullish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bullish = Create(LocalizedStrings.BullishCandle, "(O < C) && (BS >= B)");

	/// <summary>
	/// Bearish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bearish = Create(LocalizedStrings.BearishCandle, "(O > C) && (TS >= B)");

	/// <summary>
	/// Piercing candle pattern.
	/// </summary>
	public static readonly ICandlePattern Piercing = Create(LocalizedStrings.Piercing, "O > C", "(O < C) && (C > (pB / 2 + pC))");

	/// <summary>
	/// Bullish Engulfing candle pattern.
	/// </summary>
	public static readonly ICandlePattern BullishEngulfing = Create(LocalizedStrings.BullishEngulfing, "O > C", "(O < C) && (O < pC) && (C > pO)");

	/// <summary>
	/// Bearish Engulfing candle pattern.
	/// </summary>
	public static readonly ICandlePattern BearishEngulfing = Create(LocalizedStrings.BearishEngulfing, "O < C", "(O > C) && (O > pC) && (C < pO)");

	/// <summary>
	/// Morning Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern MorningStar = Create(LocalizedStrings.MorningStar, "O > C", "(O < C) && (LEN > B * 3)", "O < C");

	/// <summary>
	/// Evening Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern EveningStar = Create(LocalizedStrings.EveningStar, "O < C", "(O > C) && (LEN > B * 3)", "(O > C) && (LEN > pLEN * 3)");

	/// <summary>
	/// Three White Soldiers candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeWhiteSoldiers = Create(LocalizedStrings.ThreeWhiteSoldiers, "O < C", "(O < C) && (O > pO)", "(O < C) && (O > pO)");

	/// <summary>
	/// Three Black Crows candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeBlackCrows = Create(LocalizedStrings.ThreeBlackCrows, "O > C", "(O > C) && (O < pO)", "(O > C) && (O < pO)");

	/// <summary>
	/// Three Inside Up candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeInsideUp = Create(LocalizedStrings.ThreeInsideUp, "O > C", "(O < C) && (O > pC) && (C < pO)", "(O < C) && (O > pO) && (B > pB)");

	/// <summary>
	/// Three Inside Down candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeInsideDown = Create(LocalizedStrings.ThreeInsideDown, "O < C", "(O > C) && (O < pC) && (C > pO)", "(O > C) && (O < pO) && (B > pB)");

	/// <summary>
	/// Three Outside Up candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeOutsideUp = Create(LocalizedStrings.ThreeOutsideUp, "O > C", "(O < C) && (O < pC) && (C > pO)", "(O < C) && (O > pO) && (C > pC)");

	/// <summary>
	/// Three Outside Down candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeOutsideDown = Create(LocalizedStrings.ThreeOutsideDown, "O < C", "(O > C) && (O > pC) && (C < pO)", "(O > C) && (O < pO) && (C < pC)");

	/// <summary>
	/// Bullish Harami candle pattern.
	/// </summary>
	public static readonly ICandlePattern BullishHarami = Create(LocalizedStrings.BullishHarami, "O > C", "(O < C) && (O > pC) && (C < pO)");

	/// <summary>
	/// Bearish Harami candle pattern.
	/// </summary>
	public static readonly ICandlePattern BearishHarami = Create(LocalizedStrings.BearishHarami, "O < C", "(O > C) && (O < pC) && (C > pO)");

	/// <summary>
	/// On-Neck candle pattern.
	/// </summary>
	public static readonly ICandlePattern OnNeck = Create(LocalizedStrings.OnNeck, "O > C", "(C > O) && (pL == H)");

	/// <summary>
	/// Hanging man candle pattern.
	/// </summary>
	public static readonly ICandlePattern HangingMan = Create(LocalizedStrings.HangingMan, "(O > C) && (LEN != B) && (TS == 0)");

	/// <summary>
	/// Shooting Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern ShootingStar = Create(LocalizedStrings.ShootingStar, "(O > C) && (LEN != B) && (BS == 0)");

	/// <summary>
	/// Tweezer Top candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerTop = Create(LocalizedStrings.TweezerTop, "O < C", "(O > C) && (H == pH) && (B > (pB * 3))");

	/// <summary>
	/// Tweezer Bottom candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerBottom = Create(LocalizedStrings.TweezerBottom, "O > C", "(O < C) && (BS == 0) && (TS > B)");

	/// <summary>
	/// Falling Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern FallingThreeMethods = Create(LocalizedStrings.FallingThreeMethods, "O > C", "(O < C) && (B * 3 < pB)", "(O < C) && (B == pB)", "(O < C) && (B == pB)", "(O > C) && (B > pB * 3)");

	/// <summary>
	/// Rising Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern RisingThreeMethods = Create(LocalizedStrings.RisingThreeMethods, "O < C", "(O > C) && (B * 3 < pB)", "(O > C) && (B == pB)", "(O > C) && (B == pB)", "(O < C) && (B > pB * 3)");

	/// <summary>
	/// All patterns.
	/// </summary>
	public static IEnumerable<ICandlePattern> All => typeof(CandlePatternRegistry).GetFields().Select(p => p.GetValue(null)).OfType<ICandlePattern>();
}
