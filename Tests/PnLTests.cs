namespace StockSharp.Tests;

using StockSharp.Algo.PnL;

[TestClass]
public class PnLTests
{
	[TestMethod]
	public void QueueRealizedUnrealized()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var queue = new PnLQueue(secId);

		queue.UpdateSecurity(new Level1ChangeMessage
		{
			SecurityId = secId
		}
		.Add(Level1Fields.PriceStep, 1m)
		.Add(Level1Fields.StepPrice, 1m));

		var buy = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 10m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};

		queue.Process(buy).PnL.AssertEqual(0m);
		queue.RealizedPnL.AssertEqual(0m);

		var sell = buy.TypedClone();
		sell.TradeId = 2;
		sell.Side = Sides.Sell;
		sell.TradePrice = 12m;
		queue.Process(sell).PnL.AssertEqual(2m);
		queue.RealizedPnL.AssertEqual(2m);

		queue.ProcessExecution(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 11m
		});

		queue.UnrealizedPnL.AssertEqual(0m);
	}

	[TestMethod]
	public void PortfolioManagerRealized()
	{
		var secId = Helper.CreateSecurityId();

		var manager = new PortfolioPnLManager("pf", id => new Level1ChangeMessage
		{
			SecurityId = id
		}
		.Add(Level1Fields.PriceStep, 1m)
		.Add(Level1Fields.StepPrice, 1m));

		var buy = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 10m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};

		manager.ProcessMyTrade(buy, out var info1).AssertTrue();
		info1.PnL.AssertEqual(0m);
		manager.RealizedPnL.AssertEqual(0m);

		var sell = buy.TypedClone();
		sell.TradeId = 2;
		sell.Side = Sides.Sell;
		sell.TradePrice = 15m;
		manager.ProcessMyTrade(sell, out var info2).AssertTrue();
		info2.PnL.AssertEqual(5m);

		manager.RealizedPnL.AssertEqual(5m);
		manager.UnrealizedPnL.AssertEqual(0m);
		manager.GetPnL().AssertEqual(5m);
	}

	[TestMethod]
	public void BasicBuySell()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();

		// Register order (portfolio binding)
		var regMsg = new OrderRegisterMessage
		{
			PortfolioName = "TestPortfolio",
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(regMsg);

		// Buy 10 at 100
		var buyMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regMsg.SecurityId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 100,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		};
		manager.ProcessMessage(buyMsg);

		// After buy: position open, no realized PnL yet
		manager.RealizedPnL.AssertEqual(0);

		// Sell 10 at 110
		var sellMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regMsg.SecurityId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 110,
			TradeId = 2,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		};
		manager.ProcessMessage(sellMsg);

		// Realized PnL = (110-100)*10 = 100
		manager.RealizedPnL.AssertEqual(100);
		manager.UnrealizedPnL.AssertEqual(0);
	}

	/// <summary>
	/// The same order registration can reach the manager more than once - the pipeline resends what it has
	/// already sent. It names an order the manager is already tracking, so it must be absorbed: it is handled
	/// on the way out to the exchange, where an exception turns into a failed order, and nothing about a
	/// user's profit and loss may depend on how many times a message was delivered.
	/// </summary>
	[TestMethod]
	public void OrderRegistrationDeliveredTwiceLeavesPnLIntact()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();

		var regMsg = new OrderRegisterMessage
		{
			PortfolioName = "TestPortfolio",
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
		};

		manager.ProcessMessage(regMsg);
		manager.ProcessMessage(regMsg.TypedClone());

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = regMsg.TransactionId,
			SecurityId = secId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 100,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = regMsg.TransactionId,
			SecurityId = secId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 110,
			TradeId = 2,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		// The round trip made ten points on ten units, exactly as it would have without the repeat.
		manager.RealizedPnL.AssertEqual(100m);
	}

	/// <summary>
	/// Amending an order gives it a new transaction id, and from that moment its fills arrive under the new
	/// id. It is the same order, the same position and the same money, so those fills must keep reaching the
	/// portfolio: an amend that detaches an order from its profit and loss leaves a hole in the reported
	/// result that grows every time the user moves a price.
	/// </summary>
	[TestMethod]
	public void FillsOfAReplacedOrderStillCountTowardsThePortfolio()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();
		var pfName = Helper.CreatePortfolio().Name;

		var regMsg = new OrderRegisterMessage
		{
			PortfolioName = pfName,
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
		};

		manager.ProcessMessage(regMsg);

		// The order is amended: same portfolio, same instrument, new transaction id.
		manager.ProcessMessage(new OrderReplaceMessage
		{
			PortfolioName = pfName,
			SecurityId = secId,
			TransactionId = 2,
			OriginalTransactionId = regMsg.TransactionId,
			OldOrderId = 100,
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
		});

		// The fill comes back as a trade alone, the way an adapter reports one, naming the new transaction.
		var fill = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 2,
			SecurityId = secId,
			PortfolioName = pfName,
			Side = Sides.Buy,
			TradePrice = 101,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		fill.AssertNotNull("the fill of the amended order must be accounted");

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 111m,
			ServerTime = DateTime.UtcNow,
		});

		// Ten units bought at 101 and last traded at 111 are ten points up.
		manager.UnrealizedPnL.AssertEqual(100m);
	}

	[TestMethod]
	public void PartialCloseAndUnrealized()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();

		var regMsg = new OrderRegisterMessage
		{
			PortfolioName = "TestPortfolio",
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(regMsg);

		// Buy 10 at 100
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regMsg.SecurityId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 100,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		// Sell 4 at 110 (partial close)
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regMsg.SecurityId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 110,
			TradeId = 2,
			TradeVolume = 4,
			ServerTime = DateTime.UtcNow,
		});

		// Realized PnL = (110-100)*4 = 40
		manager.RealizedPnL.AssertEqual(40);
		manager.UnrealizedPnL.AssertEqual(0);

		// Update last market price for remaining position (unrealized PnL)
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 120m,
			ServerTime = DateTime.UtcNow,
		});

		// Unrealized PnL = (120-100)*6 = 120
		// Total PnL = 40 + 120 = 160
		manager.UnrealizedPnL.AssertEqual(120);
		manager.RealizedPnL.AssertEqual(40);
		manager.GetPnL().AssertEqual(160);
	}

	[TestMethod]
	public void Reset()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();

		// Simulate activity
		var regMsg = new OrderRegisterMessage
		{
			PortfolioName = "TestPortfolio",
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(regMsg);

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regMsg.SecurityId,
			PortfolioName = regMsg.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 100,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		// Reset PnL
		manager.ProcessMessage(new ResetMessage());

		manager.GetPnL().AssertEqual(0);
		manager.RealizedPnL.AssertEqual(0);
		manager.UnrealizedPnL.AssertEqual(0);
	}

	[TestMethod]
	public void MultiPortfolio()
	{
		IPnLManager manager = new PnLManager();

		var secId = Helper.CreateSecurityId();

		// Portfolio A
		var regA = new OrderRegisterMessage
		{
			PortfolioName = "A",
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(regA);

		// Portfolio B
		var regB = new OrderRegisterMessage
		{
			PortfolioName = "B",
			SecurityId = secId,
			TransactionId = 2,
		};
		manager.ProcessMessage(regB);

		// Buy in A
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regA.SecurityId,
			PortfolioName = regA.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 100,
			TradeId = 1,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		// Buy in B
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 2,
			SecurityId = regB.SecurityId,
			PortfolioName = regB.PortfolioName,
			Side = Sides.Buy,
			TradePrice = 200,
			TradeId = 2,
			TradeVolume = 5,
			ServerTime = DateTime.UtcNow,
		});

		// Sell in A
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = regA.SecurityId,
			PortfolioName = regA.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 110,
			TradeId = 3,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		});

		// Sell in B
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 2,
			SecurityId = regB.SecurityId,
			PortfolioName = regB.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 194,
			TradeId = 4,
			TradeVolume = 5,
			ServerTime = DateTime.UtcNow,
		});

		// Portfolio A: (110-100)*10 = 100
		// Portfolio B: (194-200)*5 = -30
		// Net: 70
		manager.RealizedPnL.AssertEqual(70m);
		manager.GetPnL().AssertEqual(70m);
	}

	[TestMethod]
	public void UnrealizedPnL_ByDataType()
	{
		var secId = Helper.CreateSecurityId();

		IPnLManager manager = new PnLManager
		{
			UseTick = true,
			UseOrderLog = true,
			UseOrderBook = true,
			UseLevel1 = true,
			UseCandles = true
		};

		var reg = new OrderRegisterMessage
		{
			PortfolioName = Helper.CreatePortfolio().Name,
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(reg);

		var buy = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(buy);

		// --- Tick ---
		var tick = new ExecutionMessage { DataTypeEx = DataType.Ticks, SecurityId = secId, TradePrice = 110m };
		manager.ProcessMessage(tick);
		manager.UnrealizedPnL.AssertEqual(10m);

		// --- OrderLog ---
		var orderLog = new ExecutionMessage { DataTypeEx = DataType.OrderLog, SecurityId = secId, TradePrice = 120m };
		manager.ProcessMessage(orderLog);
		manager.UnrealizedPnL.AssertEqual(20m);

		// --- OrderBook ---
		var quote = new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [new(130m, 1)],
			Asks = [new(131m, 1)]
		};
		manager.ProcessMessage(quote);
		manager.UnrealizedPnL.AssertEqual(30m);

		// --- Level1 ---
		var l1 = new Level1ChangeMessage { SecurityId = secId }.Add(Level1Fields.LastTradePrice, 140m);
		manager.ProcessMessage(l1);
		manager.UnrealizedPnL.AssertEqual(40m);

		// --- Candle ---
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			OpenTime = DateTime.UtcNow,
			CloseTime = DateTime.UtcNow,
			OpenPrice = 100,
			HighPrice = 150,
			LowPrice = 90,
			ClosePrice = 150,
			TotalVolume = 1
		};
		manager.ProcessMessage(candle);
		manager.UnrealizedPnL.AssertEqual(50m);
	}

	[TestMethod]
	public void SaveLoad()
	{
		var manager = new PnLManager
		{
			UseCandles = true,
			UseLevel1 = false,
			UseOrderBook = true,
			UseOrderLog = false,
			UseTick = true,
		};

		var storage = manager.Save();

		var manager2 = new PnLManager();

		manager2.Load(storage);
		manager2.UseCandles.AssertTrue();
		manager2.UseLevel1.AssertFalse();
		manager2.UseOrderBook.AssertTrue();
		manager2.UseOrderLog.AssertFalse();
		manager2.UseTick.AssertTrue();
	}

	[TestMethod]
	public void StalePrices_QuoteThenCandle()
	{
		var secId = Helper.CreateSecurityId();

		IPnLManager manager = new PnLManager
		{
			UseOrderBook = true,
			UseCandles = true
		};

		var reg = new OrderRegisterMessage
		{
			PortfolioName = Helper.CreatePortfolio().Name,
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(reg);

		// Open long position: buy 1 at 100
		var buy = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(buy);

		// Market data: quote with bid=130, ask=131
		var quote = new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [new(130m, 1)],
			Asks = [new(131m, 1)]
		};
		manager.ProcessMessage(quote);
		manager.UnrealizedPnL.AssertEqual(30m); // (130-100)*1 = 30

		// Newer market data: candle closes at 150
		// A candle is newer than the quote and must replace its prices.
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			OpenTime = DateTime.UtcNow,
			CloseTime = DateTime.UtcNow,
			OpenPrice = 100,
			HighPrice = 150,
			LowPrice = 90,
			ClosePrice = 150,
			TotalVolume = 1
		};
		manager.ProcessMessage(candle);

		// Expected: (150-100)*1 = 50
		manager.UnrealizedPnL.AssertEqual(50m);
	}

	[TestMethod]
	public void StalePrices_CandleThenQuote()
	{
		var secId = Helper.CreateSecurityId();

		IPnLManager manager = new PnLManager
		{
			UseOrderBook = true,
			UseCandles = true
		};

		var reg = new OrderRegisterMessage
		{
			PortfolioName = Helper.CreatePortfolio().Name,
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(reg);

		// Open short position: sell 1 at 100
		var sell = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 1m,
			Side = Sides.Sell,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(sell);

		// Market data: candle closes at 90
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			OpenTime = DateTime.UtcNow,
			CloseTime = DateTime.UtcNow,
			OpenPrice = 100,
			HighPrice = 110,
			LowPrice = 85,
			ClosePrice = 90,
			TotalVolume = 1
		};
		manager.ProcessMessage(candle);
		manager.UnrealizedPnL.AssertEqual(10m); // (100-90)*1 = 10

		// Newer market data: quote with ask=80
		// The newer quote must replace the candle price.
		var quote = new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [new(79m, 1)],
			Asks = [new(80m, 1)]
		};
		manager.ProcessMessage(quote);

		// For short position, uses ask price for UnrealizedPnL
		// Expected: (100-80)*1 = 20
		manager.UnrealizedPnL.AssertEqual(20m);
	}

	[TestMethod]
	public void StalePrices_TickThenQuote()
	{
		var secId = Helper.CreateSecurityId();

		IPnLManager manager = new PnLManager
		{
			UseTick = true,
			UseOrderBook = true,
		};

		var reg = new OrderRegisterMessage
		{
			PortfolioName = Helper.CreatePortfolio().Name,
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(reg);

		// Open long position: buy 1 at 100
		var buy = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(buy);

		// Market data: tick at 140
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 140m,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(tick);
		manager.UnrealizedPnL.AssertEqual(40m); // (140-100)*1 = 40

		// Newer market data: quote with bid=130
		// The newer quote must replace the tick price.
		var quote = new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [new(130m, 1)],
			Asks = [new(131m, 1)]
		};
		manager.ProcessMessage(quote);

		// For long position, uses bid price (which is more accurate than last trade)
		// Expected: (130-100)*1 = 30
		manager.UnrealizedPnL.AssertEqual(30m);
	}

	[TestMethod]
	public void StalePrices_QuoteThenTick()
	{
		var secId = Helper.CreateSecurityId();

		IPnLManager manager = new PnLManager
		{
			UseTick = true,
			UseOrderBook = true,
		};

		var reg = new OrderRegisterMessage
		{
			PortfolioName = Helper.CreatePortfolio().Name,
			SecurityId = secId,
			TransactionId = 1,
		};
		manager.ProcessMessage(reg);

		// Open long position: buy 1 at 100
		var buy = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 1m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(buy);

		// Market data: quote with bid=130
		var quote = new QuoteChangeMessage
		{
			SecurityId = secId,
			Bids = [new(130m, 1)],
			Asks = [new(131m, 1)]
		};
		manager.ProcessMessage(quote);
		manager.UnrealizedPnL.AssertEqual(30m); // (130-100)*1 = 30

		// Newer market data: tick at 150
		// The newer tick must replace the quote prices.
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 150m,
			ServerTime = DateTime.UtcNow
		};
		manager.ProcessMessage(tick);

		// Expected: (150-100)*1 = 50
		manager.UnrealizedPnL.AssertEqual(50m);
	}

	[TestMethod]
	public void QueueRecomputesAfterSecurityParamsChange()
	{
		// A PnL that was already read is cached. Changing the instrument's step price, price step or
		// lot multiplier must make the next read reflect the new money value of the same price move.
		var secId = Helper.CreateSecurityId();
		var queue = new PnLQueue(secId);

		queue.UpdateSecurity(new Level1ChangeMessage { SecurityId = secId }
			.Add(Level1Fields.PriceStep, 0.5m)
			.Add(Level1Fields.StepPrice, 2m));

		queue.Process(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 3m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		}).PnL.AssertEqual(0m);

		queue.ProcessExecution(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 110m
		});

		// A move of 10 is 20 steps of 0.5, each worth 2 => 40 per unit, 3 units held.
		queue.UnrealizedPnL.AssertEqual(120m);

		// Step price doubles => the very same move is worth twice as much money.
		queue.UpdateSecurity(new Level1ChangeMessage { SecurityId = secId }
			.Add(Level1Fields.StepPrice, 4m));
		queue.UnrealizedPnL.AssertEqual(240m);

		// Price step halves => twice as many steps fit into the same move.
		queue.UpdateSecurity(new Level1ChangeMessage { SecurityId = secId }
			.Add(Level1Fields.PriceStep, 0.25m));
		queue.UnrealizedPnL.AssertEqual(480m);

		// Lot multiplier: one traded lot is ten units of the instrument.
		queue.UpdateSecurity(new Level1ChangeMessage { SecurityId = secId }
			.Add(Level1Fields.Multiplier, 10m));
		queue.UnrealizedPnL.AssertEqual(4800m);

		// Closing now realizes at the parameters in force, and leaves nothing unrealized.
		queue.Process(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 2,
			TradePrice = 110m,
			TradeVolume = 3m,
			Side = Sides.Sell,
			ServerTime = DateTime.UtcNow
		}).PnL.AssertEqual(4800m);

		queue.RealizedPnL.AssertEqual(4800m);
		queue.UnrealizedPnL.AssertEqual(0m);
	}

	[TestMethod]
	public void QueueLateParamsMatchQueueConfiguredUpfront()
	{
		// Whatever leverage and lot multiplier do to the numbers, *when* they are applied must not:
		// a queue told about them after a PnL was already read must agree with one told upfront.
		var secId = Helper.CreateSecurityId();

		static PnLQueue create(SecurityId id)
		{
			var q = new PnLQueue(id);

			q.UpdateSecurity(new Level1ChangeMessage { SecurityId = id }
				.Add(Level1Fields.PriceStep, 0.5m)
				.Add(Level1Fields.StepPrice, 2m));

			return q;
		}

		var buy = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 3m,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow
		};

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 110m
		};

		var sell = buy.TypedClone();
		sell.TradeId = 2;
		sell.Side = Sides.Sell;
		sell.TradePrice = 110m;

		var upfront = create(secId);
		upfront.Leverage = 4m;
		upfront.LotMultiplier = 5m;
		upfront.Process(buy);
		upfront.ProcessExecution(tick);

		var late = create(secId);
		late.Process(buy);
		late.ProcessExecution(tick);

		// Fill the cache while the queue still knows nothing about leverage or lot multiplier.
		late.UnrealizedPnL.AssertEqual(120m);

		late.Leverage = 4m;
		late.LotMultiplier = 5m;

		// The parameters must move the number, otherwise the comparison below proves nothing.
		upfront.UnrealizedPnL.AssertNotEqual(120m);
		late.UnrealizedPnL.AssertEqual(upfront.UnrealizedPnL);

		upfront.Process(sell);
		late.Process(sell);

		late.RealizedPnL.AssertEqual(upfront.RealizedPnL);
		late.UnrealizedPnL.AssertEqual(0m);
	}

	[TestMethod]
	public void ManagerSameTradeIdOnDifferentSecurities()
	{
		// A trade id is issued per instrument, so two instruments may both report trade 1. They are
		// two different trades and both must reach their own queue.
		var secA = Helper.CreateSecurityId();
		var secB = Helper.CreateSecurityId();

		var manager = CreatePfManager();

		manager.ProcessMyTrade(CreateMyTrade(secA, 1, null, Sides.Buy, 10m, 1m), out var openA).AssertTrue();
		openA.PnL.AssertEqual(0m);

		manager.ProcessMyTrade(CreateMyTrade(secB, 1, null, Sides.Buy, 20m, 1m), out var openB).AssertTrue();
		openB.PnL.AssertEqual(0m);

		manager.ProcessMyTrade(CreateMyTrade(secA, 2, null, Sides.Sell, 12m, 1m), out var closeA).AssertTrue();
		closeA.PnL.AssertEqual(2m);

		manager.ProcessMyTrade(CreateMyTrade(secB, 3, null, Sides.Sell, 25m, 1m), out var closeB).AssertTrue();
		closeB.PnL.AssertEqual(5m);

		manager.RealizedPnL.AssertEqual(7m);
	}

	[TestMethod]
	public void ManagerRedeliveredTradeReturnsPreviousInfo()
	{
		// A trade delivered twice is counted once, and the second call hands back the information
		// produced by the first rather than a fresh calculation.
		var secId = Helper.CreateSecurityId();
		var manager = CreatePfManager();

		manager.ProcessMyTrade(CreateMyTrade(secId, 1, null, Sides.Buy, 10m, 2m), out _).AssertTrue();

		var sell = CreateMyTrade(secId, 2, null, Sides.Sell, 12m, 1m);
		manager.ProcessMyTrade(sell, out var first).AssertTrue();
		first.PnL.AssertEqual(2m);
		first.ClosedVolume.AssertEqual(1m);

		manager.ProcessMyTrade(sell.TypedClone(), out var again).AssertFalse();
		again.AssertSame(first);

		manager.RealizedPnL.AssertEqual(2m);
	}

	[TestMethod]
	public void ManagerTradeWithBothIdsNotReprocessedByStringId()
	{
		// The first delivery carried both ids, so "T2" is that trade's own string id. A re-delivery
		// naming it that way is the same trade and must not close a second unit of the position.
		var secId = Helper.CreateSecurityId();
		var manager = CreatePfManager();

		manager.ProcessMyTrade(CreateMyTrade(secId, 1, "T1", Sides.Buy, 10m, 2m), out _).AssertTrue();

		manager.ProcessMyTrade(CreateMyTrade(secId, 2, "T2", Sides.Sell, 12m, 1m), out var first).AssertTrue();
		first.PnL.AssertEqual(2m);

		manager.ProcessMyTrade(CreateMyTrade(secId, null, "T2", Sides.Sell, 12m, 1m), out _).AssertFalse();

		manager.RealizedPnL.AssertEqual(2m);

		// One of the two bought units is still open, and still worth its move.
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 12m,
			ServerTime = DateTime.UtcNow
		});

		manager.UnrealizedPnL.AssertEqual(2m);
	}

	[TestMethod]
	public void ManagerTradeWithStringIdNotReprocessedByNumericId()
	{
		// Mirror of the above: the trade first arrived under its string id alone, and the later
		// delivery names that same string id alongside a numeric one. Still one trade.
		var secId = Helper.CreateSecurityId();
		var manager = CreatePfManager();

		manager.ProcessMyTrade(CreateMyTrade(secId, null, "T1", Sides.Buy, 10m, 2m), out _).AssertTrue();

		manager.ProcessMyTrade(CreateMyTrade(secId, null, "T2", Sides.Sell, 12m, 1m), out var first).AssertTrue();
		first.PnL.AssertEqual(2m);

		manager.ProcessMyTrade(CreateMyTrade(secId, 2, "T2", Sides.Sell, 12m, 1m), out _).AssertFalse();

		manager.RealizedPnL.AssertEqual(2m);

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 12m,
			ServerTime = DateTime.UtcNow
		});

		manager.UnrealizedPnL.AssertEqual(2m);
	}

	/// <summary>
	/// Not every venue names its fills. An execution that carries a price and a volume and no
	/// identifier of any kind is still a fill - the framework itself calls it one, and the position
	/// manager moves the position on it by volume alone. The money changed hands, so it must reach
	/// the profit and loss as well: dropping it silently leaves a user whose broker issues no trade
	/// ids with a position that grows and a result that stays at zero, and nothing anywhere says why.
	/// </summary>
	[TestMethod]
	public void FillWithoutAnyIdStillCountsTowardsPnL()
	{
		var secId = Helper.CreateSecurityId();
		var manager = CreatePfManager();

		var buy = CreateMyTrade(secId, null, null, Sides.Buy, 100m, 10m);

		// The premise: an id-less execution is a fill, and reaches the manager as one.
		buy.HasTradeInfo.AssertTrue("a price and a volume make a fill, with or without an id");

		manager.ProcessMyTrade(buy, out var opened).AssertTrue("the fill must be accounted");
		opened.AssertNotNull("an accounted fill reports what it did");
		opened.PnL.AssertEqual(0m);

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 110m,
			ServerTime = DateTime.UtcNow
		});

		// Ten units bought at 100 and last traded at 110 are ten points up.
		manager.UnrealizedPnL.AssertEqual(100m);

		manager.ProcessMyTrade(CreateMyTrade(secId, null, null, Sides.Sell, 110m, 10m), out var closed).AssertTrue("the closing fill must be accounted");
		closed.ClosedVolume.AssertEqual(10m);
		closed.PnL.AssertEqual(100m);

		manager.RealizedPnL.AssertEqual(100m);
		manager.UnrealizedPnL.AssertEqual(0m);
	}

	[TestMethod]
	public void QueueClosesOldestLotFirst()
	{
		// Two lots bought at different prices, then one unit sold: the sale closes the lot opened
		// first, so the realized part is measured against 100 and the 110 lot is what stays open.
		var secId = Helper.CreateSecurityId();
		var queue = CreateQueue(secId);

		queue.Process(CreateMyTrade(secId, 1, null, Sides.Buy, 100m, 1m)).PnL.AssertEqual(0m);
		queue.Process(CreateMyTrade(secId, 2, null, Sides.Buy, 110m, 1m)).PnL.AssertEqual(0m);

		var close = queue.Process(CreateMyTrade(secId, 3, null, Sides.Sell, 105m, 1m));

		close.ClosedVolume.AssertEqual(1m);
		close.PnL.AssertEqual(5m);
		queue.RealizedPnL.AssertEqual(5m);

		queue.ProcessExecution(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 105m,
			ServerTime = DateTime.UtcNow
		});

		// What is left open is the lot bought at 110, so at 105 it is 5 under water.
		queue.UnrealizedPnL.AssertEqual(-5m);
	}

	[TestMethod]
	public void QueueLeverageScalesPnL()
	{
		// PnLQueue uses the leverage supplied by PositionChangeMessage as part of its contract-value
		// multiplier. A tenfold leverage therefore scales both unrealized and realized PnL by ten.
		var secId = Helper.CreateSecurityId();

		var plain = CreateQueue(secId);
		var levered = CreateQueue(secId);
		levered.Leverage = 10m;

		var buy = CreateMyTrade(secId, 1, null, Sides.Buy, 100m, 1m);
		var sell = CreateMyTrade(secId, 2, null, Sides.Sell, 110m, 1m);

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 110m,
			ServerTime = DateTime.UtcNow
		};

		plain.Process(buy);
		levered.Process(buy);

		plain.ProcessExecution(tick);
		levered.ProcessExecution(tick);

		plain.UnrealizedPnL.AssertEqual(10m);
		levered.UnrealizedPnL.AssertEqual(100m);

		plain.Process(sell);
		levered.Process(sell).PnL.AssertEqual(100m);

		plain.RealizedPnL.AssertEqual(10m);
		levered.RealizedPnL.AssertEqual(100m);
	}

	private static PnLQueue CreateQueue(SecurityId secId)
	{
		var queue = new PnLQueue(secId);

		queue.UpdateSecurity(new Level1ChangeMessage { SecurityId = secId }
			.Add(Level1Fields.PriceStep, 1m)
			.Add(Level1Fields.StepPrice, 1m));

		return queue;
	}

	private static PortfolioPnLManager CreatePfManager()
		=> new("pf", id => new Level1ChangeMessage { SecurityId = id }
			.Add(Level1Fields.PriceStep, 1m)
			.Add(Level1Fields.StepPrice, 1m));

	private static ExecutionMessage CreateMyTrade(SecurityId secId, long? tradeId, string tradeStringId, Sides side, decimal price, decimal volume)
		=> new()
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradeId = tradeId,
			TradeStringId = tradeStringId,
			TradePrice = price,
			TradeVolume = volume,
			Side = side,
			ServerTime = DateTime.UtcNow
		};
}
