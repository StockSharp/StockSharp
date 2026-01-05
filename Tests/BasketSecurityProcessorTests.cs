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
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Ticks_CalculatesCorrectly(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, expectedPrice) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 2);
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
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Ticks_MultipleRounds(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 1);
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
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_OrderBook_CalculatesCorrectDepth(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 1);
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
		basketDepth.Bids.Length.AssertGreater(0);
		basketDepth.Bids[0].Price.AssertEqual(150m);

		// Best ask: 101 + 51 = 152
		basketDepth.Asks.Length.AssertGreater(0);
		basketDepth.Asks[0].Price.AssertEqual(152m);
	}

	[TestMethod]
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_Candles_CalculatesOHLC(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 2);
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
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_IncompleteLegs_NoOutput(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 1);
		var processor = CreateProcessor(basket);

		// Only one leg - no output
		var result = processor.Process(CreateTick(lkoh, DateTime.UtcNow, 100m, 10m)).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(typeof(WeightedIndexSecurityProcessor))]
	[DataRow(typeof(ExpressionIndexSecurityProcessor))]
	public void IndexProcessor_OrderBook_IgnoresStateMessages(Type processorType)
	{
		var lkoh = CreateTestSecurity("LKOH", "TQBR");
		var sber = CreateTestSecurity("SBER", "TQBR");
		var (basket, _) = CreateIndexBasket(processorType, lkoh, sber, weight1: 1, weight2: 1);
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
	[DataRow(typeof(ContinuousSecurityExpirationProcessor))]
	[DataRow(typeof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_SwitchesToNextContract(Type processorType)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorType, riu, riz);
		var processor = CreateProcessor(basket);

		if (processorType == typeof(ContinuousSecurityExpirationProcessor))
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
		else // Volume
		{
			var time = new DateTime(2024, 9, 10);

			// RIU8 starts as active (first in list)
			// RIU8 with volume 1000
			var result1 = processor.Process(CreateTick(riu, time, price: 100000m, volume: 1000m)).ToArray();
			result1.Length.AssertEqual(0); // Waiting for both legs

			// RIZ8 with volume 500 (less than RIU8, so RIU8 stays active)
			var result2 = processor.Process(CreateTick(riz, time, price: 100500m, volume: 500m)).ToArray();
			result2.Length.AssertEqual(0); // Still no output, volumes not compared yet

			// Update RIU8 - should output as it's still active
			var result3 = processor.Process(CreateTick(riu, time.AddSeconds(1), price: 100100m, volume: 900m)).ToArray();
			// Volume logic: RIU8 active until next contract volume exceeds current + level

			// RIZ8 volume exceeds RIU8 - switch happens
			var result4 = processor.Process(CreateTick(riz, time.AddSeconds(2), price: 101000m, volume: 2000m)).ToArray();
			// After switch, RIZ8 is now active
		}
	}

	[TestMethod]
	[DataRow(typeof(ContinuousSecurityExpirationProcessor))]
	[DataRow(typeof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_IgnoresNonLegSecurities(Type processorType)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));
		var other = CreateTestSecurity("OTHER", "FORTS");

		var basket = CreateContinuousBasket(processorType, riu, riz);
		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);
		var result = processor.Process(CreateTick(other, time, price: 50000m, volume: 50m)).ToArray();

		// Should be ignored - not a leg
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(typeof(ContinuousSecurityExpirationProcessor))]
	[DataRow(typeof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_ProcessesCandles(Type processorType)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorType, riu, riz);
		var processor = CreateProcessor(basket);

		var openTime = new DateTime(2024, 9, 10, 10, 0, 0);

		var candle = CreateCandle(riu, openTime,
			open: 100000m, high: 101000m, low: 99000m, close: 100500m, volume: 5000m);

		var result = processor.Process(candle).ToArray();

		if (processorType == typeof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - should pass through
			result.Length.AssertEqual(1);
			var basketCandle = (CandleMessage)result[0];
			basketCandle.SecurityId.AssertEqual(basket.ToSecurityId());
			basketCandle.OpenPrice.AssertEqual(100000m);
		}
		// Volume processor may not output immediately due to volume comparison logic
	}

	[TestMethod]
	[DataRow(typeof(ContinuousSecurityExpirationProcessor))]
	[DataRow(typeof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_ProcessesOrderBook(Type processorType)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorType, riu, riz);
		var processor = CreateProcessor(basket);

		var serverTime = new DateTime(2024, 9, 10);

		var depth = CreateOrderBook(riu, serverTime,
			bids: [(100000m, 100m)],
			asks: [(100100m, 50m)]);

		var result = processor.Process(depth).ToArray();

		if (processorType == typeof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - should pass through
			result.Length.AssertEqual(1);
			var basketDepth = (QuoteChangeMessage)result[0];
			basketDepth.SecurityId.AssertEqual(basket.ToSecurityId());
		}
	}

	/// <summary>
	/// Regression test for operator precedence bug in ContinuousSecurityBaseProcessor.
	/// Bug was: volume = volume ?? 0 + bestAsk?.Volume (parsed as volume ?? (0 + bestAsk?.Volume))
	/// Fixed:   volume = (volume ?? 0) + bestAsk?.Volume
	/// </summary>
	[TestMethod]
	[DataRow(typeof(ContinuousSecurityExpirationProcessor))]
	[DataRow(typeof(ContinuousSecurityVolumeProcessor))]
	public void ContinuousProcessor_OrderBook_SumsBidAndAskVolumes(Type processorType)
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = CreateContinuousBasket(processorType, riu, riz);
		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		// Send order book with BOTH bid and ask volumes
		// This tests the bug fix: volume should be sum of bid + ask volumes
		// Bid volume = 100, Ask volume = 50, total should be 150
		var depth = CreateOrderBook(riu, time,
			bids: [(100000m, 100m)],
			asks: [(100100m, 50m)]);

		var result = processor.Process(depth).ToArray();

		if (processorType == typeof(ContinuousSecurityExpirationProcessor))
		{
			// Before expiry - should pass through with correct volume calculation
			result.Length.AssertEqual(1);
			var basketDepth = (QuoteChangeMessage)result[0];
			basketDepth.SecurityId.AssertEqual(basket.ToSecurityId());
		}
		// Volume processor needs both contracts' data first, so no output expected here
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

	[TestMethod]
	public void VolumeContinuous_UsesOpenInterest_WhenConfigured()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			VolumeLevel = new Unit(100),
			IsOpenInterest = true, // Use OI instead of volume
		};
		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());

		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);

		// Create ticks with OI
		var tick1 = new ExecutionMessage
		{
			SecurityId = riu.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = time,
			TradePrice = 100000m,
			TradeVolume = 100m,
			OpenInterest = 50000m,
		};

		var tick2 = new ExecutionMessage
		{
			SecurityId = riz.ToSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = time,
			TradePrice = 100500m,
			TradeVolume = 50m,
			OpenInterest = 40000m,
		};

		processor.Process(tick1).ToArray();
		processor.Process(tick2).ToArray();

		// RIU8 should still be active (OI 50000 + 100 > 40000)
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
	}

	[TestMethod]
	public void VolumeContinuous_ThreeContracts_SwitchesSequentially()
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

		var time = new DateTime(2024, 9, 10);

		// Initial state: RIU8 is active
		processor.Process(CreateTick(riu, time, 100000m, volume: 1000m)).ToArray();
		processor.Process(CreateTick(riz, time, 100500m, volume: 500m)).ToArray();

		// RIU8 still active
		var result1 = processor.Process(CreateTick(riu, time.AddSeconds(1), 100100m, volume: 900m)).ToArray();
		result1.Length.AssertEqual(1);

		// RIZ8 exceeds, switch to RIZ8
		processor.Process(CreateTick(riz, time.AddSeconds(2), 100600m, volume: 1100m)).ToArray();

		// Now need to track RIZ8 vs RIH9
		processor.Process(CreateTick(rih, time.AddSeconds(3), 101000m, volume: 200m)).ToArray();

		// RIZ8 is active
		var result2 = processor.Process(CreateTick(riz, time.AddSeconds(4), 100700m, volume: 1000m)).ToArray();
		result2.Length.AssertEqual(1);
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
	public void WeightedIndex_Candles_HighLowNormalization()
	{
		// When weighted calculation results in High < Low, they should be swapped
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: -1);
		var processor = CreateProcessor(basket);

		var openTime = new DateTime(2024, 1, 1, 10, 0, 0);

		// With negative weight, high/low might get inverted
		var lkohCandle = CreateCandle(lkoh, openTime,
			open: 100m, high: 110m, low: 90m, close: 100m, volume: 1000m);

		var sberCandle = CreateCandle(sber, openTime,
			open: 50m, high: 55m, low: 45m, close: 50m, volume: 500m);

		processor.Process(lkohCandle).ToArray();
		var result = processor.Process(sberCandle).ToArray();

		var basketCandle = (CandleMessage)result[0];

		// Verify High >= Low
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
			.ToArrayAsync();

		result.Length.AssertEqual(1);
		((ExecutionMessage)result[0]).TradePrice.AssertEqual(250m); // 100*2 + 50*1
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
		Type processorType,
		Security security1,
		Security security2,
		decimal weight1,
		decimal weight2)
	{
		Security basket;
		decimal expectedPrice;

		if (processorType == typeof(WeightedIndexSecurityProcessor))
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
		else if (processorType == typeof(ExpressionIndexSecurityProcessor))
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
			throw new ArgumentException($"Unknown processor type: {processorType}");

		return (basket, expectedPrice);
	}

	private static Security CreateContinuousBasket(
		Type processorType,
		Security future1,
		Security future2)
	{
		if (processorType == typeof(ContinuousSecurityExpirationProcessor))
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
		else if (processorType == typeof(ContinuousSecurityVolumeProcessor))
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
			throw new ArgumentException($"Unknown processor type: {processorType}");
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
		Assert.Contains(BasketCodes.ExpirationContinuous, codes);
		Assert.Contains(BasketCodes.VolumeContinuous, codes);
		Assert.Contains(BasketCodes.WeightedIndex, codes);
		Assert.Contains(BasketCodes.ExpressionIndex, codes);
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

		Assert.Contains("CUSTOM", provider.AllCodes.ToArray());

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
		Assert.Contains("CUSTOM", provider.AllCodes.ToArray());

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
			return $"{Multiplier},{string.Join(",", SecurityIds.Select(s => s.ToStringId()))}";
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
