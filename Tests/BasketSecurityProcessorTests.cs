namespace StockSharp.Tests;

/// <summary>
/// Comprehensive tests for basket security processors.
/// Tests WeightedIndexSecurity, ExpressionIndexSecurity, ContinuousSecurity processing.
/// </summary>
[TestClass]
public class BasketSecurityProcessorTests : BaseTestClass
{
	#region Parameterized Index Processor Tests

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Ticks_CalculatesCorrectly(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, expectedPrice) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 2);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;

		// Send tick for first leg
		var lkohTick = CreateTick(lkoh, serverTime, price: 100m, volume: 10m);
		var result1 = processor.Process(lkohTick).ToArray();
		result1.Length.AssertEqual(0); // No output yet, waiting for second leg

		// Send tick for second leg
		var sberTick = CreateTick(sber, serverTime, price: 50m, volume: 20m);
		var result2 = processor.Process(sberTick).ToArray();
		result2.Length.AssertEqual(1);

		var basketTick = (ExecutionMessage)result2[0];
		basketTick.SecurityId.AssertEqual(basket.ToSecurityId());
		basketTick.DataType.AssertEqual(DataType.Ticks);

		// Weighted: 100 * 1 + 50 * 2 = 200
		// Expression: 100 + 2 * 50 = 200
		basketTick.TradePrice.AssertEqual(expectedPrice);
	}

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Ticks_MultipleRounds(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 1);
		var processor = CreateProcessor(basket);

		var time1 = DateTime.UtcNow;
		var time2 = time1.AddSeconds(1);

		// First round
		processor.Process(CreateTick(lkoh, time1, price: 100m, volume: 10m)).ToArray();
		var result1 = processor.Process(CreateTick(sber, time1, price: 50m, volume: 10m)).ToArray();
		result1.Length.AssertEqual(1);
		((ExecutionMessage)result1[0]).TradePrice.AssertEqual(150m);

		// Second round with different prices
		processor.Process(CreateTick(lkoh, time2, price: 110m, volume: 15m)).ToArray();
		var result2 = processor.Process(CreateTick(sber, time2, price: 60m, volume: 15m)).ToArray();
		result2.Length.AssertEqual(1);
		((ExecutionMessage)result2[0]).TradePrice.AssertEqual(170m);
	}

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_OrderBook_CalculatesCorrectDepth(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 1);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;

		var lkohDepth = CreateOrderBook(lkoh, serverTime,
			bids: [(100m, 10m), (99m, 20m)],
			asks: [(101m, 15m), (102m, 25m)]);

		var sberDepth = CreateOrderBook(sber, serverTime,
			bids: [(50m, 5m), (49m, 10m)],
			asks: [(51m, 8m), (52m, 12m)]);

		processor.Process(lkohDepth).ToArray();
		var result = processor.Process(sberDepth).ToArray();

		result.Length.AssertEqual(1);
		var basketDepth = (QuoteChangeMessage)result[0];

		basketDepth.SecurityId.AssertEqual(basket.ToSecurityId());

		// Best bid: 100 + 50 = 150
		basketDepth.Bids.Length.AssertEqual(2);
		basketDepth.Bids[0].Price.AssertEqual(150m);

		// Best ask: 101 + 51 = 152
		basketDepth.Asks.Length.AssertEqual(2);
		basketDepth.Asks[0].Price.AssertEqual(152m);
	}

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Candles_CalculatesOHLC(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 2);
		var processor = CreateProcessor(basket);

		var openTime = new DateTime(2024, 1, 1, 10, 0, 0);

		var lkohCandle = CreateCandle(lkoh, openTime,
			open: 100m, high: 110m, low: 95m, close: 105m, volume: 1000m);

		var sberCandle = CreateCandle(sber, openTime,
			open: 50m, high: 55m, low: 48m, close: 52m, volume: 2000m);

		processor.Process(lkohCandle).ToArray();
		var result = processor.Process(sberCandle).ToArray();

		result.Length.AssertEqual(1);
		var basketCandle = (CandleMessage)result[0];

		basketCandle.SecurityId.AssertEqual(basket.ToSecurityId());
		basketCandle.OpenTime.AssertEqual(openTime);

		// Open = 100 * 1 + 50 * 2 = 200
		basketCandle.OpenPrice.AssertEqual(200m);

		// High = 110 * 1 + 55 * 2 = 220
		basketCandle.HighPrice.AssertEqual(220m);

		// Low = 95 * 1 + 48 * 2 = 191
		basketCandle.LowPrice.AssertEqual(191m);

		// Close = 105 * 1 + 52 * 2 = 209
		basketCandle.ClosePrice.AssertEqual(209m);
	}

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_IncompleteLegs_NoOutput(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 1);
		var processor = CreateProcessor(basket);

		// Only one leg - no output
		var result = processor.Process(CreateTick(lkoh, DateTime.UtcNow, 100m, 10m)).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(nameof(WeightedIndexSecurityProcessor))]
	[DataRow(nameof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_OrderBook_IgnoresStateMessages(string processorName)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorName, lkoh, sber, weight1: 1, weight2: 1);
		var processor = CreateProcessor(basket);

		// State message (reset)
		var stateMsg = new QuoteChangeMessage
		{
			SecurityId = lkoh.ToSecurityId(),
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [],
			Asks = [],
		};

		var result = processor.Process(stateMsg).ToArray();
		result.Length.AssertEqual(0);
	}

	#endregion

	#region Parameterized Continuous Processor Tests

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	[DataRow(nameof(ContinuousSecurityExpirationProcessor))]
	[DataRow(nameof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_SwitchesToNextContract(string processorName)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorName, riu, riz);
		var processor = CreateProcessor(basket);

		var basketId = basket.ToSecurityId();

		if (processorName == nameof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - RIU8 is active
			var beforeExpiry = new DateTime(2024, 9, 10);
			var result1 = processor.Process(CreateTick(riu, beforeExpiry, price: 100000m, volume: 100m)).ToArray();
			result1.Length.AssertEqual(1);
			((ExecutionMessage)result1[0]).TradePrice.AssertEqual(100000m);

			// RIZ8 tick should be ignored before switch
			var result2 = processor.Process(CreateTick(riz, beforeExpiry, price: 100500m, volume: 50m)).ToArray();
			result2.Length.AssertEqual(0);

			// After expiry - RIZ8 becomes active
			var afterExpiry = new DateTime(2024, 9, 16);
			var result3 = processor.Process(CreateTick(riz, afterExpiry, price: 101000m, volume: 80m)).ToArray();
			result3.Length.AssertEqual(1);
			((ExecutionMessage)result3[0]).TradePrice.AssertEqual(101000m);
		}
		else // Volume (VolumeLevel = 100, legs RIU8 -> RIZ8)
		{
			var time = new DateTime(2024, 9, 10);

			// RIU8 starts as active (first in list). Only _currVolume known, _nextVolume null.
			var result1 = processor.Process(CreateTick(riu, time, price: 100000m, volume: 1000m)).ToArray();
			result1.Length.AssertEqual(0); // _nextVolume still null -> CanProcess false

			// RIZ8 with volume 500. Both volumes known now: 1000 + 100 >= 500 -> no switch.
			// The tick belongs to the (still inactive) next leg, so nothing is emitted.
			var result2 = processor.Process(CreateTick(riz, time, price: 100500m, volume: 500m)).ToArray();
			result2.Length.AssertEqual(0);

			// Update RIU8 (volume 900): 900 + 100 >= 500 -> RIU8 still active -> emits.
			var result3 = processor.Process(CreateTick(riu, time.AddSeconds(1), price: 100100m, volume: 900m)).ToArray();
			result3.Length.AssertEqual(1);
			var active3 = (ExecutionMessage)result3[0];
			active3.SecurityId.AssertEqual(basketId);
			active3.TradePrice.AssertEqual(100100m); // RIU8 is still the active leg

			// RIZ8 volume 2000 exceeds RIU8 (900) + level (100) -> switch to RIZ8.
			// After the switch RIZ8 is the active (and last) leg, so its own tick is emitted.
			var result4 = processor.Process(CreateTick(riz, time.AddSeconds(2), price: 101000m, volume: 2000m)).ToArray();
			result4.Length.AssertEqual(1);
			var active4 = (ExecutionMessage)result4[0];
			active4.SecurityId.AssertEqual(basketId);
			active4.TradePrice.AssertEqual(101000m); // switched: RIZ8 is now the active leg
		}
	}

	[TestMethod]
	[DataRow(nameof(ContinuousSecurityExpirationProcessor))]
	[DataRow(nameof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_IgnoresNonLegSecurities(string processorName)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));
		var other = CreateTestSecurity("OTHER", "FORTS");

		var basket = CreateContinuousBasket(processorName, riu, riz);
		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);
		var result = processor.Process(CreateTick(other, time, price: 50000m, volume: 50m)).ToArray();

		// Should be ignored - not a leg
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	[DataRow(nameof(ContinuousSecurityExpirationProcessor))]
	[DataRow(nameof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_ProcessesCandles(string processorName)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorName, riu, riz);
		var processor = CreateProcessor(basket);

		var openTime = new DateTime(2024, 9, 10, 10, 0, 0);

		var candle = CreateCandle(riu, openTime,
			open: 100000m, high: 101000m, low: 99000m, close: 100500m, volume: 5000m);

		var result = processor.Process(candle).ToArray();

		if (processorName == nameof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - should pass through
			result.Length.AssertEqual(1);
			var basketCandle = (CandleMessage)result[0];
			basketCandle.SecurityId.AssertEqual(basket.ToSecurityId());
			basketCandle.OpenPrice.AssertEqual(100000m);
		}
		else // Volume
		{
			// Only the current leg (RIU8) sent data; _nextVolume is still null, so the
			// switch decision cannot be made and CanProcess deterministically returns false.
			result.Length.AssertEqual(0);
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	[DataRow(nameof(ContinuousSecurityExpirationProcessor))]
	[DataRow(nameof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_ProcessesOrderBook(string processorName)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorName, riu, riz);
		var processor = CreateProcessor(basket);

		var serverTime = new DateTime(2024, 9, 10);

		var depth = CreateOrderBook(riu, serverTime,
			bids: [(100000m, 100m)],
			asks: [(100100m, 50m)]);

		var result = processor.Process(depth).ToArray();

		if (processorName == nameof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - should pass through
			result.Length.AssertEqual(1);
			var basketDepth = (QuoteChangeMessage)result[0];
			basketDepth.SecurityId.AssertEqual(basket.ToSecurityId());
		}
		else // Volume
		{
			// Only the current leg (RIU8) book arrived; _nextVolume is still null, so the
			// switch decision cannot be made and CanProcess deterministically returns false.
			result.Length.AssertEqual(0);
		}
	}

	/// <summary>
	/// Smoke test for the order-book path of the continuous processors.
	/// The operator-precedence fix (volume = (volume ?? 0) + bestAsk?.Volume) is actually
	/// exercised by <see cref="VolumeContinuous_OrderBook_BidPlusAskVolume_DrivesSwitch"/>,
	/// which depends on the summed bid+ask volume to make a switching decision.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	[DataRow(nameof(ContinuousSecurityExpirationProcessor))]
	[DataRow(nameof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_OrderBook_SumsBidAndAskVolumes(string processorName)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorName, riu, riz);
		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		var depth = CreateOrderBook(riu, time,
			bids: [(100000m, 100m)],
			asks: [(100100m, 50m)]);

		var result = processor.Process(depth).ToArray();

		if (processorName == nameof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry the active leg's book passes through, re-keyed to the basket id.
			result.Length.AssertEqual(1);
			var basketDepth = (QuoteChangeMessage)result[0];
			basketDepth.SecurityId.AssertEqual(basket.ToSecurityId());
		}
		else // Volume
		{
			// Only the current leg book arrived; _nextVolume is still null, so CanProcess
			// deterministically returns false and nothing is emitted.
			result.Length.AssertEqual(0);
		}
	}

	/// <summary>
	/// Real regression guard for the operator-precedence fix in
	/// <see cref="ContinuousSecurityBaseProcessor{T}"/>:
	/// <code>if (bestAsk?.Volume != null) volume = (volume ?? 0) + bestAsk?.Volume;</code>
	/// The volume processor's switch decision must use the summed bid+ask volume of the next
	/// leg. The data is tuned so that bid+ask (640) crosses the switch threshold while the
	/// bid-only value (590, the buggy parse) would not, making the two interpretations
	/// observably different (switch vs no switch).
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void VolumeContinuous_OrderBook_BidPlusAskVolume_DrivesSwitch()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(100),
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());

		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		// RIU8 (current leg): bid-only book -> _currVolume = 500 (unambiguous).
		var riuBook = processor.Process(CreateOrderBook(riu, time,
			bids: [(100000m, 500m)],
			asks: [])).ToArray();
		riuBook.Length.AssertEqual(0); // _nextVolume still null

		// RIZ8 (next leg): bid 590 + ask 50 = 640 with the correct fix.
		// Threshold check: _currVolume(500) + level(100) = 600.
		//   Correct (sum 640): 600 >= 640 is false -> switch to RIZ8 -> book emitted.
		//   Buggy (bid only 590): 600 >= 590 is true -> no switch -> nothing emitted.
		var rizBook = processor.Process(CreateOrderBook(riz, time,
			bids: [(100500m, 590m)],
			asks: [(100600m, 50m)])).ToArray();

		rizBook.Length.AssertEqual(1);
		((QuoteChangeMessage)rizBook[0]).SecurityId.AssertEqual(basket.ToSecurityId());
	}

	#endregion

	#region Volume Continuous Specific Tests

	[TestMethod]
	public void VolumeContinuous_SwitchesWhenNextVolumeExceeds()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(100), // Switch when next contract volume exceeds current + 100
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());

		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		// RIU8 volume 1000, RIZ8 volume 800 - RIU8 stays active (1000 + 100 > 800)
		processor.Process(CreateTick(riu, time, 100000m, volume: 1000m)).ToArray();
		processor.Process(CreateTick(riz, time, 100500m, volume: 800m)).ToArray();

		// Check RIU8 is still active
		var result1 = processor.Process(CreateTick(riu, time.AddSeconds(1), 100100m, volume: 950m)).ToArray();
		result1.Length.AssertEqual(1);
		((ExecutionMessage)result1[0]).SecurityId.AssertEqual(basket.ToSecurityId());

		// RIZ8 volume exceeds RIU8 + level, switch should happen
		processor.Process(CreateTick(riz, time.AddSeconds(2), 100600m, volume: 1200m)).ToArray();

		// Now RIZ8 should be active
		var result2 = processor.Process(CreateTick(riz, time.AddSeconds(3), 100700m, volume: 1300m)).ToArray();
		result2.Length.AssertEqual(1);
	}

	/// <summary>
	/// Verifies that with <see cref="VolumeContinuousSecurity.IsOpenInterest"/> = true the switch
	/// decision is driven by open interest, not trade volume. The data is deliberately built so the
	/// two metrics disagree: by OI the current leg (RIU8) stays active, but by trade volume RIZ8
	/// would have already taken over. So the test passes only if the engine honors the OI flag.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void VolumeContinuous_UsesOpenInterest_WhenConfigured()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(100),
			IsOpenInterest = true, // Use OI instead of trade volume
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());

		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		// RIU8: high OI (50000) but low trade volume (100).
		var tick1 = new ExecutionMessage
		{
			SecurityId = riu.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = time,
			TradePrice = 100000m,
			TradeVolume = 100m,
			OpenInterest = 50000m,
		};

		// RIZ8: low OI (40000) but high trade volume (5000).
		// By OI:     50000 + 100 >= 40000 -> RIU8 stays active.
		// By volume: 100   + 100 >= 5000  is false -> RIZ8 would have taken over.
		var tick2 = new ExecutionMessage
		{
			SecurityId = riz.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = time,
			TradePrice = 100500m,
			TradeVolume = 5000m,
			OpenInterest = 40000m,
		};

		processor.Process(tick1).ToArray();
		processor.Process(tick2).ToArray();

		// RIU8 update: by OI it is still active (49000 + 100 >= 40000) and must emit.
		// Had the engine wrongly used trade volume, RIZ8 would be active and this RIU8 tick
		// would produce no output.
		var tick3 = new ExecutionMessage
		{
			SecurityId = riu.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = time.AddSeconds(1),
			TradePrice = 100100m,
			TradeVolume = 100m,
			OpenInterest = 49000m,
		};

		var result = processor.Process(tick3).ToArray();
		result.Length.AssertEqual(1);
		var active = (ExecutionMessage)result[0];
		active.SecurityId.AssertEqual(basket.ToSecurityId());
		active.TradePrice.AssertEqual(100100m); // RIU8 is the active leg
	}

	/// <summary>
	/// Three-contract basket, first volume switch RIU8 -> RIZ8. Verifies the switch is taken
	/// (RIZ8 starts being emitted) and that ticks of the now-inactive RIU8 and of the not-yet-active
	/// RIH9 are dropped while RIZ8 is the active leg. A genuine second sequential switch
	/// (RIZ8 -> RIH9) is covered separately because of the volume-reset behavior on rollover.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void VolumeContinuous_ThreeContracts_SwitchesToSecond()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));
		var rih = CreateFuture("RIH9", new DateTime(2025, 3, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(0), // Switch immediately when next exceeds current
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());
		basket.InnerSecurities.Add(rih.ToSecurityId());

		var processor = CreateProcessor(basket);
		var basketId = basket.ToSecurityId();

		var time = new DateTime(2024, 9, 10);

		// Initial state: RIU8 active. Then RIZ8 (vol 500) < RIU8 -> RIU8 stays active.
		processor.Process(CreateTick(riu, time, 100000m, volume: 1000m)).ToArray();
		processor.Process(CreateTick(riz, time, 100500m, volume: 500m)).ToArray();

		// RIU8 still active (900 >= 500) -> emits.
		var result1 = processor.Process(CreateTick(riu, time.AddSeconds(1), 100100m, volume: 900m)).ToArray();
		result1.Length.AssertEqual(1);
		((ExecutionMessage)result1[0]).SecurityId.AssertEqual(basketId);
		((ExecutionMessage)result1[0]).TradePrice.AssertEqual(100100m);

		// RIZ8 volume (1100) exceeds RIU8 (900) -> switch to RIZ8, which is now emitted.
		var switchResult = processor.Process(CreateTick(riz, time.AddSeconds(2), 100600m, volume: 1100m)).ToArray();
		switchResult.Length.AssertEqual(1);
		((ExecutionMessage)switchResult[0]).SecurityId.AssertEqual(basketId);
		((ExecutionMessage)switchResult[0]).TradePrice.AssertEqual(100600m);

		// RIH9 is only the next (inactive) leg now -> its tick must not be emitted.
		var rihResult = processor.Process(CreateTick(rih, time.AddSeconds(3), 101000m, volume: 200m)).ToArray();
		rihResult.Length.AssertEqual(0);

		// RIZ8 remains the active leg -> emits.
		var result2 = processor.Process(CreateTick(riz, time.AddSeconds(4), 100700m, volume: 1000m)).ToArray();
		result2.Length.AssertEqual(1);
		((ExecutionMessage)result2[0]).SecurityId.AssertEqual(basketId);
		((ExecutionMessage)result2[0]).TradePrice.AssertEqual(100700m);
	}

	/// <summary>
	/// Rollover bug guard: when the volume processor switches to the next contract it must
	/// re-base the running volumes onto the new (current, next) pair. After a switch RIU8 -> RIZ8
	/// in a three-contract basket, the very first tick of the newly active RIZ8 (with no fresh
	/// RIH9 volume seen yet) must keep RIZ8 active and emit. The contract: a leg cannot be
	/// switched away from based on its own (or the previous leg's) stale volume.
	///
	/// With the current engine _currVolume/_nextVolume are not reset on switch, so RIZ8's own
	/// tick is compared against the stale RIZ8 volume carried over as _nextVolume, triggering a
	/// false immediate second switch to RIH9 (and _finished). This test asserts the correct
	/// behavior and is therefore expected to fail until that reset is fixed.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void VolumeContinuous_TickOfNewActiveLeg_AfterSwitch_StaysActive()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));
		var rih = CreateFuture("RIH9", new DateTime(2025, 3, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(0), // Switch immediately when next exceeds current
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());
		basket.InnerSecurities.Add(rih.ToSecurityId());

		var processor = CreateProcessor(basket);
		var basketId = basket.ToSecurityId();

		var time = new DateTime(2024, 9, 10);

		// RIU8 active with volume 1000.
		processor.Process(CreateTick(riu, time, 100000m, volume: 1000m)).ToArray();

		// RIZ8 volume 1100 exceeds RIU8 (1000) -> switch to RIZ8 (now the active leg).
		var switchResult = processor.Process(CreateTick(riz, time.AddSeconds(1), 100600m, volume: 1100m)).ToArray();
		switchResult.Length.AssertEqual(1);
		((ExecutionMessage)switchResult[0]).SecurityId.AssertEqual(basketId);

		// First RIZ8 tick after the switch. No RIH9 volume has been observed since the switch,
		// so there is no basis to switch away from RIZ8: it must stay active and emit.
		var afterSwitch = processor.Process(CreateTick(riz, time.AddSeconds(2), 100700m, volume: 1000m)).ToArray();
		afterSwitch.Length.AssertEqual(1);
		((ExecutionMessage)afterSwitch[0]).SecurityId.AssertEqual(basketId);
		((ExecutionMessage)afterSwitch[0]).TradePrice.AssertEqual(100700m);
	}

	#endregion

	#region WeightedIndexSecurity Specific Tests

	[TestMethod]
	public void WeightedIndex_Ticks_NegativeWeight()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: -1);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;

		processor.Process(CreateTick(lkoh, serverTime, price: 100m, volume: 10m)).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, price: 30m, volume: 5m)).ToArray();

		var basketTick = (ExecutionMessage)result[0];

		// Price = 100 * 1 + 30 * (-1) = 70
		basketTick.TradePrice.AssertEqual(70m);
	}

	[TestMethod]
	public void WeightedIndex_Ticks_FractionalWeights()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 0.5m, sberWeight: 0.5m);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;

		processor.Process(CreateTick(lkoh, serverTime, price: 100m, volume: 10m)).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, price: 200m, volume: 20m)).ToArray();

		var basketTick = (ExecutionMessage)result[0];

		// Price = 100 * 0.5 + 200 * 0.5 = 150
		basketTick.TradePrice.AssertEqual(150m);
	}

	[TestMethod]
	public void WeightedIndex_OrderBook_DifferentDepths()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;

		// LKOH has 3 levels
		var lkohDepth = CreateOrderBook(lkoh, serverTime,
			bids: [(100m, 10m), (99m, 20m), (98m, 30m)],
			asks: [(101m, 15m)]);

		// SBER has 1 level
		var sberDepth = CreateOrderBook(sber, serverTime,
			bids: [(50m, 5m)],
			asks: [(51m, 8m), (52m, 12m), (53m, 20m)]);

		processor.Process(lkohDepth).ToArray();
		var result = processor.Process(sberDepth).ToArray();

		var basketDepth = (QuoteChangeMessage)result[0];

		// Min depth used - SBER has 1 bid, LKOH has 1 ask
		basketDepth.Bids.Length.AssertEqual(1);
		basketDepth.Asks.Length.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void WeightedIndex_Candles_HighLowNormalization()
	{
		// When the weighted calculation produces High < Low (because a leg with a negative
		// weight has a wider range than the positive leg), FillIndexCandle must swap them so
		// the basket candle stays consistent (BasketSecurityBaseProcessor: HighPrice < LowPrice).
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: -1);
		var processor = CreateProcessor(basket);

		var openTime = new DateTime(2024, 1, 1, 10, 0, 0);

		// lkoh (weight +1): narrow range around 10.
		var lkohCandle = CreateCandle(lkoh, openTime,
			open: 10m, high: 10m, low: 9m, close: 10m, volume: 1000m);

		// sber (weight -1): wide range. Subtracting its wide High/Low inverts the index range.
		var sberCandle = CreateCandle(sber, openTime,
			open: 12m, high: 20m, low: 5m, close: 12m, volume: 500m);

		processor.Process(lkohCandle).ToArray();
		var result = processor.Process(sberCandle).ToArray();

		result.Length.AssertEqual(1);
		var basketCandle = (CandleMessage)result[0];

		// Raw weighted parts (before normalization):
		//   Open  = 10*1 + 12*(-1) = -2
		//   High  = 10*1 + 20*(-1) = -10
		//   Low   =  9*1 +  5*(-1) =  4
		//   Close = 10*1 + 12*(-1) = -2
		// High (-10) < Low (4) -> swap fires -> High = 4, Low = -10.
		// Open/Close (-2) lie inside [-10, 4], so no further clamping occurs.
		basketCandle.OpenPrice.AssertEqual(-2m);
		basketCandle.ClosePrice.AssertEqual(-2m);
		basketCandle.HighPrice.AssertEqual(4m);
		basketCandle.LowPrice.AssertEqual(-10m);

		// Sanity: invariant restored by the swap.
		(basketCandle.HighPrice >= basketCandle.LowPrice).AssertTrue();
	}

	[TestMethod]
	public void WeightedIndex_ClonedMessages()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processor = CreateProcessor(basket);

		var serverTime = DateTime.UtcNow;
		var originalTick = CreateTick(lkoh, serverTime, 100m, 10m);

		processor.Process(originalTick).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, 50m, 5m)).ToArray();

		// Output message should have basket's SecurityId
		var basketTick = (ExecutionMessage)result[0];
		basketTick.SecurityId.AssertNotEqual(originalTick.SecurityId);
		basketTick.SecurityId.AssertEqual(basket.ToSecurityId());
	}

	#endregion

	#region ExpressionIndexSecurity Specific Tests

	[TestMethod]
	public void ExpressionIndex_Ticks_SubtractionExpression()
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");

		var basket = new ExpressionIndexSecurity
		{
			Id = "LKOH_SBER_EXP@TQBR",
			Board = ExchangeBoard.MicexTqbr,
			BasketExpression = "LKOH@TQBR - 2 * SBER@TQBR",
		};

		var processor = CreateProcessor(basket);
		var serverTime = DateTime.UtcNow;

		processor.Process(CreateTick(lkoh, serverTime, price: 100m, volume: 10m)).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, price: 30m, volume: 5m)).ToArray();

		result.Length.AssertEqual(1);
		var basketTick = (ExecutionMessage)result[0];

		// Price = 100 - 2 * 30 = 40
		basketTick.TradePrice.AssertEqual(40m);
	}

	[TestMethod]
	public void ExpressionIndex_Ticks_DivisionExpression()
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");

		var basket = new ExpressionIndexSecurity
		{
			Id = "LKOH_SBER_RATIO@TQBR",
			Board = ExchangeBoard.MicexTqbr,
			BasketExpression = "LKOH@TQBR / SBER@TQBR",
		};

		var processor = CreateProcessor(basket);
		var serverTime = DateTime.UtcNow;

		processor.Process(CreateTick(lkoh, serverTime, price: 100m, volume: 10m)).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, price: 50m, volume: 5m)).ToArray();

		var basketTick = (ExecutionMessage)result[0];

		// Price = 100 / 50 = 2
		basketTick.TradePrice.AssertEqual(2m);
	}

	#endregion

	#region ToBasket Extension Method Tests

	[TestMethod]
	public void ToBasket_Sync_ProcessesTickSequence()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: -1);
		var processorProvider = new BasketSecurityProcessorProvider();

		var serverTime = DateTime.UtcNow;
		var ticks = new ExecutionMessage[]
		{
			CreateTick(lkoh, serverTime, 100m, 10m),
			CreateTick(sber, serverTime, 30m, 5m),
			CreateTick(lkoh, serverTime.AddSeconds(1), 102m, 12m),
			CreateTick(sber, serverTime.AddSeconds(1), 32m, 6m),
		};

		var result = ticks.ToBasket(basket, processorProvider).ToArray();

		result.Length.AssertEqual(2);
		((ExecutionMessage)result[0]).TradePrice.AssertEqual(70m);  // 100 - 30
		((ExecutionMessage)result[1]).TradePrice.AssertEqual(70m);  // 102 - 32
	}

	[TestMethod]
	public async Task ToBasket_Async_ProcessesTickSequence()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 2, sberWeight: 1);
		var processorProvider = new BasketSecurityProcessorProvider();

		var serverTime = DateTime.UtcNow;
		var ticks = new ExecutionMessage[]
		{
			CreateTick(lkoh, serverTime, 100m, 10m),
			CreateTick(sber, serverTime, 50m, 5m),
		};

		var result = await ticks.ToAsyncEnumerable()
			.ToBasket(basket, processorProvider)
			.ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(1);
		result[0].TradePrice.AssertEqual(250m); // 100*2 + 50*1
	}

	[TestMethod]
	public void ToBasket_Sync_ProcessesCandleSequence()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processorProvider = new BasketSecurityProcessorProvider();

		var openTime = new DateTime(2024, 1, 1, 10, 0, 0);

		var candles = new CandleMessage[]
		{
			CreateCandle(lkoh, openTime, 100m, 110m, 90m, 105m, 1000m),
			CreateCandle(sber, openTime, 50m, 55m, 45m, 52m, 500m),
		};

		var result = candles.ToBasket(basket, processorProvider).ToArray();

		result.Length.AssertEqual(1);
		var basketCandle = (CandleMessage)result[0];
		basketCandle.OpenPrice.AssertEqual(150m);
		basketCandle.ClosePrice.AssertEqual(157m);
	}

	[TestMethod]
	public void ToBasket_Sync_ProcessesOrderBookSequence()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processorProvider = new BasketSecurityProcessorProvider();

		var serverTime = DateTime.UtcNow;
		var orderBooks = new QuoteChangeMessage[]
		{
			CreateOrderBook(lkoh, serverTime, [(100m, 10m)], [(101m, 15m)]),
			CreateOrderBook(sber, serverTime, [(50m, 5m)], [(51m, 8m)]),
		};

		var result = orderBooks.ToBasket(basket, processorProvider).ToArray();

		result.Length.AssertEqual(1);
		var basketDepth = (QuoteChangeMessage)result[0];
		basketDepth.Bids[0].Price.AssertEqual(150m);
		basketDepth.Asks[0].Price.AssertEqual(152m);
	}

	#endregion

	#region Helper Methods

	private static (Security basket, decimal expectedPrice) CreateIndexBasket(
		string processorName,
		Security security1,
		Security security2,
		decimal weight1,
		decimal weight2)
	{
		Security basket;
		decimal expectedPrice;

		if (processorName == nameof(WeightedIndexSecurityProcessor))
		{
			var weightedBasket = new WeightedIndexSecurity
			{
				Id = $"{security1.Code}_{security2.Code}_WEI@TQBR",
				Board = ExchangeBoard.Associated,
			};
			weightedBasket.Weights[security1.ToSecurityId()] = weight1;
			weightedBasket.Weights[security2.ToSecurityId()] = weight2;
			basket = weightedBasket;
			// For test: 100 * weight1 + 50 * weight2
			expectedPrice = 100m * weight1 + 50m * weight2;
		}
		else if (processorName == nameof(ExpressionIndexSecurityProcessor))
		{
			var expressionBasket = new ExpressionIndexSecurity
			{
				Id = $"{security1.Code}_{security2.Code}_EXP@TQBR",
				Board = ExchangeBoard.MicexTqbr,
				BasketExpression = $"{security1.Id} + {weight2} * {security2.Id}",
			};
			basket = expressionBasket;
			// For test: 100 + weight2 * 50
			expectedPrice = 100m + weight2 * 50m;
		}
		else
			throw new ArgumentException($"Unknown processor name: {processorName}");

		return (basket, expectedPrice);
	}

	private static Security CreateContinuousBasket(
		string processorName,
		Security future1,
		Security future2)
	{
		if (processorName == nameof(ContinuousSecurityExpirationProcessor))
		{
			var basket = new ExpirationContinuousSecurity
			{
				Id = "RI@FORTS",
				Board = ExchangeBoard.Forts,
			};
			basket.ExpirationJumps.Add(future1.ToSecurityId(), future1.ExpiryDate!.Value);
			basket.ExpirationJumps.Add(future2.ToSecurityId(), future2.ExpiryDate!.Value);
			return basket;
		}
		else if (processorName == nameof(ContinuousSecurityVolumeProcessor))
		{
			var basket = new VolumeContinuousSecurity
			{
				Id = "RI@FORTS",
				Board = ExchangeBoard.Forts,
				VolumeLevel = new Unit(100),
			};
			basket.InnerSecurities.Add(future1.ToSecurityId());
			basket.InnerSecurities.Add(future2.ToSecurityId());
			return basket;
		}
		else
			throw new ArgumentException($"Unknown processor name: {processorName}");
	}

	private static (Security basket, Security lkoh, Security sber) CreateWeightedBasket(
		decimal lkohWeight, decimal sberWeight)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = lkohWeight;
		basket.Weights[sber.ToSecurityId()] = sberWeight;

		return (basket, lkoh, sber);
	}

	private static Security CreateTestSecurity(string code, string board)
	{
		return new Security
		{
			Id = $"{code}@{board}",
			Code = code,
			Board = new ExchangeBoard { Code = board, Exchange = Exchange.Test },
		};
	}

	private static Security CreateFuture(string code, DateTime expiryDate)
	{
		return new Security
		{
			Id = $"{code}@FORTS",
			Code = code,
			Board = ExchangeBoard.Forts,
			ExpiryDate = expiryDate,
		};
	}

	private static IBasketSecurityProcessor CreateProcessor(Security basket)
	{
		return new BasketSecurityProcessorProvider().CreateProcessor(basket);
	}

	private static ExecutionMessage CreateTick(Security security, DateTime serverTime, decimal price, decimal volume)
	{
		return new ExecutionMessage
		{
			SecurityId = security.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = serverTime,
			TradePrice = price,
			TradeVolume = volume,
		};
	}

	private static QuoteChangeMessage CreateOrderBook(
		Security security,
		DateTime serverTime,
		(decimal price, decimal volume)[] bids,
		(decimal price, decimal volume)[] asks)
	{
		return new QuoteChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = serverTime,
			Bids = [.. bids.Select(b => new QuoteChange(b.price, b.volume))],
			Asks = [.. asks.Select(a => new QuoteChange(a.price, a.volume))],
		};
	}

	private static TimeFrameCandleMessage CreateCandle(
		Security security,
		DateTime openTime,
		decimal open, decimal high, decimal low, decimal close,
		decimal volume)
	{
		return new TimeFrameCandleMessage
		{
			SecurityId = security.ToSecurityId(),
			DataType = TimeSpan.FromMinutes(1).TimeFrame(),
			OpenTime = openTime,
			CloseTime = openTime.AddMinutes(1),
			OpenPrice = open,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = close,
			TotalVolume = volume,
			State = CandleStates.Finished,
		};
	}

	#endregion

	#region Provider Registration Tests

	[TestMethod]
	public void Provider_AllCodes_ReturnsDefaultCodes()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var codes = provider.AllCodes.ToArray();

		codes.Length.AssertEqual(4);
		codes.Count(c => c == BasketCodes.ExpirationContinuous).AssertEqual(1);
		codes.Count(c => c == BasketCodes.VolumeContinuous).AssertEqual(1);
		codes.Count(c => c == BasketCodes.WeightedIndex).AssertEqual(1);
		codes.Count(c => c == BasketCodes.ExpressionIndex).AssertEqual(1);
	}

	[TestMethod]
	public void Provider_TryGetProcessorType_ReturnsTrue_ForKnownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var result = provider.TryGetProcessorType(BasketCodes.WeightedIndex, out var processorType);

		result.AssertTrue();
		processorType.AssertEqual(typeof(WeightedIndexSecurityProcessor));
	}

	[TestMethod]
	public void Provider_TryGetProcessorType_ReturnsFalse_ForUnknownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var result = provider.TryGetProcessorType("UNKNOWN", out var processorType);

		result.AssertFalse();
		IsNull(processorType);
	}

	[TestMethod]
	public void Provider_TryGetSecurityType_ReturnsTrue_ForKnownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var result = provider.TryGetSecurityType(BasketCodes.ExpressionIndex, out var securityType);

		result.AssertTrue();
		securityType.AssertEqual(typeof(ExpressionIndexSecurity));
	}

	[TestMethod]
	public void Provider_TryGetSecurityType_ReturnsFalse_ForUnknownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var result = provider.TryGetSecurityType("UNKNOWN", out var securityType);

		result.AssertFalse();
		IsNull(securityType);
	}

	[TestMethod]
	public void Provider_GetProcessorType_ThrowsForUnknownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		ThrowsExactly<ArgumentException>(() => provider.GetProcessorType("UNKNOWN"));
	}

	[TestMethod]
	public void Provider_GetSecurityType_ThrowsForUnknownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		ThrowsExactly<ArgumentException>(() => provider.GetSecurityType("UNKNOWN"));
	}

	[TestMethod]
	public void Provider_Register_AddsNewCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		provider.Register("CUSTOM", typeof(CustomBasketProcessor), typeof(CustomBasketSecurity));

		provider.AllCodes.Count(c => c == "CUSTOM").AssertEqual(1);

		provider.TryGetProcessorType("CUSTOM", out var processorType).AssertTrue();
		processorType.AssertEqual(typeof(CustomBasketProcessor));

		provider.TryGetSecurityType("CUSTOM", out var securityType).AssertTrue();
		securityType.AssertEqual(typeof(CustomBasketSecurity));
	}

	[TestMethod]
	public void Provider_Register_ThrowsForNullCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Register(null, typeof(CustomBasketProcessor), typeof(CustomBasketSecurity)));
		ThrowsExactly<ArgumentNullException>(() => provider.Register(string.Empty, typeof(CustomBasketProcessor), typeof(CustomBasketSecurity)));
	}

	[TestMethod]
	public void Provider_Register_ThrowsForNullTypes()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Register("X", null, typeof(CustomBasketSecurity)));
		ThrowsExactly<ArgumentNullException>(() => provider.Register("X", typeof(CustomBasketProcessor), null));
	}

	[TestMethod]
	public void Provider_UnRegister_RemovesCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		provider.Register("CUSTOM", typeof(CustomBasketProcessor), typeof(CustomBasketSecurity));
		provider.AllCodes.Count(c => c == "CUSTOM").AssertEqual(1);

		var result = provider.UnRegister("CUSTOM");

		result.AssertTrue();
		provider.TryGetProcessorType("CUSTOM", out _).AssertFalse();
	}

	[TestMethod]
	public void Provider_UnRegister_ReturnsFalseForUnknownCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		var result = provider.UnRegister("UNKNOWN");

		result.AssertFalse();
	}

	[TestMethod]
	public void Provider_UnRegister_ThrowsForNullCode()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.UnRegister(null));
		ThrowsExactly<ArgumentNullException>(() => provider.UnRegister(string.Empty));
	}

	[TestMethod]
	public void Provider_CaseInsensitive()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		provider.TryGetProcessorType("wi", out var type1).AssertTrue();
		provider.TryGetProcessorType("WI", out var type2).AssertTrue();
		provider.TryGetProcessorType("Wi", out var type3).AssertTrue();

		type1.AssertEqual(typeof(WeightedIndexSecurityProcessor));
		type2.AssertEqual(typeof(WeightedIndexSecurityProcessor));
		type3.AssertEqual(typeof(WeightedIndexSecurityProcessor));
	}

	#endregion

	#region Custom Basket Processor Tests

	[TestMethod]
	public void CustomProcessor_ProcessesTicks()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		provider.Register("CUSTOM", typeof(CustomBasketProcessor), typeof(CustomBasketSecurity));

		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");

		var basket = new CustomBasketSecurity
		{
			Id = "CUSTOM_BASKET@TQBR",
			Board = ExchangeBoard.MicexTqbr,
			Multiplier = 10m,
		};
		basket.SecurityIds.Add(lkoh.ToSecurityId());
		basket.SecurityIds.Add(sber.ToSecurityId());

		var processor = provider.CreateProcessor(basket);
		processor.GetType().AssertEqual(typeof(CustomBasketProcessor));

		var serverTime = DateTime.UtcNow;

		processor.Process(CreateTick(lkoh, serverTime, 100m, 10m)).ToArray();
		var result = processor.Process(CreateTick(sber, serverTime, 50m, 5m)).ToArray();

		result.Length.AssertEqual(1);
		var basketTick = (ExecutionMessage)result[0];

		// Custom processor: (100 + 50) * 10 = 1500
		basketTick.TradePrice.AssertEqual(1500m);
	}

	[TestMethod]
	public void CustomProcessor_ToBasket_ConvertsSecurity()
	{
		IBasketSecurityProcessorProvider provider = new BasketSecurityProcessorProvider();

		provider.Register("CUSTOM", typeof(CustomBasketProcessor), typeof(CustomBasketSecurity));

		var security = new Security
		{
			Id = "TEST@TQBR",
			Code = "TEST",
			Board = ExchangeBoard.MicexTqbr,
			BasketCode = "CUSTOM",
			BasketExpression = "10,LKOH@TQBR,SBER@TQBR",
		};

		var basketSecurity = security.ToBasket(provider);

		basketSecurity.GetType().AssertEqual(typeof(CustomBasketSecurity));
		var custom = (CustomBasketSecurity)basketSecurity;
		custom.Multiplier.AssertEqual(10m);
		custom.SecurityIds.Count.AssertEqual(2);
	}

	#endregion

	#region Custom Basket Security & Processor (Test Classes)

	[BasketCode("CUSTOM")]
	private class CustomBasketSecurity : IndexSecurity
	{
		public new decimal Multiplier { get; set; } = 1m;

		public SynchronizedList<SecurityId> SecurityIds { get; } = [];

		public override IEnumerable<SecurityId> InnerSecurityIds => SecurityIds;

		protected override string ToSerializedString()
		{
			return $"{Multiplier},{SecurityIds.Select(s => s.ToStringId()).JoinComma()}";
		}

		protected override void FromSerializedString(string text)
		{
			var parts = text.SplitByComma();
			Multiplier = parts[0].To<decimal>();

			SecurityIds.Clear();
			SecurityIds.AddRange(parts.Skip(1).Select(p => p.ToSecurityId()));
		}
	}

	private class CustomBasketProcessor : IndexSecurityBaseProcessor<CustomBasketSecurity>
	{
		public CustomBasketProcessor(BasketSecurity security)
			: base(security)
		{
		}

		protected override decimal OnCalculate(decimal[] prices)
		{
			return prices.Sum() * BasketSecurity.Multiplier;
		}
	}

	#endregion
}
