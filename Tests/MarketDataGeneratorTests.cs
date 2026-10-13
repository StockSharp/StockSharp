namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;

[TestClass]
public class MarketDataGeneratorTests : BaseTestClass
{
	private static SecurityMessage CreateSecurityMessage(SecurityId secId, decimal priceStep = 0.01m, decimal volumeStep = 1m)
	{
		return new SecurityMessage
		{
			SecurityId = secId,
			PriceStep = priceStep,
			VolumeStep = volumeStep,
		};
	}

	private static BoardMessage CreateBoardMessage()
	{
		return new BoardMessage
		{
			Code = "TEST",
			ExchangeCode = "TEST",
			WorkingTime = new WorkingTime
			{
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = DateTime.MaxValue,
						Times = [new Range<TimeSpan>(TimeSpan.Zero, TimeSpan.FromDays(1))]
					}
				]
			}
		};
	}

	#region RandomWalkTradeGenerator Tests

	[TestMethod]
	public void TradeGenerator_Init_SetsDefaultValues()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		generator.Init();

		generator.DataType.AssertEqual(DataType.Ticks);
		generator.SecurityId.AssertEqual(secId);
		generator.MinVolume.AssertEqual(1);
		generator.MaxVolume.AssertEqual(20);
		generator.MaxPriceStepCount.AssertEqual(10);
	}

	[TestMethod]
	public void TradeGenerator_Process_RequiresSecurityMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();

		var time = DateTime.UtcNow;
		var timeMsg = new TimeMessage { ServerTime = time };

		var result = generator.Process(timeMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void TradeGenerator_Process_GeneratesTradeAfterSecurityMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result = generator.Process(l1Msg);
		result.AssertNotNull();

		var trade = (ExecutionMessage)result;
		trade.DataTypeEx.AssertEqual(DataType.Ticks);
		trade.SecurityId.AssertEqual(secId);
		trade.TradeId.AssertNotNull();
		trade.TradePrice.AssertNotNull();
		(trade.TradePrice > 0).AssertTrue();
		trade.TradeVolume.AssertNotNull();
		(trade.TradeVolume > 0).AssertTrue();
	}

	[TestMethod]
	public void TradeGenerator_Process_RespectsInterval()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.FromSeconds(10);

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result1 = generator.Process(l1Msg);
		result1.AssertNotNull();

		var result2 = generator.Process(l1Msg);
		result2.AssertNull();

		var l1Msg2 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time.AddSeconds(11),
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result3 = generator.Process(l1Msg2);
		result3.AssertNotNull();
	}

	[TestMethod]
	public void TradeGenerator_Process_UsesExecutionMessagePrice()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId, 0.01m);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 150m,
		};

		var result = generator.Process(tickMsg);
		result.AssertNotNull();

		var trade = (ExecutionMessage)result;
		(trade.TradePrice >= 140m && trade.TradePrice <= 160m).AssertTrue();
	}

	[TestMethod]
	public void TradeGenerator_GenerateOriginSide_GeneratesSide()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.GenerateOriginSide = true;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var hasBuy = false;
		var hasSell = false;

		for (var i = 0; i < 100; i++)
		{
			generator.Init();
			generator.Interval = TimeSpan.Zero;
			generator.GenerateOriginSide = true;
			generator.Process(secMsg);

			var result = generator.Process(l1Msg);
			result.AssertNotNull();

			var trade = (ExecutionMessage)result;
			trade.OriginSide.AssertNotNull();

			if (trade.OriginSide == Sides.Buy)
				hasBuy = true;
			else if (trade.OriginSide == Sides.Sell)
				hasSell = true;

			if (hasBuy && hasSell)
				break;
		}

		hasBuy.AssertTrue();
		hasSell.AssertTrue();
	}

	[TestMethod]
	public void TradeGenerator_Price_NeverGoesNegative()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MaxPriceStepCount = 100;

		var secMsg = CreateSecurityMessage(secId, 1m);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 10m);

		for (var i = 0; i < 1000; i++)
		{
			l1Msg = new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = time.AddMilliseconds(i),
			};

			var result = generator.Process(l1Msg);
			if (result != null)
			{
				var trade = (ExecutionMessage)result;
				(trade.TradePrice > 0).AssertTrue();
			}
		}
	}

	[TestMethod]
	public void TradeGenerator_Clone_CreatesIndependentCopy()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.GenerateOriginSide = true;
		generator.MinVolume = 5;
		generator.MaxVolume = 50;
		generator.Interval = TimeSpan.FromMinutes(5);

		var clone = (RandomWalkTradeGenerator)generator.Clone();

		clone.SecurityId.AssertEqual(secId);
		clone.GenerateOriginSide.AssertEqual(true);
		clone.MinVolume.AssertEqual(5);
		clone.MaxVolume.AssertEqual(50);
		clone.Interval.AssertEqual(TimeSpan.FromMinutes(5));
	}

	[TestMethod]
	public void TradeGenerator_Clone_HasIndependentIdGenerator()
	{
		// Clone should have independent IdGenerator to avoid duplicate IDs
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var clone = (RandomWalkTradeGenerator)generator.Clone();
		clone.Init();

		generator.IdGenerator.AssertNotSame(clone.IdGenerator);
	}

	[TestMethod]
	public void TradeGenerator_VolumeStep_AppliedToTradeVolume()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MinVolume = 1;
		generator.MaxVolume = 10;

		var volumeStep = 0.5m;
		var secMsg = CreateSecurityMessage(secId, volumeStep: volumeStep);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result = generator.Process(l1Msg);
		result.AssertNotNull();

		var trade = (ExecutionMessage)result;
		(trade.TradeVolume % volumeStep == 0).AssertTrue();
	}

	[TestMethod]
	public void TradeGenerator_Process_HandlesTimeMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);
		generator.Process(l1Msg);

		var timeMsg = new TimeMessage
		{
			ServerTime = DateTime.UtcNow.AddSeconds(1),
		};

		var result = generator.Process(timeMsg);
		result.AssertNotNull();
	}

	[TestMethod]
	public void TradeGenerator_Process_ReturnNullForBoardMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);
		generator.Process(l1Msg);

		var boardMsg = CreateBoardMessage();
		var result = generator.Process(boardMsg);
		result.AssertNull();
	}

	#endregion

	#region TrendMarketDepthGenerator Tests

	[TestMethod]
	public void DepthGenerator_Init_SetsDefaultValues()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);

		generator.Init();

		generator.DataType.AssertEqual(DataType.MarketDepth);
		generator.SecurityId.AssertEqual(secId);
		generator.MinSpreadStepCount.AssertEqual(1);
		generator.MaxSpreadStepCount.AssertEqual(int.MaxValue);
		generator.MaxBidsDepth.AssertEqual(10);
		generator.MaxAsksDepth.AssertEqual(10);
		generator.MaxGenerations.AssertEqual(20);
		generator.UseTradeVolume.AssertEqual(true);
	}

	[TestMethod]
	public void DepthGenerator_Process_RequiresBoardMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result = generator.Process(l1Msg);
		result.AssertNull();
	}

	[TestMethod]
	public void DepthGenerator_Process_GeneratesDepthAfterTradeData()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};

		var result = generator.Process(tickMsg);
		result.AssertNotNull();

		var depth = (QuoteChangeMessage)result;
		depth.SecurityId.AssertEqual(secId);
		(depth.Bids.Length > 0).AssertTrue();
		(depth.Asks.Length > 0).AssertTrue();
	}

	[TestMethod]
	public void DepthGenerator_MaxGenerations_LimitsGenerationWithoutNewTrades()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MaxGenerations = 3;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};
		generator.Process(tickMsg);

		var generationCount = 0;
		for (var i = 0; i < 10; i++)
		{
			var timeMsg = new TimeMessage
			{
				ServerTime = time.AddSeconds(i + 1),
			};

			var result = generator.Process(timeMsg);
			if (result != null)
				generationCount++;
		}

		generationCount.AssertEqual(3);
	}

	[TestMethod]
	public void DepthGenerator_GenerateDepthOnEachTrade_GeneratesOnTrade()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.FromHours(1);
		generator.GenerateDepthOnEachTrade = true;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;

		var tickMsg1 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};
		var result1 = generator.Process(tickMsg1);
		result1.AssertNotNull();

		var tickMsg2 = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time.AddMilliseconds(100),
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
		};
		var result2 = generator.Process(tickMsg2);
		result2.AssertNotNull();
	}

	[TestMethod]
	public void DepthGenerator_Clone_CreatesIndependentCopy()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.MinSpreadStepCount = 3;
		generator.MaxSpreadStepCount = 10;
		generator.MaxBidsDepth = 15;
		generator.MaxAsksDepth = 20;
		generator.MaxGenerations = 50;
		generator.UseTradeVolume = false;

		var clone = (TrendMarketDepthGenerator)generator.Clone();

		clone.SecurityId.AssertEqual(secId);
		clone.MinSpreadStepCount.AssertEqual(3);
		clone.MaxSpreadStepCount.AssertEqual(10);
		clone.MaxBidsDepth.AssertEqual(15);
		clone.MaxAsksDepth.AssertEqual(20);
		clone.MaxGenerations.AssertEqual(50);
		clone.UseTradeVolume.AssertEqual(false);
	}

	[TestMethod]
	public void DepthGenerator_Clone_CopiesGenerateDepthOnEachTrade()
	{
		// Clone should copy GenerateDepthOnEachTrade
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.GenerateDepthOnEachTrade = true;

		var clone = (TrendMarketDepthGenerator)generator.Clone();

		clone.GenerateDepthOnEachTrade.AssertEqual(true);
	}

	[TestMethod]
	public void DepthGenerator_Clone_CopiesGenerateOrdersCount()
	{
		// Clone should copy GenerateOrdersCount
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.GenerateOrdersCount = true;

		var clone = (TrendMarketDepthGenerator)generator.Clone();

		clone.GenerateOrdersCount.AssertEqual(true);
	}

	[TestMethod]
	public void DepthGenerator_OriginSide_AffectsBestQuotes()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxPriceStepCount = 1;
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;

		var firstTick = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};

		var firstDepth = (QuoteChangeMessage)generator.Process(firstTick);
		firstDepth.AssertNotNull();

		var secondTick = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time.AddSeconds(1),
			DataTypeEx = DataType.Ticks,
			TradePrice = 101m,
			OriginSide = Sides.Buy,
		};

		var secondDepth = (QuoteChangeMessage)generator.Process(secondTick);
		secondDepth.AssertNotNull();

		secondDepth.Asks[0].Price.AssertGreater(firstDepth.Asks[0].Price);
		secondDepth.Bids[0].Price.AssertEqual(firstDepth.Bids[0].Price);
	}

	[TestMethod]
	public void DepthGenerator_Price_NeverGoesNegative()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MaxPriceStepCount = 50;

		var secMsg = CreateSecurityMessage(secId, 1m);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 10m,
		};
		generator.Process(tickMsg);

		for (var i = 0; i < 100; i++)
		{
			var timeMsg = new TimeMessage
			{
				ServerTime = time.AddSeconds(i + 1),
			};

			var result = generator.Process(timeMsg);
			if (result is QuoteChangeMessage depth)
			{
				foreach (var bid in depth.Bids)
					(bid.Price > 0).AssertTrue();

				foreach (var ask in depth.Asks)
					(ask.Price > 0).AssertTrue();
			}
		}
	}

	[TestMethod]
	public void DepthGenerator_GenerateOrdersCount_GeneratesOrdersCount()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.GenerateOrdersCount = true;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};

		var hasOrdersCount = false;
		for (var i = 0; i < 50; i++)
		{
			generator.Init();
			generator.Interval = TimeSpan.Zero;
			generator.GenerateOrdersCount = true;
			generator.Process(secMsg);
			generator.Process(boardMsg);

			var result = generator.Process(tickMsg);
			if (result is QuoteChangeMessage depth)
			{
				foreach (var quote in depth.Bids.Concat(depth.Asks))
				{
					if (quote.OrdersCount != null)
					{
						hasOrdersCount = true;
						break;
					}
				}
			}

			if (hasOrdersCount)
				break;
		}

		hasOrdersCount.AssertTrue();
	}

	#endregion

	#region OrderLogGenerator Tests

	[TestMethod]
	public void OrderLogGenerator_Init_SetsDefaultValues()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new OrderLogGenerator(secId);

		generator.Init();

		generator.DataType.AssertEqual(DataType.OrderLog);
		generator.SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public void OrderLogGenerator_Process_GeneratesNewOrders()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new OrderLogGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result = generator.Process(l1Msg);
		result.AssertNotNull();

		var orderLog = (ExecutionMessage)result;
		orderLog.DataTypeEx.AssertEqual(DataType.OrderLog);
		orderLog.SecurityId.AssertEqual(secId);
		orderLog.OrderId.AssertNotNull();
		orderLog.OrderPrice.AssertNotNull();
		(orderLog.OrderPrice > 0).AssertTrue();
		orderLog.OrderVolume.AssertNotNull();
		(orderLog.OrderVolume > 0).AssertTrue();
		orderLog.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void OrderLogGenerator_Process_ShouldGenerateMatchedTrades()
	{
		// OrderLogGenerator should generate trades for existing orders.
		// With enough iterations, entries with TradeId should appear.
		var secId = Helper.CreateSecurityId();
		var generator = new OrderLogGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.TradeGenerator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;

		var hasTradeInfo = false;
		for (var i = 0; i < 1000; i++)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = time.AddMilliseconds(i),
			}.TryAdd(Level1Fields.LastTradePrice, 100m);

			var result = generator.Process(msg);
			if (result is ExecutionMessage exec && exec.TradeId != null)
			{
				hasTradeInfo = true;
				break;
			}
		}

		// Expecting trades to be generated
		hasTradeInfo.AssertTrue();
	}

	// A source whose every draw is chosen by the test, so a generated fill is a decision and not a
	// coin toss. An empty range is refused the way the real sources refuse it.
	private class ScriptedRandomProvider(Func<int, int, int> next) : IRandomProvider
	{
		int IRandomProvider.Next(int min, int max)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException(nameof(min), min, $"Nothing can be drawn from the empty range [{min}, {max}].");

			return next(min, max);
		}

		long IRandomProvider.NextLong(long min, long max) => min;
		double IRandomProvider.NextDouble() => 0d;
		void IRandomProvider.NextBytes(byte[] buffer) => throw new NotSupportedException();
	}

	// The draws OrderLogGenerator makes, told apart by the range each one asks for: (0, 5) picks the
	// action and 5 is the one that matches a resting order, (1, N) sizes the fill. Side and price
	// step are held still so only volume varies.
	private static OrderLogGenerator CreateScriptedOrderLogGenerator(SecurityId secId, decimal volumeStep, int units, Func<int, int> fill)
	{
		var tradeGenerator = new RandomWalkTradeGenerator(secId)
		{
			Interval = TimeSpan.Zero,
			MaxPriceStepCount = 1,
			RandomProvider = new ScriptedRandomProvider((min, max) => min),
		};

		var generator = new OrderLogGenerator(secId, tradeGenerator)
		{
			Interval = TimeSpan.Zero,
			MaxVolume = units,
			MinVolume = units,
			MaxPriceStepCount = 1,
			RandomProvider = new ScriptedRandomProvider((min, max) =>
			{
				if (min == 1)
					return fill(max);

				return min == 0 && max == 5 ? 5 : 0;
			}),
		};

		generator.Init();
		generator.Process(CreateSecurityMessage(secId, volumeStep: volumeStep));

		return generator;
	}

	[TestMethod]
	public void OrderLogGenerator_MatchedTrade_FillsNeverExceedTheBalance()
	{
		// 25 volume steps of 0.1 make an order of 2.5. Each fill is a slice of what is left: above
		// zero, no larger than the balance, and a whole number of steps. Taking as much as allowed
		// each time walks the balance 2.5 -> 0.5, so the last fill owes exactly the 0.5 left, and
		// the fills add up to 2.5 when the order goes Done.
		const decimal volumeStep = 0.1m;
		const decimal orderVolume = 2.5m;

		var secId = Helper.CreateSecurityId();
		var generator = CreateScriptedOrderLogGenerator(secId, volumeStep, 25, max => max);

		var time = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc);

		var balance = orderVolume;
		var filled = 0m;
		var done = false;

		for (var i = 0; i < 10 && !done; i++)
		{
			if (generator.Process(new TimeMessage { ServerTime = time.AddSeconds(i) }) is not ExecutionMessage exec || exec.TradeId is null)
				continue;

			var fill = exec.TradeVolume.Value;

			IsGreater(fill, 0m, "a fill of nothing is not a trade");
			IsLessOrEqual(fill, balance, $"filled {fill} while only {balance} was left");
			AreEqual(0m, fill % volumeStep, $"{fill} is not a whole number of {volumeStep} steps");

			balance -= fill;
			filled += fill;

			done = exec.OrderState == OrderStates.Done;

			if (!done)
				AreEqual(OrderStates.Active, exec.OrderState, "an order with volume left stays active");
		}

		IsTrue(done, "the order has to be filled out and closed");
		AreEqual(orderVolume, filled, "the fills add up to the order volume");
		AreEqual(0m, balance, "nothing is left of the order");
	}

	[TestMethod]
	public void OrderLogGenerator_MatchedTrade_FillIsWholeVolumeSteps()
	{
		// 7 volume steps of 0.3 make an order of 2.1. What trades is a whole number of steps -
		// 0.3, 0.6 ... 2.1 - because nothing smaller than a step is tradable, and neither is
		// anything between two steps.
		const decimal volumeStep = 0.3m;
		const decimal orderVolume = 2.1m;

		var secId = Helper.CreateSecurityId();
		var generator = CreateScriptedOrderLogGenerator(secId, volumeStep, 7, max => max);

		var time = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc);

		decimal? fill = null;

		for (var i = 0; i < 10 && fill is null; i++)
		{
			if (generator.Process(new TimeMessage { ServerTime = time.AddSeconds(i) }) is ExecutionMessage exec && exec.TradeId != null)
				fill = exec.TradeVolume.Value;
		}

		IsNotNull(fill, "a matched order log entry has to appear");
		IsLessOrEqual(fill.Value, orderVolume, $"filled {fill} of an order of {orderVolume}");
		AreEqual(0m, fill.Value % volumeStep, $"{fill} is not a whole number of {volumeStep} steps");
	}

	[TestMethod]
	public void OrderLogGenerator_MatchedTrade_FillsAddUpToTheOrder()
	{
		// 3 volume steps of 1 make an order of 3, taken one step at a time: the balance walks
		// 3 -> 2 -> 1 -> 0, so three fills of 1 arrive, the first two leaving the order active and
		// the third closing it.
		const decimal volumeStep = 1m;
		const decimal orderVolume = 3m;

		var secId = Helper.CreateSecurityId();
		var generator = CreateScriptedOrderLogGenerator(secId, volumeStep, 3, max => 1);

		var time = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc);

		var fills = new List<ExecutionMessage>();

		for (var i = 0; i < 10 && !fills.Any(f => f.OrderState == OrderStates.Done); i++)
		{
			if (generator.Process(new TimeMessage { ServerTime = time.AddSeconds(i) }) is ExecutionMessage exec && exec.TradeId != null)
				fills.Add(exec);
		}

		HasCount(3, fills, "one step per fill closes an order of three steps in three fills");

		AreEqual(1m, fills[0].TradeVolume.Value);
		AreEqual(1m, fills[1].TradeVolume.Value);
		AreEqual(1m, fills[2].TradeVolume.Value);

		AreEqual(OrderStates.Active, fills[0].OrderState);
		AreEqual(OrderStates.Active, fills[1].OrderState);
		AreEqual(OrderStates.Done, fills[2].OrderState, "the fill that takes the last step closes the order");

		AreEqual(orderVolume, fills.Sum(f => f.TradeVolume.Value), "the fills add up to the order volume");
	}

	[TestMethod]
	public void OrderLogGenerator_Clone_CreatesIndependentCopy()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new OrderLogGenerator(secId);
		generator.Init();
		generator.MinVolume = 10;
		generator.MaxVolume = 100;

		var clone = (OrderLogGenerator)generator.Clone();

		clone.SecurityId.AssertEqual(secId);
		clone.MinVolume.AssertEqual(10);
		clone.MaxVolume.AssertEqual(100);
	}

	[TestMethod]
	public void OrderLogGenerator_Process_HandlesExecutionMessage()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new OrderLogGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 150m,
		};

		var result = generator.Process(tickMsg);
		result.AssertNotNull();

		var orderLog = (ExecutionMessage)result;
		(orderLog.OrderPrice >= 140m && orderLog.OrderPrice <= 160m).AssertTrue();
	}

	#endregion

	#region MarketDataGenerator Validation Tests

	[TestMethod]
	public void Generator_MinVolume_ThrowsOnZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MinVolume = 0);
	}

	[TestMethod]
	public void Generator_MaxVolume_ThrowsOnZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxVolume = 0);
	}

	[TestMethod]
	public void Generator_MaxPriceStepCount_ThrowsOnZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxPriceStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MinSpreadStepCount_ThrowsOnZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MinSpreadStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MaxSpreadStepCount_ThrowsOnZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxSpreadStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MaxBidsDepth_AcceptsZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxBidsDepth = 0;
		generator.MaxBidsDepth.AssertEqual(0);
	}

	[TestMethod]
	public void DepthGenerator_MaxAsksDepth_AcceptsZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxAsksDepth = 0;
		generator.MaxAsksDepth.AssertEqual(0);
	}

	[TestMethod]
	public void Generator_MinVolumeGreaterThanMaxVolume_ThrowsOnInit()
	{
		// MinVolume > MaxVolume throws ArgumentException during Init() when RandomArray is created
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.MaxVolume = 5;
		generator.MinVolume = 10; // min > max
		ThrowsExactly<ArgumentException>(() => generator.Init());
	}

	[TestMethod]
	public void DepthGenerator_MinSpreadGreaterThanMaxSpread_ThrowsOnInit()
	{
		// MinSpreadStepCount > MaxSpreadStepCount throws ArgumentException during Init()
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxSpreadStepCount = 2;
		generator.MinSpreadStepCount = 5; // min > max
		ThrowsExactly<ArgumentException>(() => generator.Init());
	}

	[TestMethod]
	public void Generator_Volumes_ThrowsIfNotInitialized()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<InvalidOperationException>(() => { var _ = generator.Volumes; });
	}

	[TestMethod]
	public void Generator_Steps_ThrowsIfNotInitialized()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<InvalidOperationException>(() => { var _ = generator.Steps; });
	}

	[TestMethod]
	public void Generator_Process_ThrowsOnNull()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		ThrowsExactly<ArgumentNullException>(() => generator.Process(null));
	}

	#endregion

	#region GeneratorMessage Tests

	[TestMethod]
	public void GeneratorMessage_Clone_ClonesGenerator()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.MinVolume = 5;
		generator.MaxVolume = 50;

		var msg = new GeneratorMessage
		{
			Generator = generator,
			IsSubscribe = true,
			TransactionId = 123,
			SecurityId = secId,
		};

		var clone = (GeneratorMessage)msg.Clone();

		clone.IsSubscribe.AssertEqual(true);
		clone.TransactionId.AssertEqual(123);
		clone.SecurityId.AssertEqual(secId);
		clone.Generator.AssertNotNull();
		clone.Generator.AssertNotSame(generator);

		var clonedGenerator = (RandomWalkTradeGenerator)clone.Generator;
		clonedGenerator.MinVolume.AssertEqual(5);
		clonedGenerator.MaxVolume.AssertEqual(50);
	}

	[TestMethod]
	public void GeneratorMessage_Clone_HandlesNullGenerator()
	{
		var secId = Helper.CreateSecurityId();
		var msg = new GeneratorMessage
		{
			Generator = null,
			IsSubscribe = true,
			TransactionId = 123,
			SecurityId = secId,
		};

		var clone = (GeneratorMessage)msg.Clone();

		clone.Generator.AssertNull();
	}

	#endregion

	#region Edge Cases and Stress Tests

	[TestMethod]
	public void TradeGenerator_LargeNumberOfTrades_NoMemoryLeak()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);
		generator.Process(l1Msg);

		// Weighing the process would count what the tests running beside this one allocate. What
		// survives a collection is about these objects only.
		var produced = new List<WeakReference>();
		var consumed = new List<WeakReference>();

		for (var i = 0; i < 10000; i++)
		{
			var timeMsg = new TimeMessage
			{
				ServerTime = DateTime.UtcNow.AddMilliseconds(i),
			};

			var result = generator.Process(timeMsg);

			// Retention would hold all of them, so a sample is enough.
			if (i % 1000 == 0)
			{
				consumed.Add(new(timeMsg));

				if (result is not null)
					produced.Add(new(result));
			}
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		consumed.Any(r => r.IsAlive).AssertFalse("the generator is holding on to messages it was given");
		produced.Any(r => r.IsAlive).AssertFalse("the generator is holding on to messages it produced");
	}

	[TestMethod]
	public void TradeGenerator_PriceStability_AfterManyIterations()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MaxPriceStepCount = 5;

		var secMsg = CreateSecurityMessage(secId, 0.01m);
		generator.Process(secMsg);

		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 100m);
		generator.Process(l1Msg);

		var prices = new List<decimal>();

		for (var i = 0; i < 1000; i++)
		{
			var timeMsg = new TimeMessage
			{
				ServerTime = DateTime.UtcNow.AddMilliseconds(i),
			};

			var result = generator.Process(timeMsg);
			if (result is ExecutionMessage trade)
				prices.Add(trade.TradePrice.Value);
		}

		prices.All(p => p > 0).AssertTrue();

		var distinctPrices = prices.Distinct().Count();
		(distinctPrices > 10).AssertTrue();
	}

	[TestMethod]
	public void DepthGenerator_EmptyDepth_WhenDepthZero()
	{
		var secId = Helper.CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.Interval = TimeSpan.Zero;
		generator.MaxBidsDepth = 0;
		generator.MaxAsksDepth = 0;

		var secMsg = CreateSecurityMessage(secId);
		generator.Process(secMsg);

		var boardMsg = CreateBoardMessage();
		generator.Process(boardMsg);

		var time = DateTime.UtcNow;
		var tickMsg = new ExecutionMessage
		{
			SecurityId = secId,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = 100m,
		};

		var result = generator.Process(tickMsg);
		result.AssertNotNull();

		var depth = (QuoteChangeMessage)result;
		depth.Bids.Length.AssertEqual(0);
		depth.Asks.Length.AssertEqual(0);
	}

	[TestMethod]
	public void TradeGenerator_MultipleSecurities_Independent()
	{
		var secId1 = Helper.CreateSecurityId();
		var secId2 = Helper.CreateSecurityId();

		var generator1 = new RandomWalkTradeGenerator(secId1);
		var generator2 = new RandomWalkTradeGenerator(secId2);

		generator1.Init();
		generator2.Init();
		generator1.Interval = TimeSpan.Zero;
		generator2.Interval = TimeSpan.Zero;

		generator1.Process(CreateSecurityMessage(secId1));
		generator2.Process(CreateSecurityMessage(secId2));

		var time = DateTime.UtcNow;

		generator1.Process(new Level1ChangeMessage
		{
			SecurityId = secId1,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 100m));

		generator2.Process(new Level1ChangeMessage
		{
			SecurityId = secId2,
			ServerTime = time,
		}.TryAdd(Level1Fields.LastTradePrice, 200m));

		var result1 = generator1.Process(new TimeMessage { ServerTime = time.AddSeconds(1) });
		var result2 = generator2.Process(new TimeMessage { ServerTime = time.AddSeconds(1) });

		result1.AssertNotNull();
		result2.AssertNotNull();

		var trade1 = (ExecutionMessage)result1;
		var trade2 = (ExecutionMessage)result2;

		trade1.SecurityId.AssertEqual(secId1);
		trade2.SecurityId.AssertEqual(secId2);

		trade1.TradePrice.AssertNotEqual(trade2.TradePrice);
	}

	#endregion
}
