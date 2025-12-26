namespace StockSharp.Tests;

/// <summary>
/// Comprehensive tests for basket security processors.
/// Tests WeightedIndexSecurity, ExpressionIndexSecurity, ContinuousSecurity processing.
/// </summary>
[TestClass]
public class BasketSecurityProcessorTests : BaseTestClass
{
	#region WeightedIndexSecurity Tick Tests

	[TestMethod]
	public void WeightedIndex_Ticks_CalculatesWeightedPrice()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 2);
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

		// Price = 100 * 1 + 50 * 2 = 200
		basketTick.TradePrice.AssertEqual(200m);
		// Volume = 10 * 1 + 20 * 2 = 50 (но тут нужно проверить логику)
	}

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
	public void WeightedIndex_Ticks_MultipleRounds()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
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

	#endregion

	#region WeightedIndexSecurity OrderBook Tests

	[TestMethod]
	public void WeightedIndex_OrderBook_CalculatesWeightedDepth()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
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

	#endregion

	#region WeightedIndexSecurity Candle Tests

	[TestMethod]
	public void WeightedIndex_Candles_CalculatesWeightedOHLC()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 2);
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
	public void WeightedIndex_Candles_MultipleCandles()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processor = CreateProcessor(basket);

		var time1 = new DateTime(2024, 1, 1, 10, 0, 0);
		var time2 = new DateTime(2024, 1, 1, 10, 1, 0);

		// First candle
		processor.Process(CreateCandle(lkoh, time1, 100m, 110m, 90m, 105m, 1000m)).ToArray();
		var result1 = processor.Process(CreateCandle(sber, time1, 50m, 55m, 45m, 52m, 500m)).ToArray();

		result1.Length.AssertEqual(1);
		((CandleMessage)result1[0]).OpenPrice.AssertEqual(150m);

		// Second candle
		processor.Process(CreateCandle(lkoh, time2, 105m, 115m, 100m, 112m, 1200m)).ToArray();
		var result2 = processor.Process(CreateCandle(sber, time2, 52m, 58m, 50m, 56m, 600m)).ToArray();

		result2.Length.AssertEqual(1);
		((CandleMessage)result2[0]).OpenPrice.AssertEqual(157m);
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

	#endregion

	#region ExpressionIndexSecurity Tests

	[TestMethod]
	public void ExpressionIndex_Ticks_EvaluatesExpression()
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

	#region ContinuousSecurity Tests (Expiration-based)

	[TestMethod]
	public void ExpirationContinuous_SwitchesOnExpiry()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));

		var basket = new ExpirationContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
		};
		basket.ExpirationJumps.Add(riu.ToSecurityId(), riu.ExpiryDate.Value);
		basket.ExpirationJumps.Add(riz.ToSecurityId(), riz.ExpiryDate.Value);

		var processor = CreateProcessor(basket);

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

	[TestMethod]
	public void ExpirationContinuous_IgnoresNonLegSecurities()
	{
		var riu = CreateFuture("RIU8", new DateTime(2024, 9, 15));
		var riz = CreateFuture("RIZ8", new DateTime(2024, 12, 15));
		var other = CreateTestSecurity("OTHER", "FORTS");

		var basket = new ExpirationContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
		};
		basket.ExpirationJumps.Add(riu.ToSecurityId(), riu.ExpiryDate.Value);
		basket.ExpirationJumps.Add(riz.ToSecurityId(), riz.ExpiryDate.Value);

		var processor = CreateProcessor(basket);

		var time = new DateTime(2024, 9, 10);
		var result = processor.Process(CreateTick(other, time, price: 50000m, volume: 50m)).ToArray();

		// Should be ignored - not a leg
		result.Length.AssertEqual(0);
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void WeightedIndex_IncompleteLegs_NoOutput()
	{
		var (basket, lkoh, sber) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
		var processor = CreateProcessor(basket);

		// Only one leg - no output
		var result = processor.Process(CreateTick(lkoh, DateTime.UtcNow, 100m, 10m)).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void WeightedIndex_OrderBook_IgnoresStateMessages()
	{
		var (basket, lkoh, _) = CreateWeightedBasket(lkohWeight: 1, sberWeight: 1);
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
}
