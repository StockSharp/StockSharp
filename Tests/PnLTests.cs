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
			ServerTime = DateTimeOffset.UtcNow
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
			ServerTime = DateTimeOffset.UtcNow
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
		};
		manager.ProcessMessage(sellMsg);

		// Realized PnL = (110-100)*10 = 100
		manager.RealizedPnL.AssertEqual(100);
		manager.UnrealizedPnL.AssertEqual(0);
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
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
			ServerTime = DateTimeOffset.Now,
		});

		// Sell in B
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 2,
			SecurityId = regB.SecurityId,
			PortfolioName = regB.PortfolioName,
			Side = Sides.Sell,
			TradePrice = 180,
			TradeId = 4,
			TradeVolume = 5,
			ServerTime = DateTimeOffset.Now,
		});

		// Portfolio A: (110-100)*10 = 100
		// Portfolio B: (180-200)*5 = -100
		// Net: 0
		manager.RealizedPnL.AssertEqual(0);
		manager.GetPnL().AssertEqual(0);
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
			ServerTime = DateTimeOffset.UtcNow
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
			OpenTime = DateTimeOffset.Now,
			CloseTime = DateTimeOffset.Now,
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
		var manager = new PnLManager { UseLevel1 = true };
		
		var storage = manager.Save();

		var manager2 = new PnLManager();
		
		manager2.UseLevel1.AssertFalse();
		manager2.Load(storage);
		manager2.UseLevel1.AssertTrue();
	}
}