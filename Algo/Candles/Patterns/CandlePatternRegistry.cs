namespace StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Registry of basic patterns.
/// </summary>
public static class CandlePatternRegistry
{
	private static ExpressionCandlePattern Create(string name, params string[] expressions) => new(name, expressions.Select(e => new CandleExpressionCondition(e)));

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
	public static readonly ICandlePattern SpinningTop = Create(LocalizedStrings.SpinningTop, "(B < LEN * 0.5m) && (ABS(BS - TS) <= LEN * 0.1m)");

	/// <summary>
	/// Hammer candle pattern.
	/// </summary>
	public static readonly ICandlePattern Hammer = Create(LocalizedStrings.Hammer, "(B <= BS) && (TS <= LEN * 0.2m)");

	/// <summary>
	/// Inverted hummer candle pattern.
	/// </summary>
	public static readonly ICandlePattern InvertedHammer = Create(LocalizedStrings.InvertedHammer, "(B <= TS) && (BS <= LEN * 0.2m)");

	/// <summary>
	/// Dragonfly candle pattern.
	/// </summary>
	public static readonly ICandlePattern Dragonfly = Create(LocalizedStrings.Dragonfly, "(ABS(O - C) <= LEN * 0.1m) && (TS <= LEN * 0.1m)");

	/// <summary>
	/// Gravestone candle pattern.
	/// </summary>
	public static readonly ICandlePattern Gravestone = Create(LocalizedStrings.Gravestone, "(ABS(O - C) <= LEN * 0.1m) && (BS <= LEN * 0.1m)");

	/// <summary>
	/// Bullish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bullish = Create(LocalizedStrings.BullishCandle, "(O < C)");

	/// <summary>
	/// Bearish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bearish = Create(LocalizedStrings.BearishCandle, "(O > C)");

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
	public static readonly ICandlePattern MorningStar = Create(LocalizedStrings.MorningStar, "O > C", "(B < pB * 0.5m)", "O < C");

	/// <summary>
	/// Evening Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern EveningStar = Create(LocalizedStrings.EveningStar, "O < C", "(B < pB * 0.5m)", "(O > C) && (B > pB * 2)");

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
	public static readonly ICandlePattern ThreeOutsideUp = Create(LocalizedStrings.ThreeOutsideUp, "O > C", "(O < C) && (O <= pC) && (C >= pO)", "(O < C) && (C > pC)");

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
	public static readonly ICandlePattern OnNeck = Create(LocalizedStrings.OnNeck, "O > C", "(C > O) && (ABS(pL - C) <= 1)");

	/// <summary>
	/// Hanging man candle pattern.
	/// </summary>
	public static readonly ICandlePattern HangingMan = Create(LocalizedStrings.HangingMan, "(B < BS * 0.5m) && (TS <= LEN * 0.2m)");

	/// <summary>
	/// Shooting Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern ShootingStar = Create(LocalizedStrings.ShootingStar, "(B < TS * 0.5m) && (BS <= LEN * 0.2m)");

	/// <summary>
	/// Tweezer Top candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerTop = Create(LocalizedStrings.TweezerTop, "O < C", "(O > C) && (H == pH)");

	/// <summary>
	/// Tweezer Bottom candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerBottom = Create(LocalizedStrings.TweezerBottom, "O > C", "(O < C) && (L == pL)");

	/// <summary>
	/// Falling Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern FallingThreeMethods = Create(LocalizedStrings.FallingThreeMethods, "O > C", "(O < C) && (B < pB * 0.5m)", "(O < C) && (B < ppB * 0.5m)", "(O < C) && (B < pppB * 0.5m)", "(O > C) && (B > pB * 2)");

	/// <summary>
	/// Rising Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern RisingThreeMethods = Create(LocalizedStrings.RisingThreeMethods, "O < C", "(O > C) && (B < pB * 0.5m)", "(O > C) && (B < ppB * 0.5m)", "(O > C) && (B < pppB * 0.5m)", "(O < C) && (B > pB * 2)");

	/// <summary>
	/// All patterns.
	/// </summary>
	public static IEnumerable<ICandlePattern> All => typeof(CandlePatternRegistry).GetFields().Select(p => p.GetValue(null)).OfType<ICandlePattern>();
}
