namespace StockSharp.Algo.Candles.Patterns;

using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Localization;

/// <summary>
/// Registry of basic patterns.
/// </summary>
public static class CandlePatternRegistry
{
	private static ComplexCandlePattern Create(params string[] expressions)
	{
		//if (expressions.Length == 1)
		//	return new CandlePattern { Expression = expressions[0] };

		var comples = new ComplexCandlePattern();
		comples.Inner.AddRange(expressions.Select(e => new CandlePattern { Expression = e }));
		return comples;
	}

	private static ComplexCandlePattern Name(this ComplexCandlePattern pattern, string name)
	{
		pattern.Name = name;
		return pattern;
	}

	/// <summary>
	/// Flat candle pattern.
	/// </summary>
	public static readonly ICandlePattern Flat = Create("O == C").Name(LocalizedStrings.FlatCandle);

	/// <summary>
	/// White candle pattern.
	/// </summary>
	public static readonly ICandlePattern White = Create("O < C").Name(LocalizedStrings.WhiteCandle);

	/// <summary>
	/// Black candle pattern.
	/// </summary>
	public static readonly ICandlePattern Black = Create("O > C").Name(LocalizedStrings.BlackCandle);

	/// <summary>
	/// White Marubozu candle pattern.
	/// </summary>
	public static readonly ICandlePattern WhiteMarubozu = Create("(LEN == B) && (O < C)").Name(LocalizedStrings.WhiteMarubozu);

	/// <summary>
	/// Back Marubozu candle pattern.
	/// </summary>
	public static readonly ICandlePattern BlackMarubozu = Create("(LEN == B) && (O > C)").Name(LocalizedStrings.BlackMarubozu);

	/// <summary>
	/// Spinning top candle pattern.
	/// </summary>
	public static readonly ICandlePattern SpinningTop = Create("(LEN != B) && (BS == TS)").Name(LocalizedStrings.SpinningTop);

	/// <summary>
	/// Hammer candle pattern.
	/// </summary>
	public static readonly ICandlePattern Hammer = Create("(O < C) && (LEN != B) && (TS == 0)").Name(LocalizedStrings.Hammer);

	/// <summary>
	/// Inverted hummer candle pattern.
	/// </summary>
	public static readonly ICandlePattern InvertedHammer = Create("(O < C) && (LEN != B) && (BS == 0)").Name(LocalizedStrings.InvertedHammer);

	/// <summary>
	/// Dragonfly candle pattern.
	/// </summary>
	public static readonly ICandlePattern Dragonfly = Create("(O == C) && (TS == 0)").Name(LocalizedStrings.Dragonfly);

	/// <summary>
	/// Gravestone candle pattern.
	/// </summary>
	public static readonly ICandlePattern Gravestone = Create("(O == C) && (BS == 0)").Name(LocalizedStrings.Gravestone);

	/// <summary>
	/// Bullish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bullish = Create("(O < C) && (BS >= B)").Name(LocalizedStrings.BullishCandle);

	/// <summary>
	/// Bearish candle pattern.
	/// </summary>
	public static readonly ICandlePattern Bearish = Create("(O > C) && (TS >= B)").Name(LocalizedStrings.BearishCandle);

	/// <summary>
	/// Piercing candle pattern.
	/// </summary>
	public static readonly ICandlePattern Piercing = Create("O > C", "(O < C) && (C > (pB / 2 + pC))").Name(LocalizedStrings.Piercing);

	/// <summary>
	/// Bullish Engulfing candle pattern.
	/// </summary>
	public static readonly ICandlePattern BullishEngulfing = Create("O > C", "(O < C) && (O < pC) && (C > pO)").Name(LocalizedStrings.BullishEngulfing);

	/// <summary>
	/// Bearish Engulfing candle pattern.
	/// </summary>
	public static readonly ICandlePattern BearishEngulfing = Create("O < C", "(O > C) && (O > pC) && (C < pO)").Name(LocalizedStrings.BearishEngulfing);

	/// <summary>
	/// Morning Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern MorningStar = Create("O > C", "(O < C) && (LEN > B * 3)", "O < C").Name(LocalizedStrings.MorningStar);

	/// <summary>
	/// Evening Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern EveningStar = Create("O < C", "(O > C) && (LEN > B * 3)", "(O > C) && (LEN > pLEN * 3)").Name(LocalizedStrings.EveningStar);

	/// <summary>
	/// Three White Soldiers candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeWhiteSoldiers = Create("O < C", "(O < C) && (O > pO)", "(O < C) && (O > pO)").Name(LocalizedStrings.ThreeWhiteSoldiers);

	/// <summary>
	/// Three Black Crows candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeBlackCrows = Create("O > C", "(O > C) && (O < pO)", "(O > C) && (O < pO)").Name(LocalizedStrings.ThreeBlackCrows);

	/// <summary>
	/// Three Inside Up candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeInsideUp = Create("O > C", "(O < C) && (O > pC) && (C < pO)", "(O < C) && (O > pO) && (B > pB)").Name(LocalizedStrings.ThreeInsideUp);

	/// <summary>
	/// Three Inside Down candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeInsideDown = Create("O < C", "(O > C) && (O < pC) && (C > pO)", "(O > C) && (O < pO) && (B > pB)").Name(LocalizedStrings.ThreeInsideDown);

	/// <summary>
	/// Three Outside Up candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeOutsideUp = Create("O > C", "(O < C) && (O < pC) && (C > pO)", "(O < C) && (O > pO) && (C > pC)").Name(LocalizedStrings.ThreeOutsideUp);

	/// <summary>
	/// Three Outside Down candle pattern.
	/// </summary>
	public static readonly ICandlePattern ThreeOutsideDown = Create("O < C", "(O > C) && (O > pC) && (C < pO)", "(O > C) && (O < pO) && (C < pC)").Name(LocalizedStrings.ThreeOutsideDown);

	/// <summary>
	/// Bullish Harami candle pattern.
	/// </summary>
	public static readonly ICandlePattern BullishHarami = Create("O > C", "(O < C) && (O > pC) && (C < pO)").Name(LocalizedStrings.BullishHarami);

	/// <summary>
	/// Bearish Harami candle pattern.
	/// </summary>
	public static readonly ICandlePattern BearishHarami = Create("O < C", "(O > C) && (O < pC) && (C > pO)").Name(LocalizedStrings.BearishHarami);

	/// <summary>
	/// On-Neck candle pattern.
	/// </summary>
	public static readonly ICandlePattern OnNeck = Create("O > C", "(C > O) && (pL == H)").Name(LocalizedStrings.OnNeck);

	/// <summary>
	/// Hanging man candle pattern.
	/// </summary>
	public static readonly ICandlePattern HangingMan = Create("(O > C) && (LEN != B) && (TS == 0)").Name(LocalizedStrings.HangingMan);

	/// <summary>
	/// Shooting Star candle pattern.
	/// </summary>
	public static readonly ICandlePattern ShootingStar = Create("(O > C) && (LEN != B) && (BS == 0)").Name(LocalizedStrings.ShootingStar);

	/// <summary>
	/// Tweezer Top candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerTop = Create("O < C", "(O > C) && (H == pH) && (B > (pB * 3))").Name(LocalizedStrings.TweezerTop);

	/// <summary>
	/// Tweezer Bottom candle pattern.
	/// </summary>
	public static readonly ICandlePattern TweezerBottom = Create("O > C", "(O < C) && (BS == 0) && (TS > B)").Name(LocalizedStrings.TweezerBottom);

	/// <summary>
	/// Falling Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern FallingThreeMethods = Create("O > C", "(O < C) && (B * 3 < pB)", "(O < C) && (B == pB)", "(O < C) && (B == pB)", "(O > C) && (B > pB * 3)").Name(LocalizedStrings.FallingThreeMethods);

	/// <summary>
	/// Rising Three Methods candle pattern.
	/// </summary>
	public static readonly ICandlePattern RisingThreeMethods = Create("O < C", "(O > C) && (B * 3 < pB)", "(O > C) && (B == pB)", "(O > C) && (B == pB)", "(O < C) && (B > pB * 3)").Name(LocalizedStrings.RisingThreeMethods);

	/// <summary>
	/// All patterns.
	/// </summary>
	public static IEnumerable<ICandlePattern> All => typeof(CandlePatternRegistry).GetFields().Select(p => p.GetValue(null)).OfType<ICandlePattern>();
}