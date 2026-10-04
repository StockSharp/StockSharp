namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Patterns;

[TestClass]
public class PatternsTests : BaseTestClass
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
			OpenTime = DateTime.UtcNow,
			SecurityId = _secId,
			DataType = _dt
		};
	}

	private async Task TestPattern(ICandlePattern pattern, CandleMessage[] candles, bool expectedResult)
	{
		await pattern.PrepareAsync(CancellationToken);

		var result = pattern.Recognize(candles);
		result.AssertEqual(expectedResult);
	}

	[TestMethod]
	public async Task Flat()
	{
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);

		await TestPattern(CandlePatternRegistry.Flat, [flatCandle], true);
		await TestPattern(CandlePatternRegistry.Flat, [whiteCandle], false);
	}

	[TestMethod]
	public async Task White()
	{
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);
		var blackCandle = CreateCandle(100m, 105m, 95m, 97m);
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);

		await TestPattern(CandlePatternRegistry.White, [whiteCandle], true);
		await TestPattern(CandlePatternRegistry.White, [blackCandle], false);
		await TestPattern(CandlePatternRegistry.White, [flatCandle], false);
	}

	[TestMethod]
	public async Task Black()
	{
		var blackCandle = CreateCandle(100m, 105m, 95m, 97m);
		var whiteCandle = CreateCandle(100m, 105m, 95m, 103m);
		var flatCandle = CreateCandle(100m, 105m, 95m, 100m);

		await TestPattern(CandlePatternRegistry.Black, [blackCandle], true);
		await TestPattern(CandlePatternRegistry.Black, [whiteCandle], false);
		await TestPattern(CandlePatternRegistry.Black, [flatCandle], false);
	}

	[TestMethod]
	public async Task WhiteMarubozu()
	{
		var whiteMarubozu = CreateCandle(100m, 110m, 100m, 110m); // No shadows
		var whiteWithShadows = CreateCandle(100m, 115m, 95m, 110m); // With shadows
		var blackMarubozu = CreateCandle(110m, 110m, 100m, 100m);

		await TestPattern(CandlePatternRegistry.WhiteMarubozu, [whiteMarubozu], true);
		await TestPattern(CandlePatternRegistry.WhiteMarubozu, [whiteWithShadows], false);
		await TestPattern(CandlePatternRegistry.WhiteMarubozu, [blackMarubozu], false);
	}

	[TestMethod]
	public async Task BlackMarubozu()
	{
		var blackMarubozu = CreateCandle(110m, 110m, 100m, 100m); // No shadows
		var blackWithShadows = CreateCandle(110m, 115m, 95m, 100m); // With shadows
		var whiteMarubozu = CreateCandle(100m, 110m, 100m, 110m);

		await TestPattern(CandlePatternRegistry.BlackMarubozu, [blackMarubozu], true);
		await TestPattern(CandlePatternRegistry.BlackMarubozu, [blackWithShadows], false);
		await TestPattern(CandlePatternRegistry.BlackMarubozu, [whiteMarubozu], false);
	}

	[TestMethod]
	public async Task SpinningTop()
	{
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m); // Small body, equal shadows
		var marubozu = CreateCandle(100m, 110m, 100m, 110m); // No shadows
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow only

		await TestPattern(CandlePatternRegistry.SpinningTop, [spinningTop], true);
		await TestPattern(CandlePatternRegistry.SpinningTop, [marubozu], false);
		await TestPattern(CandlePatternRegistry.SpinningTop, [hammer], false);
	}

	[TestMethod]
	public async Task Hammer()
	{
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow, no top shadow
		var invertedHammer = CreateCandle(100m, 110m, 95m, 105m); // Long top shadow, no bottom shadow
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m);

		await TestPattern(CandlePatternRegistry.Hammer, [hammer], true);
		await TestPattern(CandlePatternRegistry.Hammer, [invertedHammer], false);
		await TestPattern(CandlePatternRegistry.Hammer, [spinningTop], false);
	}

	[TestMethod]
	public async Task InvertedHammer()
	{
		var invertedHammer = CreateCandle(100m, 110m, 100m, 105m); // Long top shadow, no bottom shadow
		var hammer = CreateCandle(100m, 105m, 90m, 105m); // Long bottom shadow
		var spinningTop = CreateCandle(102m, 107m, 97m, 103m);

		await TestPattern(CandlePatternRegistry.InvertedHammer, [invertedHammer], true);
		await TestPattern(CandlePatternRegistry.InvertedHammer, [hammer], false);
		await TestPattern(CandlePatternRegistry.InvertedHammer, [spinningTop], false);
	}

	[TestMethod]
	public async Task Dragonfly()
	{
		var dragonfly = CreateCandle(100m, 100m, 90m, 100m); // Flat, long bottom shadow
		var gravestone = CreateCandle(100m, 110m, 100m, 100m); // Flat, long top shadow
		var normalCandle = CreateCandle(100m, 105m, 95m, 103m);

		await TestPattern(CandlePatternRegistry.Dragonfly, [dragonfly], true);
		await TestPattern(CandlePatternRegistry.Dragonfly, [gravestone], false);
		await TestPattern(CandlePatternRegistry.Dragonfly, [normalCandle], false);
	}

	[TestMethod]
	public async Task Gravestone()
	{
		var gravestone = CreateCandle(100m, 110m, 100m, 100m); // Flat, long top shadow
		var dragonfly = CreateCandle(100m, 100m, 90m, 100m); // Flat, long bottom shadow
		var normalCandle = CreateCandle(100m, 105m, 95m, 103m);

		await TestPattern(CandlePatternRegistry.Gravestone, [gravestone], true);
		await TestPattern(CandlePatternRegistry.Gravestone, [dragonfly], false);
		await TestPattern(CandlePatternRegistry.Gravestone, [normalCandle], false);
	}

	[TestMethod]
	public async Task Bullish()
	{
		var bullish = CreateCandle(100m, 110m, 100m, 107m); // White candle with small/no bottom shadow
		var bearish = CreateCandle(107m, 107m, 90m, 100m); // Black candle
		var weakBullish = CreateCandle(100m, 110m, 80m, 107m); // White but large bottom shadow

		await TestPattern(CandlePatternRegistry.Bullish, [bullish], true);
		await TestPattern(CandlePatternRegistry.Bullish, [bearish], false);
		await TestPattern(CandlePatternRegistry.Bullish, [weakBullish], true);
	}

	[TestMethod]
	public async Task Bearish()
	{
		var bearish = CreateCandle(107m, 107m, 90m, 100m); // Black candle with small/no top shadow
		var bullish = CreateCandle(100m, 110m, 100m, 107m); // White candle
		var weakBearish = CreateCandle(107m, 120m, 90m, 100m); // Black but large top shadow

		await TestPattern(CandlePatternRegistry.Bearish, [bearish], true);
		await TestPattern(CandlePatternRegistry.Bearish, [bullish], false);
		await TestPattern(CandlePatternRegistry.Bearish, [weakBearish], true);
	}

	[TestMethod]
	public async Task Piercing()
	{
		var firstCandle = CreateCandle(110m, 110m, 95m, 100m); // Black candle
		var secondCandle = CreateCandle(98m, 108m, 95m, 106m); // White candle closing above midpoint

		var invalidFirst = CreateCandle(100m, 110m, 95m, 105m); // White candle
		var invalidSecond = CreateCandle(98m, 108m, 95m, 102m); // Doesn't close above midpoint

		await TestPattern(CandlePatternRegistry.Piercing, [firstCandle, secondCandle], true);
		await TestPattern(CandlePatternRegistry.Piercing, [invalidFirst, secondCandle], false);
		await TestPattern(CandlePatternRegistry.Piercing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public async Task BullishEngulfing()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 105m); // Small black candle
		var secondCandle = CreateCandle(102m, 115m, 98m, 112m); // Large white candle engulfing first

		var invalidFirst = CreateCandle(100m, 110m, 100m, 105m); // White candle
		var invalidSecond = CreateCandle(106m, 115m, 98m, 112m); // Doesn't engulf

		await TestPattern(CandlePatternRegistry.BullishEngulfing, [firstCandle, secondCandle], true);
		await TestPattern(CandlePatternRegistry.BullishEngulfing, [invalidFirst, secondCandle], false);
		await TestPattern(CandlePatternRegistry.BullishEngulfing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public async Task BearishEngulfing()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 105m); // Small white candle
		var secondCandle = CreateCandle(108m, 108m, 95m, 98m); // Large black candle engulfing first

		var invalidFirst = CreateCandle(110m, 110m, 100m, 105m); // Black candle
		var invalidSecond = CreateCandle(102m, 108m, 95m, 98m); // Doesn't engulf

		await TestPattern(CandlePatternRegistry.BearishEngulfing, [firstCandle, secondCandle], true);
		await TestPattern(CandlePatternRegistry.BearishEngulfing, [invalidFirst, secondCandle], false);
		await TestPattern(CandlePatternRegistry.BearishEngulfing, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public async Task MorningStar()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var secondCandle = CreateCandle(101m, 103m, 99m, 100m); // Small body (star)
		var thirdCandle = CreateCandle(101m, 115m, 101m, 112m); // White candle

		var invalidFirst = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var invalidSecond = CreateCandle(101m, 115m, 95m, 110m); // Large body
		var invalidThird = CreateCandle(110m, 115m, 101m, 105m); // Black candle

		await TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, secondCandle, thirdCandle], true);
		await TestPattern(CandlePatternRegistry.MorningStar, [invalidFirst, secondCandle, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, invalidSecond, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.MorningStar, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public async Task EveningStar()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var secondCandle = CreateCandle(109m, 111m, 107m, 110m); // Small body (star)
		var thirdCandle = CreateCandle(109m, 109m, 95m, 102m); // Black candle

		var invalidFirst = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var invalidSecond = CreateCandle(109m, 125m, 95m, 120m); // Large body
		var invalidThird = CreateCandle(109m, 115m, 105m, 112m); // White candle

		await TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, secondCandle, thirdCandle], true);
		await TestPattern(CandlePatternRegistry.EveningStar, [invalidFirst, secondCandle, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, invalidSecond, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.EveningStar, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public async Task ThreeWhiteSoldiers()
	{
		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var secondCandle = CreateCandle(106m, 115m, 105m, 113m); // White candle opening above previous open
		var thirdCandle = CreateCandle(111m, 120m, 110m, 118m); // White candle opening above previous open

		var invalidFirst = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var invalidSecond = CreateCandle(98m, 115m, 95m, 113m); // Opens below previous open
		var invalidThird = CreateCandle(105m, 120m, 105m, 118m); // Opens below previous open

		await TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, secondCandle, thirdCandle], true);
		await TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [invalidFirst, secondCandle, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, invalidSecond, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.ThreeWhiteSoldiers, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public async Task ThreeBlackCrows()
	{
		var firstCandle = CreateCandle(110m, 110m, 100m, 102m); // Black candle
		var secondCandle = CreateCandle(104m, 104m, 95m, 97m); // Black candle opening below previous open
		var thirdCandle = CreateCandle(99m, 99m, 90m, 92m); // Black candle opening below previous open

		var invalidFirst = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var invalidSecond = CreateCandle(112m, 112m, 95m, 97m); // Opens above previous open
		var invalidThird = CreateCandle(105m, 105m, 90m, 92m); // Opens above previous open

		await TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, secondCandle, thirdCandle], true);
		await TestPattern(CandlePatternRegistry.ThreeBlackCrows, [invalidFirst, secondCandle, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, invalidSecond, thirdCandle], false);
		await TestPattern(CandlePatternRegistry.ThreeBlackCrows, [firstCandle, secondCandle, invalidThird], false);
	}

	[TestMethod]
	public async Task BullishHarami()
	{
		var firstCandle = CreateCandle(110m, 110m, 95m, 100m); // Large black candle
		var secondCandle = CreateCandle(103m, 107m, 102m, 105m); // Small white candle inside first

		var invalidFirst = CreateCandle(100m, 110m, 95m, 105m); // White candle
		var invalidSecond = CreateCandle(98m, 115m, 95m, 110m); // Large candle, not inside

		await TestPattern(CandlePatternRegistry.BullishHarami, [firstCandle, secondCandle], true);
		await TestPattern(CandlePatternRegistry.BullishHarami, [invalidFirst, secondCandle], false);
		await TestPattern(CandlePatternRegistry.BullishHarami, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public async Task BearishHarami()
	{
		var firstCandle = CreateCandle(100m, 115m, 100m, 110m); // Large white candle
		var secondCandle = CreateCandle(107m, 108m, 105m, 106m); // Small black candle inside first

		var invalidFirst = CreateCandle(110m, 110m, 95m, 100m); // Black candle
		var invalidSecond = CreateCandle(116m, 120m, 95m, 114m); // Black candle outside first body

		await TestPattern(CandlePatternRegistry.BearishHarami, [firstCandle, secondCandle], true);
		await TestPattern(CandlePatternRegistry.BearishHarami, [invalidFirst, secondCandle], false);
		await TestPattern(CandlePatternRegistry.BearishHarami, [firstCandle, invalidSecond], false);
	}

	[TestMethod]
	public async Task HangingMan()
	{
		var hangingMan = CreateCandle(107m, 110m, 90m, 105m); // Black candle with long bottom shadow
		var bodyEqualsHalfLowerShadow = CreateCandle(100m, 105m, 90m, 105m);
		var normalCandle = CreateCandle(107m, 110m, 102m, 105m); // With top shadow

		await TestPattern(CandlePatternRegistry.HangingMan, [hangingMan], true);
		await TestPattern(CandlePatternRegistry.HangingMan, [bodyEqualsHalfLowerShadow], false);
		await TestPattern(CandlePatternRegistry.HangingMan, [normalCandle], false);
	}

	[TestMethod]
	public async Task ShootingStar()
	{
		var shootingStar = CreateCandle(107m, 120m, 105m, 105m); // Black candle with long top shadow
		var bodyEqualsHalfUpperShadow = CreateCandle(100m, 115m, 100m, 105m);
		var normalCandle = CreateCandle(107m, 110m, 95m, 105m); // With bottom shadow

		await TestPattern(CandlePatternRegistry.ShootingStar, [shootingStar], true);
		await TestPattern(CandlePatternRegistry.ShootingStar, [bodyEqualsHalfUpperShadow], false);
		await TestPattern(CandlePatternRegistry.ShootingStar, [normalCandle], false);
	}

	[TestMethod]
	public async Task CustomExpression()
	{
		var fs = Helper.FileSystem;
		
		// Test custom pattern: Large white candle (body > 5 and O < C)
		var customPattern = new ExpressionCandlePattern("CustomLargeWhite",
			[new CandleExpressionCondition(fs, "(B > 5) && (O < C)")]);

		var largeWhite = CreateCandle(100m, 110m, 100m, 108m); // Body = 8
		var smallWhite = CreateCandle(100m, 105m, 100m, 102m); // Body = 2
		var largeBlack = CreateCandle(108m, 108m, 100m, 100m); // Body = 8 but black

		await TestPattern(customPattern, [largeWhite], true);
		await TestPattern(customPattern, [smallWhite], false);
		await TestPattern(customPattern, [largeBlack], false);
	}

	[TestMethod]
	public async Task CustomMultiCandle()
	{
		// Compiling a formula reads the reference assemblies from this file system, so it has to be the real one.
		var fs = Helper.FileSystem;
		
		// Test custom pattern: Two consecutive white candles
		var customPattern = new ExpressionCandlePattern("TwoWhiteCandles",
		[
			new CandleExpressionCondition(fs, "O < C"),
			new CandleExpressionCondition(fs, "O < C")
		]);

		var whiteCandle1 = CreateCandle(100m, 110m, 100m, 108m);
		var whiteCandle2 = CreateCandle(106m, 115m, 105m, 113m);
		var blackCandle = CreateCandle(113m, 113m, 105m, 110m);

		await TestPattern(customPattern, [whiteCandle1, whiteCandle2], true);
		await TestPattern(customPattern, [whiteCandle1, blackCandle], false);
		await TestPattern(customPattern, [blackCandle, whiteCandle2], false);
	}

	[TestMethod]
	public async Task PreviousReference()
	{
		var fs = Helper.FileSystem;
		
		// Test pattern using previous candle reference (pO, pC, etc.)
		var customPattern = new ExpressionCandlePattern("HigherClose",
		[
			new CandleExpressionCondition(fs, "O < C"), // First candle is white
			new CandleExpressionCondition(fs, "C > pC") // Second candle closes higher than first
		]);

		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var higherClose = CreateCandle(106m, 115m, 105m, 113m); // Closes higher
		var lowerClose = CreateCandle(106m, 110m, 105m, 107m); // Closes lower

		await TestPattern(customPattern, [firstCandle, higherClose], true);
		await TestPattern(customPattern, [firstCandle, lowerClose], false);
	}

	[TestMethod]
	public void Valid()
	{
		var allPatterns = CandlePatternRegistry.All.ToArray();

		allPatterns.Length.AssertEqual(32);

		foreach (var pattern in allPatterns)
		{
			pattern.AssertNotNull();
			pattern.Name.AssertNotNull();

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
	public async Task InsufficientData()
	{
		var singleCandle = CreateCandle(100m, 110m, 100m, 105m);

		await CandlePatternRegistry.Piercing.PrepareAsync(CancellationToken);

		// Test multi-candle pattern with insufficient data
		ThrowsExactly<ArgumentException>(() => CandlePatternRegistry.Piercing.Recognize([singleCandle]));
	}
	[TestMethod]
	public async Task SaveLoad()
	{
		var fs = Helper.FileSystem;

		var pattern = new ExpressionCandlePattern("TestPattern",
		[
			new CandleExpressionCondition(fs, "O < C"),
			new CandleExpressionCondition(fs, "B > 2")
		]);

		var storage = new SettingsStorage();
		await ((IAsyncPersistable)pattern).SaveAsync(storage, CancellationToken);

		var loaded = new ExpressionCandlePattern("Loaded", []);
		await ((IAsyncPersistable)loaded).LoadAsync(storage, CancellationToken);

		loaded.Name.AssertEqual(pattern.Name);
		loaded.Conditions.Length.AssertEqual(pattern.Conditions.Length);
		for (int i = 0; i < pattern.Conditions.Length; i++)
		{
			// Save/Load should preserve the formula string
			loaded.Conditions[i].ToString().AssertEqual(pattern.Conditions[i].ToString());
		}
	}

	[TestMethod]
	public async Task SaveLoadWithCustomName()
	{
		// Compiling a formula reads the reference assemblies from this file system, so it has to be the real one.
		var fs = Helper.FileSystem;

		var pattern = new ExpressionCandlePattern("CustomName",
		[
			new CandleExpressionCondition(fs, "O > C")
		]);

		var storage = new SettingsStorage();
		await ((IAsyncPersistable)pattern).SaveAsync(storage, CancellationToken);

		var loaded = new ExpressionCandlePattern("OtherName", []);
		await ((IAsyncPersistable)loaded).LoadAsync(storage, CancellationToken);

		loaded.Name.AssertEqual(pattern.Name);
		loaded.Conditions.Length.AssertEqual(1);
		loaded.Conditions[0].ToString().AssertEqual("O > C");
	}

	[TestMethod]
	public async Task OnNeck()
	{
		// Первая свеча чёрная, вторая белая, close второй рядом с low первой
		var first = CreateCandle(110m, 115m, 100m, 102m);    // Black (Low = 100)
		var second = CreateCandle(99m, 110m, 99m, 101m);     // White (Close = 101)
		await TestPattern(CandlePatternRegistry.OnNeck, [first, second], true);

		// Неверный случай: close второй далеко от low первой
		var wrong = CreateCandle(102m, 116m, 101m, 110m);
		await TestPattern(CandlePatternRegistry.OnNeck, [first, wrong], false);
	}

	[TestMethod]
	public async Task TweezerTop()
	{
		// Первая свеча белая, вторая чёрная, high совпадают, тело второй больше в 3 раза
		var first = CreateCandle(100m, 110m, 100m, 108m); // White
		var second = CreateCandle(109m, 110m, 105m, 100m); // Black, high совпадает, тело больше
		await TestPattern(CandlePatternRegistry.TweezerTop, [first, second], true);

		// Неверный случай: high не совпадает
		var wrong = CreateCandle(109m, 111m, 105m, 100m);
		await TestPattern(CandlePatternRegistry.TweezerTop, [first, wrong], false);
	}

	[TestMethod]
	public async Task TweezerBottom()
	{
		// Первая свеча чёрная, вторая белая, low совпадают, у второй нет нижней тени, верхняя тень больше тела
		var first = CreateCandle(110m, 120m, 100m, 102m); // Black
		var second = CreateCandle(102m, 120m, 100m, 120m); // White, low совпадает, BS==0, TS>B
		await TestPattern(CandlePatternRegistry.TweezerBottom, [first, second], true);

		// Неверный случай: есть нижняя тень
		var wrong = CreateCandle(102m, 120m, 99m, 120m);
		await TestPattern(CandlePatternRegistry.TweezerBottom, [first, wrong], false);
	}

	[TestMethod]
	public async Task FallingThreeMethods()
	{
		// Первая свеча чёрная, три маленьких белых, последняя большая чёрная
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(106m, 110m, 105m, 109m); // Small white
		var third = CreateCandle(109m, 112m, 108m, 111m); // Small white
		var fourth = CreateCandle(111m, 113m, 110m, 112m); // Small white
		var fifth = CreateCandle(112m, 120m, 100m, 102m); // Big black
		await TestPattern(CandlePatternRegistry.FallingThreeMethods, [first, second, third, fourth, fifth], true);

		// Неверный случай: последняя белая
		var wrong = CreateCandle(112m, 120m, 100m, 115m);
		await TestPattern(CandlePatternRegistry.FallingThreeMethods, [first, second, third, fourth, wrong], false);
	}

	[TestMethod]
	public async Task RisingThreeMethods()
	{
		// Первая свеча белая, три маленьких чёрных, последняя большая белая
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(119m, 119m, 110m, 115m); // Small black
		var third = CreateCandle(115m, 115m, 110m, 112m); // Small black
		var fourth = CreateCandle(112m, 112m, 110m, 111m); // Small black
		var fifth = CreateCandle(111m, 130m, 110m, 130m); // Big white
		await TestPattern(CandlePatternRegistry.RisingThreeMethods, [first, second, third, fourth, fifth], true);

		// Неверный случай: последняя чёрная
		var wrong = CreateCandle(111m, 130m, 110m, 100m);
		await TestPattern(CandlePatternRegistry.RisingThreeMethods, [first, second, third, fourth, wrong], false);
	}

	[TestMethod]
	public async Task ThreeInsideUp()
	{
		// Первая свеча чёрная, вторая белая внутри тела первой, третья белая больше второй
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(106m, 110m, 105m, 109m); // White, внутри тела первой
		var third = CreateCandle(109m, 120m, 109m, 120m); // White, тело больше второй
		await TestPattern(CandlePatternRegistry.ThreeInsideUp, [first, second, third], true);

		// Неверный случай: третья меньше второй
		var wrong = CreateCandle(109m, 110m, 109m, 110m);
		await TestPattern(CandlePatternRegistry.ThreeInsideUp, [first, second, wrong], false);
	}

	[TestMethod]
	public async Task ThreeInsideDown()
	{
		// Первая свеча белая, вторая чёрная внутри тела первой, третья чёрная больше второй
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(119m, 119m, 110m, 115m); // Black, внутри тела первой
		var third = CreateCandle(115m, 115m, 100m, 100m); // Black, тело больше второй
		await TestPattern(CandlePatternRegistry.ThreeInsideDown, [first, second, third], true);

		// Неверный случай: третья меньше второй
		var wrong = CreateCandle(115m, 115m, 110m, 114m);
		await TestPattern(CandlePatternRegistry.ThreeInsideDown, [first, second, wrong], false);
	}

	[TestMethod]
	public async Task ThreeOutsideUp()
	{
		// Первая свеча чёрная, вторая белая перекрывает первую, третья белая выше второй
		var first = CreateCandle(120m, 125m, 100m, 105m); // Black
		var second = CreateCandle(104m, 130m, 104m, 130m); // White, open < close первой, close > open первой
		var third = CreateCandle(130m, 140m, 130m, 140m); // White, выше второй
		await TestPattern(CandlePatternRegistry.ThreeOutsideUp, [first, second, third], true);

		// Неверный случай: третья ниже второй
		var wrong = CreateCandle(130m, 131m, 130m, 130m);
		await TestPattern(CandlePatternRegistry.ThreeOutsideUp, [first, second, wrong], false);
	}

	[TestMethod]
	public async Task ThreeOutsideDown()
	{
		// Первая свеча белая, вторая чёрная перекрывает первую, третья чёрная ниже второй
		var first = CreateCandle(100m, 120m, 100m, 120m); // White
		var second = CreateCandle(121m, 121m, 90m, 90m); // Black, open > close первой, close < open первой
		var third = CreateCandle(90m, 90m, 80m, 80m); // Black, ниже второй
		await TestPattern(CandlePatternRegistry.ThreeOutsideDown, [first, second, third], true);

		// Неверный случай: третья выше второй
		var wrong = CreateCandle(90m, 100m, 90m, 100m);
		await TestPattern(CandlePatternRegistry.ThreeOutsideDown, [first, second, wrong], false);
	}

	/// <summary>
	/// Creating a condition compiles nothing, so a formula with an unknown variable is accepted there.
	/// It is the preparation that reports it - every time it is asked, not only the first.
	/// </summary>
	[TestMethod]
	public async Task AnInvalidFormulaIsReportedWhenTheConditionIsPrepared()
	{
		// Compiling a formula reads the reference assemblies from this file system, so it has to be the real one.
		var fs = Helper.FileSystem;

		var cond = new CandleExpressionCondition(fs, "ZZZ > 0");

		cond.Expression.AssertEqual("ZZZ > 0");
		cond.ToString().AssertEqual("ZZZ > 0");

		await ThrowsExactlyAsync<InvalidOperationException>(() => cond.PrepareAsync(CancellationToken).AsTask());
		await ThrowsExactlyAsync<InvalidOperationException>(() => cond.PrepareAsync(CancellationToken).AsTask());

		ICandlePattern broken = new ExpressionCandlePattern("Broken", [cond]);

		await ThrowsExactlyAsync<InvalidOperationException>(() => broken.PrepareAsync(CancellationToken).AsTask());
		await ThrowsExactlyAsync<InvalidOperationException>(() => broken.PrepareAsync(CancellationToken).AsTask());
	}

	/// <summary>
	/// A pattern that was not prepared has nothing compiled to evaluate. It has to say so instead of
	/// compiling on the spot, where nothing can wait for the compiler.
	/// </summary>
	[TestMethod]
	public async Task APatternThatWasNotPreparedRefusesToRecognize()
	{
		var fs = Helper.FileSystem;

		ICandlePattern pattern = new ExpressionCandlePattern("White", [new CandleExpressionCondition(fs, "O < C")]);
		var candles = new CandleMessage[] { CreateCandle(100m, 110m, 100m, 108m) };

		ThrowsExactly<InvalidOperationException>(() => pattern.Recognize(candles));

		await pattern.PrepareAsync(CancellationToken);

		pattern.Recognize(candles).AssertTrue("a prepared pattern has to recognize the candle its formula describes");
	}

	/// <summary>
	/// Preparing a pattern again compiles nothing anew and changes no result.
	/// </summary>
	[TestMethod]
	public async Task PreparingAPatternTwiceChangesNothing()
	{
		var fs = Helper.FileSystem;

		ICandlePattern pattern = new ExpressionCandlePattern("HigherClose",
		[
			new CandleExpressionCondition(fs, "O < C"),
			new CandleExpressionCondition(fs, "(O < C) && (C > pC)")
		]);

		var firstCandle = CreateCandle(100m, 110m, 100m, 108m); // White candle
		var higherClose = CreateCandle(106m, 115m, 105m, 113m); // Closes higher
		var lowerClose = CreateCandle(106m, 110m, 105m, 107m); // Closes lower

		await pattern.PrepareAsync(CancellationToken);
		await pattern.PrepareAsync(CancellationToken);

		pattern.Recognize(new CandleMessage[] { firstCandle, higherClose }).AssertTrue();
		pattern.Recognize(new CandleMessage[] { firstCandle, lowerClose }).AssertFalse();
	}

	/// <summary>
	/// A formula that looks at the previous candle cannot stand first in a pattern: there is no candle
	/// before it. The preparation refuses such a pattern and names the formula.
	/// </summary>
	[TestMethod]
	public async Task AFormulaThatReachesOutsideThePatternIsRefusedWhenThePatternIsPrepared()
	{
		var fs = Helper.FileSystem;

		ICandlePattern pattern = new ExpressionCandlePattern("OutOfRange",
		[
			new CandleExpressionCondition(fs, "C > pC"),
			new CandleExpressionCondition(fs, "O < C")
		]);

		var error = await ThrowsExactlyAsync<ExpressionCandlePattern.ConditionError>(() => pattern.PrepareAsync(CancellationToken).AsTask());

		error.Indexes.Length.AssertEqual(1);
		error.Indexes[0].AssertEqual(0);

		// the refusal is repeated for as long as the pattern stays what it is...
		await ThrowsExactlyAsync<ExpressionCandlePattern.ConditionError>(() => pattern.PrepareAsync(CancellationToken).AsTask());

		// ...and does not make the pattern usable: it is the pattern that refuses, before any formula is evaluated
		var candles = new CandleMessage[] { CreateCandle(100m, 110m, 100m, 108m), CreateCandle(106m, 115m, 105m, 113m) };
		var refusal = ThrowsExactly<InvalidOperationException>(() => pattern.Recognize(candles));
		refusal.Message.Contains("not prepared").AssertTrue($"the pattern must refuse as one that is not prepared, got: {refusal.Message}");
	}

	/// <summary>
	/// A pattern with no formulas, or with empty ones only, would match every candle. It is refused.
	/// </summary>
	[TestMethod]
	public async Task APatternWithoutFormulasIsRefusedWhenItIsPrepared()
	{
		var fs = Helper.FileSystem;

		ICandlePattern noConditions = new ExpressionCandlePattern("NoConditions", []);
		await ThrowsExactlyAsync<InvalidOperationException>(() => noConditions.PrepareAsync(CancellationToken).AsTask());

		ICandlePattern emptyOnly = new ExpressionCandlePattern("EmptyOnly", [new CandleExpressionCondition(fs, null), new CandleExpressionCondition(fs, " ")]);
		await ThrowsExactlyAsync<InvalidOperationException>(() => emptyOnly.PrepareAsync(CancellationToken).AsTask());
	}

	/// <summary>
	/// A complex pattern is prepared through its parts: preparing it alone has to be enough to recognize.
	/// </summary>
	[TestMethod]
	public async Task PreparingAComplexPatternPreparesItsParts()
	{
		var fs = Helper.FileSystem;

		var white = new ExpressionCandlePattern("White", [new CandleExpressionCondition(fs, "O < C")]);
		var black = new ExpressionCandlePattern("Black", [new CandleExpressionCondition(fs, "O > C")]);
		ICandlePattern complex = new ComplexCandlePattern("WhiteThenBlack", [white, black]);

		var whiteCandle = CreateCandle(100m, 110m, 100m, 108m);
		var blackCandle = CreateCandle(108m, 110m, 100m, 100m);

		await complex.PrepareAsync(CancellationToken);

		complex.Recognize(new CandleMessage[] { whiteCandle, blackCandle }).AssertTrue();
		complex.Recognize(new CandleMessage[] { blackCandle, whiteCandle }).AssertFalse();
	}

	/// <summary>
	/// A pattern without formulas spans no candles, so the indicator never evaluates it and answers with
	/// empty values. Its preparation therefore has nothing to refuse.
	/// </summary>
	[TestMethod]
	public async Task AnIndicatorWithAPatternOfNoCandlesHasNothingToPrepare()
	{
		var indicator = new CandlePatternIndicator { Pattern = new ExpressionCandlePattern() };

		await indicator.PrepareAsync(CancellationToken);

		indicator.Process(CreateCandle(100m, 110m, 100m, 108m)).IsEmpty.AssertTrue();
	}

	/// <summary>
	/// A pattern handed to the provider is prepared by the save, because indicators switch to it the moment
	/// it replaces the one they use - in a handler that cannot wait for a compiler.
	/// </summary>
	[TestMethod]
	public async Task APatternIsPreparedByTheProviderThatSavesIt()
	{
		var fs = Helper.FileSystem;
		ICandlePatternProvider provider = new InMemoryCandlePatternProvider();

		var whiteCandle = new CandleMessage[] { CreateCandle(100m, 110m, 100m, 108m) };

		await provider.SaveAsync(new ExpressionCandlePattern("Mine", [new CandleExpressionCondition(fs, "O > C")]), CancellationToken);

		bool? recognizedByTheReplacement = null;
		provider.PatternReplaced += (_, replacement) => recognizedByTheReplacement = replacement.Recognize(whiteCandle);

		await provider.SaveAsync(new ExpressionCandlePattern("Mine", [new CandleExpressionCondition(fs, "O < C")]), CancellationToken);

		recognizedByTheReplacement.AssertEqual(true, "the replacement has to be usable by the time it is announced");
	}

	/// <summary>
	/// A pattern that cannot be prepared is refused by the save and does not get into the provider.
	/// </summary>
	[TestMethod]
	public async Task APatternThatCannotBePreparedIsNotSaved()
	{
		var fs = Helper.FileSystem;
		ICandlePatternProvider provider = new InMemoryCandlePatternProvider();

		var broken = new ExpressionCandlePattern("Broken", [new CandleExpressionCondition(fs, "ZZZ > 0")]);

		await ThrowsExactlyAsync<InvalidOperationException>(() => provider.SaveAsync(broken, CancellationToken).AsTask());

		provider.TryFind("Broken", out _).AssertFalse();
	}

	private sealed class PatternStrategy(CandlePatternIndicator indicator) : Strategy
	{
		protected override void OnStarted2(DateTime time)
		{
			base.OnStarted2(time);

			Indicators.Add(indicator);
		}
	}

	/// <summary>
	/// A strategy feeds its indicators from handlers that cannot wait, so it prepares the ones its start
	/// registered: a pattern indicator bound there recognizes without the strategy preparing it by hand.
	/// </summary>
	[TestMethod]
	public async Task AStrategyPreparesTheIndicatorsItsStartRegistered()
	{
		var fs = Helper.FileSystem;

		var indicator = new CandlePatternIndicator
		{
			Pattern = new ExpressionCandlePattern("White", [new CandleExpressionCondition(fs, "O < C")]),
		};

		var strategy = new PatternStrategy(indicator)
		{
			Security = Helper.CreateSecurity(),
			Portfolio = Helper.CreatePortfolio(),
		};

		// hands every outgoing message straight back to the strategy, the way the message loop of a connector does
		var connector = new Mock<IConnector>();
		connector.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());
		connector
			.Setup(c => c.SendOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
			.Returns((Message message, CancellationToken token) => strategy.OnNewMessage(message, token));

		strategy.Connector = connector.Object;

		await strategy.StartAsync(CancellationToken);
		strategy.ProcessState.AssertEqual(ProcessStates.Started);

		var value = (CandlePatternIndicatorValue)indicator.Process(CreateCandle(100m, 110m, 100m, 108m));

		value.Value.AssertTrue("the indicator the strategy started with has to be ready for its first candle");
	}

	/// <summary>
	/// The indicator evaluates its pattern on every candle, where nothing can wait. Its own preparation
	/// is what compiles the pattern, and an indicator that was not prepared says so.
	/// </summary>
	[TestMethod]
	public async Task ThePatternIndicatorRecognizesOnceItIsPrepared()
	{
		var fs = Helper.FileSystem;

		var pattern = new ExpressionCandlePattern("White", [new CandleExpressionCondition(fs, "O < C")]);
		var whiteCandle = CreateCandle(100m, 110m, 100m, 108m);

		var unprepared = new CandlePatternIndicator { Pattern = pattern };
		ThrowsExactly<InvalidOperationException>(() => unprepared.Process(whiteCandle));

		var indicator = new CandlePatternIndicator { Pattern = pattern };
		await indicator.PrepareAsync(CancellationToken);

		var value = (CandlePatternIndicatorValue)indicator.Process(whiteCandle);

		value.Value.AssertTrue("a prepared indicator has to recognize the candle its pattern describes");
	}

	[TestMethod]
	public async Task LoadedConditionIsCompiled()
	{
		// Compiling a formula reads the reference assemblies from this file system, so it has to be the real one.
		var fs = Helper.FileSystem;

		// Saving needs the formula text only, so the condition is not prepared here.
		var pattern = new ExpressionCandlePattern("SavedWhite",
		[
			new CandleExpressionCondition(fs, "O < C")
		]);

		var storage = new SettingsStorage();
		await ((IAsyncPersistable)pattern).SaveAsync(storage, CancellationToken);

		var loaded = new ExpressionCandlePattern("Loaded", []);
		await ((IAsyncPersistable)loaded).LoadAsync(storage, CancellationToken);

		loaded.Conditions.Length.AssertEqual(1);
		loaded.Conditions[0].ToString().AssertEqual("O < C");

		// A condition constructed empty and then loaded has to evaluate the loaded formula:
		// a condition left uncompiled silently matches everything, which no assertion on the
		// formula text would ever notice. Loading compiles it, so the condition answers at once.
		loaded.Conditions[0].CheckCondition(new CandleMessage[] { CreateCandle(100m, 110m, 100m, 108m) }, 0).AssertTrue();
		loaded.Conditions[0].CheckCondition(new CandleMessage[] { CreateCandle(108m, 110m, 100m, 100m) }, 0).AssertFalse();

		await TestPattern(loaded, [CreateCandle(100m, 110m, 100m, 108m)], true);
		await TestPattern(loaded, [CreateCandle(108m, 110m, 100m, 100m)], false);
	}
}
