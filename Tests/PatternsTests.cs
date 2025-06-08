namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Patterns;

[TestClass]
public class PatternsTests
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();
	private static readonly DataType _dt = TimeSpan.FromMinutes(1).TimeFrame();

	private static TimeFrameCandleMessage CreateCandle(decimal open, decimal high, decimal low, decimal close, decimal volume = 1000m)
	{
		return new()
		{
			OpenPrice = open,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = close,
			TotalVolume = volume,
			OpenTime = DateTimeOffset.UtcNow,
			SecurityId = _secId,
			DataType = _dt
		};
	}

	private static void TestPattern(ICandlePattern pattern, CandleMessage[] candles, bool expectedResult)
	{
		var result = pattern.Recognize(candles);
		result.AssertEqual(expectedResult);
	}

	[TestMethod]
	public void Flat()
	{
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);

		TestPattern(CandlePatternRegistry.Flat, [flatCandle], true);
		TestPattern(CandlePatternRegistry.Flat, [whiteCandle], false);
	}

	[TestMethod]
	public void White()
	{
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);
		var blackCandle = CreateCandle(100m, 105m, 95m, 97m);
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);

		TestPattern(CandlePatternRegistry.White, [whiteCandle], true);
		TestPattern(CandlePatternRegistry.White, [blackCandle], false);
		TestPattern(CandlePatternRegistry.White, [flatCandle], false);
	}

	[TestMethod]
	public void Black()
	{
		var blackCandle = CreateCandle(100m, 105m, 95m, 97m);
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);

		TestPattern(CandlePatternRegistry.Black, [blackCandle], true);
		TestPattern(CandlePatternRegistry.Black, [whiteCandle], false);
		TestPattern(CandlePatternRegistry.Black, [flatCandle], false);
	}

	[TestMethod]
	public void WhiteMarubozu()
	{
		var whiteMarubozu = CreateCandle(100m, 110m, 100m, 110m); // No shadows
		var whiteWithShadows = CreateCandle(100m, 115m, 95m, 110m); // With shadows
		var blackMarubozu = CreateCandle(110m, 110m, 100m, 100m);

		TestPattern(CandlePatternRegistry.WhiteMarubozu, [whiteMarubozu], true);
		TestPattern(CandlePatternRegistry.WhiteMarubozu, [whiteWithShadows], false);
		TestPattern(CandlePatternRegistry.WhiteMarubozu, [blackMarubozu], false);
	}

	[TestMethod]
	public void BlackMarubozu()
	{
		var blackMarubozu = CreateCandle(110m, 110m, 100m, 100m); // No shadows
		var blackWithShadows = CreateCandle(110m, 115m, 95m, 100m); // With shadows
		var whiteMarubozu = CreateCandle(100m, 110m, 100m, 110m);

		TestPattern(CandlePatternRegistry.BlackMarubozu, [blackMarubozu], true);
		TestPattern(CandlePatternRegistry.BlackMarubozu, [blackWithShadows], false);
		TestPattern(CandlePatternRegistry.BlackMarubozu, [whiteMarubozu], false);
	}

	[TestMethod]
	public void SpinningTop()
	{
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m); // Small body, equal shadows
		var marubozu = CreateCandle(100m, 110m, 100m, 110m); // No shadows
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow only

		TestPattern(CandlePatternRegistry.SpinningTop, [spinningTop], true);
		TestPattern(CandlePatternRegistry.SpinningTop, [marubozu], false);
		TestPattern(CandlePatternRegistry.SpinningTop, [hammer], false);
	}

	[TestMethod]
	public void Hammer()
	{
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow, no top shadow
		var invertedHammer = CreateCandle(100m, 110m, 95m, 105m); // Long top shadow, no bottom shadow
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m);

		TestPattern(CandlePatternRegistry.Hammer, [hammer], true);
		TestPattern(CandlePatternRegistry.Hammer, [invertedHammer], false);
		TestPattern(CandlePatternRegistry.Hammer, [spinningTop], false);
	}

	[TestMethod]
	public void InvertedHammer()
	{
		var invertedHammer = CreateCandle(100m, 110m, 100m, 105m); // Long top shadow, no bottom shadow
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m);

		TestPattern(CandlePatternRegistry.InvertedHammer, [invertedHammer], true);
		TestPattern(CandlePatternRegistry.InvertedHammer, [hammer], false);
		TestPattern(CandlePatternRegistry.InvertedHammer, [spinningTop], false);
	}

	[TestMethod]
	public void Dragonfly()
	{
		var dragonfly = CreateCandle(100m, 100m, 90m, 100m); // Flat, long bottom shadow
		var gravestone = CreateCandle(100m, 110m, 100m, 100m); // Flat, long top shadow
		var normalCandle = CreateCandle(100m, 105m, 95m, 103m);

		TestPattern(CandlePatternRegistry.Dragonfly, [dragonfly], true);
		TestPattern(CandlePatternRegistry.Dragonfly, [gravestone], false);
		TestPattern(CandlePatternRegistry.Dragonfly, [normalCandle], false);
	}

	[TestMethod]
	public void Gravestone()
	{
		var gravestone = CreateCandle(100m, 110m, 100m, 100m); // Flat, long top shadow
		var dragonfly = CreateCandle(100m, 100m, 90m, 100m); // Flat, long bottom shadow
		var normalCandle = CreateCandle(100m, 105m, 95m, 103m);

		TestPattern(CandlePatternRegistry.Gravestone, [gravestone], true);
		TestPattern(CandlePatternRegistry.Gravestone, [dragonfly], false);
		TestPattern(CandlePatternRegistry.Gravestone, [normalCandle], false);
	}

	[TestMethod]
	public void Bullish()
	{
		var bullish = CreateCandle(100m, 110m, 100m, 107m); // White candle with small/no bottom shadow
		var bearish = CreateCandle(107m, 107m, 90m, 100m); // Black candle
		var weakBullish = CreateCandle(100m, 110m, 80m, 107m); // White but large bottom shadow

		TestPattern(CandlePatternRegistry.Bullish, [bullish], true);
		TestPattern(CandlePatternRegistry.Bullish, [bearish], false);
		TestPattern(CandlePatternRegistry.Bullish, [weakBullish], true);
	}

	[TestMethod]
	public void Bearish()
	{
		var bearish = CreateCandle(107m, 107m, 90m, 100m); // Black candle with small/no top shadow
		var bullish = CreateCandle(100m, 110m, 100m, 107m); // White candle
		var weakBearish = CreateCandle(107m, 120m, 90m, 100m); // Black but large top shadow

		TestPattern(CandlePatternRegistry.Bearish, [bearish], true);
		TestPattern(CandlePatternRegistry.Bearish, [bullish], false);
		TestPattern(CandlePatternRegistry.Bearish, [weakBearish], true);
	}

	[TestMethod]
	public void Piercing()
	{
		var firstCandle = CreateCandle(110m, 110m, 95m, 100m); // Black candle
		var secondCandle = CreateCandle(98m, 108m, 95m, 106m); // White candle closing above midpoint

		var invalidFirst = CreateCandle(100m, 110m, 95m, 105m); // White candle
		var invalidSecond = CreateCandle(98m, 108m, 95m, 102m); // Doesn't close above midpoint

		TestPattern(CandlePatternRegistry.Piercing, [firstCandle, secondCandle], true);
		TestPattern(CandlePatternRegistry.Piercing, [invalidFirst, secondCandle], false);
		TestPattern(CandlePatternRegistry.Piercing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public void BullishEngulfing()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 105m); // Small black candle
		var secondCandle = CreateCandle(102m, 115m, 98m, 112m); // Large white candle engulfing first

		var invalidFirst = CreateCandle(100m, 110m, 100m, 105m); // White candle
		var invalidSecond = CreateCandle(106m, 115m, 98m, 112m); // Doesn't engulf

		TestPattern(CandlePatternRegistry.BullishEngulfing, [firstCandle, secondCandle], true);
		TestPattern(CandlePatternRegistry.BullishEngulfing, [invalidFirst, secondCandle], false);
		TestPattern(CandlePatternRegistry.BullishEngulfing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public void BearishEngulfing()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 105m); // Small white candle
		var secondCandle = CreateCandle(108m, 108m, 95m, 98m); // Large black candle engulfing first

		var invalidFirst = CreateCandle(110m, 110m, 100m, 105m); // Black candle
		var invalidSecond = CreateCandle(102m, 108m, 95m, 98m); // Doesn't engulf

		TestPattern(CandlePatternRegistry.BearishEngulfing, [firstCandle, secondCandle], true);
		TestPattern(CandlePatternRegistry.BearishEngulfing, [invalidFirst, secondCandle], false);
		TestPattern(CandlePatternRegistry.BearishEngulfing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public void MorningStar()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var secondCandle = CreateCandle(101m, 103m, 99m, 100m); // Small body (star)
		var thirdCandle = CreateCandle(101m, 115m, 101m, 112m); // White candle

		var invalidFirst = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var invalidSecond = CreateCandle(101m, 115m, 95m, 110m); // Large body
		var invalidThird = CreateCandle(110m, 115m, 101m, 105m); // Black candle

		TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, secondCandle, thirdCandle], true);
		TestPattern(CandlePatternRegistry.MorningStar, [invalidFirst, secondCandle, thirdCandle], false);
		TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, invalidSecond, thirdCandle], false);
		TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public void EveningStar()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var secondCandle = CreateCandle(109m, 111m, 107m, 110m); // Small body (star)
		var thirdCandle = CreateCandle(109m, 109m, 95m, 102m); // Black candle

		var invalidFirst = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var invalidSecond = CreateCandle(109m, 125m, 95m, 120m); // Large body
		var invalidThird = CreateCandle(109m, 115m, 105m, 112m); // White candle

		TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, secondCandle, thirdCandle], true);
		TestPattern(CandlePatternRegistry.EveningStar, [invalidFirst, secondCandle, thirdCandle], false);
		TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, invalidSecond, thirdCandle], false);
		TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public void ThreeWhiteSoldiers()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var secondCandle = CreateCandle(106m, 115m, 105m, 113m); // White candle opening above previous open
		var thirdCandle = CreateCandle(111m, 120m, 110m, 118m); // White candle opening above previous open

		var invalidFirst = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var invalidSecond = CreateCandle(98m, 115m, 95m, 113m); // Opens below previous open
		var invalidThird = CreateCandle(105m, 120m, 105m, 118m); // Opens below previous open

		TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, secondCandle, thirdCandle], true);
		TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [invalidFirst, secondCandle, thirdCandle], false);
		TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, invalidSecond, thirdCandle], false);
		TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public void ThreeBlackCrows()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var secondCandle = CreateCandle(104m, 104m, 95m, 97m); // Black candle opening below previous open
		var thirdCandle = CreateCandle(99m, 99m, 90m, 92m); // Black candle opening below previous open

		var invalidFirst = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var invalidSecond = CreateCandle(112m, 112m, 95m, 97m); // Opens above previous open
		var invalidThird = CreateCandle(105m, 105m, 90m, 92m); // Opens above previous open

		TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, secondCandle, thirdCandle], true);
		TestPattern(CandlePatternRegistry.ThreeBlackCrows, [invalidFirst, secondCandle, thirdCandle], false);
		TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, invalidSecond, thirdCandle], false);
		TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public void BullishHarami()
	{
		var firstCandle = CreateCandle(110m, 110m, 95m, 100m); // Large black candle
		var secondCandle = CreateCandle(103m, 107m, 102m, 105m); // Small white candle inside first

		var invalidFirst = CreateCandle(100m, 110m, 95m, 105m); // White candle
		var invalidSecond = CreateCandle(98m, 115m, 95m, 110m); // Large candle, not inside

		TestPattern(CandlePatternRegistry.BullishHarami, [firstCandle, secondCandle], true);
		TestPattern(CandlePatternRegistry.BullishHarami, [invalidFirst, secondCandle], false);
		TestPattern(CandlePatternRegistry.BullishHarami, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public void BearishHarami()
	{
		var firstCandle = CreateCandle(100m, 115m, 100m, 110m); // Large white candle
		var secondCandle = CreateCandle(107m, 108m, 105m, 106m); // Small black candle inside first

		var invalidFirst = CreateCandle(110m, 110m, 95m, 100m); // Black candle
		var invalidSecond = CreateCandle(98m, 120m, 95m, 98m); // Large candle, not inside

		TestPattern(CandlePatternRegistry.BearishHarami, [firstCandle, secondCandle], true);
		TestPattern(CandlePatternRegistry.BearishHarami, [invalidFirst, secondCandle], false);
		TestPattern(CandlePatternRegistry.BearishHarami, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public void HangingMan()
	{
		var hangingMan = CreateCandle(107m, 110m, 90m, 105m); // Black candle with long bottom shadow
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // White candle (hammer)
		var normalCandle = CreateCandle(107m, 110m, 102m, 105m); // With top shadow

		TestPattern(CandlePatternRegistry.HangingMan, [hangingMan], true);
		TestPattern(CandlePatternRegistry.HangingMan, [hammer], false);
		TestPattern(CandlePatternRegistry.HangingMan, [normalCandle], false);
	}

	[TestMethod]
	public void ShootingStar()
	{
		var shootingStar = CreateCandle(107m, 120m, 105m, 105m); // Black candle with long top shadow
		var invertedHammer = CreateCandle(100m, 115m, 100m, 105m); // White candle
		var normalCandle = CreateCandle(107m, 110m, 95m, 105m); // With bottom shadow

		TestPattern(CandlePatternRegistry.ShootingStar, [shootingStar], true);
		TestPattern(CandlePatternRegistry.ShootingStar, [invertedHammer], false);
		TestPattern(CandlePatternRegistry.ShootingStar, [normalCandle], false);
	}

	[TestMethod]
	public void CustomExpression()
	{
		// Test custom pattern: Large white candle (body > 5 and O < C)
		var customPattern = new ExpressionCandlePattern("CustomLargeWhite",
			[new CandleExpressionCondition("(B > 5) && (O < C)")]);

		var largeWhite = CreateCandle(100m, 110m, 100m, 108m); // Body = 8
		var smallWhite = CreateCandle(100m, 105m, 100m, 102m); // Body = 2
		var largeBlack = CreateCandle(108m, 108m, 100m, 100m); // Body = 8 but black

		TestPattern(customPattern, [largeWhite], true);
		TestPattern(customPattern, [smallWhite], false);
		TestPattern(customPattern, [largeBlack], false);
	}

	[TestMethod]
	public void CustomMultiCandle()
	{
		// Test custom pattern: Two consecutive white candles
		var customPattern = new ExpressionCandlePattern("TwoWhiteCandles",
		[
			new CandleExpressionCondition("O < C"),
			new CandleExpressionCondition("O < C")
		]);

		var whiteCandle1 = CreateCandle(100m, 110m, 100m, 108m);
		var whiteCandle2 = CreateCandle(106m, 115m, 105m, 113m);
		var blackCandle = CreateCandle(113m, 113m, 105m, 110m);

		TestPattern(customPattern, [whiteCandle1, whiteCandle2], true);
		TestPattern(customPattern, [whiteCandle1, blackCandle], false);
		TestPattern(customPattern, [blackCandle, whiteCandle2], false);
	}

	[TestMethod]
	public void PreviousReference()
	{
		// Test pattern using previous candle reference (pO, pC, etc.)
		var customPattern = new ExpressionCandlePattern("HigherClose",
		[
			new CandleExpressionCondition("O < C"), // First candle is white
			new CandleExpressionCondition("C > pC") // Second candle closes higher than first
		]);

		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var higherClose = CreateCandle(106m, 115m, 105m, 113m); // Closes higher
		var lowerClose = CreateCandle(106m, 110m, 105m, 107m); // Closes lower

		TestPattern(customPattern, [firstCandle, higherClose], true);
		TestPattern(customPattern, [firstCandle, lowerClose], false);
	}

	[TestMethod]
	public void Valid()
	{
		var allPatterns = CandlePatternRegistry.All.ToArray();

		allPatterns.Length.AssertGreater(0);

		foreach (var pattern in allPatterns)
		{
			pattern.AssertNotNull();
			pattern.Name.AssertNotNull();

			// Test that all patterns implement ICandlePattern
			pattern.AssertOfType<ICandlePattern>();

			// Test that all basic patterns are ExpressionCandlePattern
			pattern.AssertOfType<ExpressionCandlePattern>();
		}
	}

	[TestMethod]
	public void NamesUnique()
	{
		var allPatterns = CandlePatternRegistry.All.ToArray();
		var names = allPatterns.Select(p => p.Name).ToArray();
		var uniqueNames = names.Distinct().ToArray();

		uniqueNames.Length.AssertEqual(names.Length);
	}

	[TestMethod]
	public void InsufficientData()
	{
		var singleCandle = CreateCandle(100m, 110m, 100m, 105m);

		// Test multi-candle pattern with insufficient data
		Assert.ThrowsExactly<ArgumentException>(() => CandlePatternRegistry.Piercing.Recognize([singleCandle]));
	}
	[TestMethod]
	public void SaveLoad()
	{
		var pattern = new ExpressionCandlePattern("TestPattern",
		[
			new CandleExpressionCondition("O < C"),
			new CandleExpressionCondition("B > 2")
		]);

		var storage = new SettingsStorage();
		((IPersistable)pattern).Save(storage);

		var loaded = new ExpressionCandlePattern("Loaded", []);
		((IPersistable)loaded).Load(storage);

		loaded.Name.AssertEqual(pattern.Name);
		loaded.Conditions.Length.AssertEqual(pattern.Conditions.Length);
		for (int i = 0; i < pattern.Conditions.Length; i++)
		{
			// Save/Load should preserve the formula string
			loaded.Conditions[i].ToString().AssertEqual(pattern.Conditions[i].ToString());
		}
	}

	[TestMethod]
	public void SaveLoadWithCustomName()
	{
		var pattern = new ExpressionCandlePattern("CustomName",
		[
			new CandleExpressionCondition("O > C")
		]);

		var storage = new SettingsStorage();
		((IPersistable)pattern).Save(storage);

		var loaded = new ExpressionCandlePattern("OtherName", []);
		((IPersistable)loaded).Load(storage);

		loaded.Name.AssertEqual(pattern.Name);
		loaded.Conditions.Length.AssertEqual(1);
		loaded.Conditions[0].ToString().AssertEqual("O > C");
	}

	[TestMethod]
	public void OnNeck()
	{
		// Первая свеча чёрная, вторая белая, low первой совпадает с high второй
		var first = CreateCandle(110m, 115m, 100m, 102m);    // Black (Low = 100)
		var second = CreateCandle(99m, 110m, 100m, 101m);    // White (Close = 100)
		TestPattern(CandlePatternRegistry.OnNeck, [first, second], true);

		// Неверный случай: low первой не совпадает с high второй
		var wrong = CreateCandle(102m, 116m, 103m, 110m);
		TestPattern(CandlePatternRegistry.OnNeck, [first, wrong], false);
	}

	[TestMethod]
	public void TweezerTop()
	{
		// Первая свеча белая, вторая чёрная, high совпадают, тело второй больше в 3 раза
		var first = CreateCandle(100m, 110m, 100m, 108m); // White
		var second = CreateCandle(109m, 110m, 105m, 100m); // Black, high совпадает, тело больше
		TestPattern(CandlePatternRegistry.TweezerTop, [first, second], true);

		// Неверный случай: high не совпадает
		var wrong = CreateCandle(109m, 111m, 105m, 100m);
		TestPattern(CandlePatternRegistry.TweezerTop, [first, wrong], false);
	}

	[TestMethod]
	public void TweezerBottom()
	{
		// Первая свеча чёрная, вторая белая, low совпадают, у второй нет нижней тени, верхняя тень больше тела
		var first = CreateCandle(110m, 120m, 100m, 102m); // Black
		var second = CreateCandle(102m, 120m, 100m, 120m); // White, low совпадает, BS==0, TS>B
		TestPattern(CandlePatternRegistry.TweezerBottom, [first, second], true);

		// Неверный случай: есть нижняя тень
		var wrong = CreateCandle(102m, 120m, 99m, 120m);
		TestPattern(CandlePatternRegistry.TweezerBottom, [first, wrong], false);
	}

	[TestMethod]
	public void FallingThreeMethods()
	{
		// Первая свеча чёрная, три маленьких белых, последняя большая чёрная
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(106m, 110m, 105m, 109m); // Small white
		var third = CreateCandle(109m, 112m, 108m, 111m); // Small white
		var fourth = CreateCandle(111m, 113m, 110m, 112m); // Small white
		var fifth = CreateCandle(112m, 120m, 100m, 102m); // Big black
		TestPattern(CandlePatternRegistry.FallingThreeMethods, [first, second, third, fourth, fifth], true);

		// Неверный случай: последняя белая
		var wrong = CreateCandle(112m, 120m, 100m, 115m);
		TestPattern(CandlePatternRegistry.FallingThreeMethods, [first, second, third, fourth, wrong], false);
	}

	[TestMethod]
	public void RisingThreeMethods()
	{
		// Первая свеча белая, три маленьких чёрных, последняя большая белая
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(119m, 119m, 110m, 115m); // Small black
		var third = CreateCandle(115m, 115m, 110m, 112m); // Small black
		var fourth = CreateCandle(112m, 112m, 110m, 111m); // Small black
		var fifth = CreateCandle(111m, 130m, 110m, 130m); // Big white
		TestPattern(CandlePatternRegistry.RisingThreeMethods, [first, second, third, fourth, fifth], true);

		// Неверный случай: последняя чёрная
		var wrong = CreateCandle(111m, 130m, 110m, 100m);
		TestPattern(CandlePatternRegistry.RisingThreeMethods, [first, second, third, fourth, wrong], false);
	}

	[TestMethod]
	public void ThreeInsideUp()
	{
		// Первая свеча чёрная, вторая белая внутри тела первой, третья белая больше второй
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(106m, 110m, 105m, 109m); // White, внутри тела первой
		var third = CreateCandle(109m, 120m, 109m, 120m); // White, тело больше второй
		TestPattern(CandlePatternRegistry.ThreeInsideUp, [first, second, third], true);

		// Неверный случай: третья меньше второй
		var wrong = CreateCandle(109m, 110m, 109m, 110m);
		TestPattern(CandlePatternRegistry.ThreeInsideUp, [first, second, wrong], false);
	}

	[TestMethod]
	public void ThreeInsideDown()
	{
		// Первая свеча белая, вторая чёрная внутри тела первой, третья чёрная больше второй
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(119m, 119m, 110m, 115m); // Black, внутри тела первой
		var third = CreateCandle(115m, 100m, 110m, 100m); // Black, тело больше второй
		TestPattern(CandlePatternRegistry.ThreeInsideDown, [first, second, third], true);

		// Неверный случай: третья меньше второй
		var wrong = CreateCandle(115m, 114m, 110m, 114m);
		TestPattern(CandlePatternRegistry.ThreeInsideDown, [first, second, wrong], false);
	}

	[TestMethod]
	public void ThreeOutsideUp()
	{
		// Первая свеча чёрная, вторая белая перекрывает первую, третья белая выше второй
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(104m, 130m, 104m, 130m); // White, open < close первой, close > open первой
		var third = CreateCandle(130m, 140m, 130m, 140m); // White, выше второй
		TestPattern(CandlePatternRegistry.ThreeOutsideUp, [first, second, third], true);

		// Неверный случай: третья ниже второй
		var wrong = CreateCandle(130m, 131m, 130m, 130m);
		TestPattern(CandlePatternRegistry.ThreeOutsideUp, [first, second, wrong], false);
	}

	[TestMethod]
	public void ThreeOutsideDown()
	{
		// Первая свеча белая, вторая чёрная перекрывает первую, третья чёрная ниже второй
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(121m, 90m, 90m, 90m); // Black, open > close первой, close < open первой
		var third = CreateCandle(90m, 80m, 80m, 80m); // Black, ниже второй
		TestPattern(CandlePatternRegistry.ThreeOutsideDown, [first, second, third], true);

		// Неверный случай: третья выше второй
		var wrong = CreateCandle(90m, 100m, 90m, 100m);
		TestPattern(CandlePatternRegistry.ThreeOutsideDown, [first, second, wrong], false);
	}
}