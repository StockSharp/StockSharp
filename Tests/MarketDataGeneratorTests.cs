namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;

[TestClass]
public class MarketDataGeneratorTests : BaseTestClass
{
	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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

		(generationCount <= 3).AssertTrue();
	}

	[TestMethod]
	public void DepthGenerator_GenerateDepthOnEachTrade_GeneratesOnTrade()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();
		generator.GenerateOrdersCount = true;

		var clone = (TrendMarketDepthGenerator)generator.Clone();

		clone.GenerateOrdersCount.AssertEqual(true);
	}

	[TestMethod]
	public void DepthGenerator_OriginSide_AffectsBestQuotes()
	{
		var secId = CreateSecurityId();
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
			OriginSide = Sides.Buy,
		};

		var result = generator.Process(tickMsg);
		result.AssertNotNull();

		var depth = (QuoteChangeMessage)result;
		(depth.Asks.Length > 0).AssertTrue();
	}

	[TestMethod]
	public void DepthGenerator_Price_NeverGoesNegative()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
		var generator = new OrderLogGenerator(secId);

		generator.Init();

		generator.DataType.AssertEqual(DataType.OrderLog);
		generator.SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public void OrderLogGenerator_Process_GeneratesNewOrders()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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

	[TestMethod]
	public void OrderLogGenerator_Clone_CreatesIndependentCopy()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MinVolume = 0);
	}

	[TestMethod]
	public void Generator_MaxVolume_ThrowsOnZero()
	{
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxVolume = 0);
	}

	[TestMethod]
	public void Generator_MaxPriceStepCount_ThrowsOnZero()
	{
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxPriceStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MinSpreadStepCount_ThrowsOnZero()
	{
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MinSpreadStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MaxSpreadStepCount_ThrowsOnZero()
	{
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		ThrowsExactly<ArgumentOutOfRangeException>(() => generator.MaxSpreadStepCount = 0);
	}

	[TestMethod]
	public void DepthGenerator_MaxBidsDepth_AcceptsZero()
	{
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxBidsDepth = 0;
		generator.MaxBidsDepth.AssertEqual(0);
	}

	[TestMethod]
	public void DepthGenerator_MaxAsksDepth_AcceptsZero()
	{
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxAsksDepth = 0;
		generator.MaxAsksDepth.AssertEqual(0);
	}

	[TestMethod]
	public void Generator_MinVolumeGreaterThanMaxVolume_ThrowsOnInit()
	{
		// MinVolume > MaxVolume throws ArgumentException during Init() when RandomArray is created
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.MaxVolume = 5;
		generator.MinVolume = 10; // min > max
		ThrowsExactly<ArgumentException>(() => generator.Init());
	}

	[TestMethod]
	public void DepthGenerator_MinSpreadGreaterThanMaxSpread_ThrowsOnInit()
	{
		// MinSpreadStepCount > MaxSpreadStepCount throws ArgumentException during Init()
		var secId = CreateSecurityId();
		var generator = new TrendMarketDepthGenerator(secId);
		generator.MaxSpreadStepCount = 2;
		generator.MinSpreadStepCount = 5; // min > max
		ThrowsExactly<ArgumentException>(() => generator.Init());
	}

	[TestMethod]
	public void Generator_Volumes_ThrowsIfNotInitialized()
	{
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<InvalidOperationException>(() => { var _ = generator.Volumes; });
	}

	[TestMethod]
	public void Generator_Steps_ThrowsIfNotInitialized()
	{
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		ThrowsExactly<InvalidOperationException>(() => { var _ = generator.Steps; });
	}

	[TestMethod]
	public void Generator_Process_ThrowsOnNull()
	{
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);
		generator.Init();
		ThrowsExactly<ArgumentNullException>(() => generator.Process(null));
	}

	#endregion

	#region GeneratorMessage Tests

	[TestMethod]
	public void GeneratorMessage_Clone_ClonesGenerator()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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

		var startMemory = GC.GetTotalMemory(true);

		for (var i = 0; i < 10000; i++)
		{
			var timeMsg = new TimeMessage
			{
				ServerTime = DateTime.UtcNow.AddMilliseconds(i),
			};
			generator.Process(timeMsg);
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var endMemory = GC.GetTotalMemory(true);
		var memoryGrowth = endMemory - startMemory;

		(memoryGrowth < 10 * 1024 * 1024).AssertTrue();
	}

	[TestMethod]
	public void TradeGenerator_PriceStability_AfterManyIterations()
	{
		var secId = CreateSecurityId();
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
		var secId = CreateSecurityId();
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
		var secId1 = CreateSecurityId();
		var secId2 = CreateSecurityId();

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
