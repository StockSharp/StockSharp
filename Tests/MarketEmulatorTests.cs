namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

[TestClass]
public class MarketEmulatorTests
{
	private static IMarketEmulator CreateEmuWithEvents(SecurityId secId, out List<Message> result)
	{
		var emu = new MarketEmulator(new CollectionSecurityProvider([new() { Id = secId.ToStringId() }]), new CollectionPortfolioProvider([Portfolio.CreateSimulator()]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator());
		var result2 = new List<Message>();
		emu.NewOutMessage += result2.Add;
		result = result2;
		return emu;
	}

	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;

	private static void AddBook(IMarketEmulator emu, SecurityId secId, DateTimeOffset now, decimal bid = 100, decimal ask = 101)
	{
		emu.SendInMessage(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new QuoteChange(bid, 10)],
			Asks = [new QuoteChange(ask, 10)]
		});
	}

	[TestMethod]
	public void LimitBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		});

		var m = res.Find(x => x is ExecutionMessage em && em.TransactionId == 1);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void LimitSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		});

		var m = res.Find(x => x is ExecutionMessage em && em.TransactionId == 2);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void LimitBuyIOCOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 3,
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 3);
		m.AssertNotNull();
		(((ExecutionMessage)m).OrderState == OrderStates.Done || ((ExecutionMessage)m).IsCanceled()).AssertTrue();
	}

	[TestMethod]
	public void LimitSellIOCOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 4,
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 4);
		m.AssertNotNull();
		(((ExecutionMessage)m).OrderState == OrderStates.Done || ((ExecutionMessage)m).IsCanceled()).AssertTrue();
	}

	[TestMethod]
	public void LimitBuyPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 5,
			Side = Sides.Buy,
			Price = 99,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 5);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void LimitSellPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 6,
			Side = Sides.Sell,
			Price = 102,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 6);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void MarketBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 7,
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 7);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void MarketSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 8,
			Side = Sides.Sell,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 8);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void ExpiryDateLimitOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		var expiry = DateTime.UtcNow.AddDays(1);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 9,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			ExpiryDate = expiry,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 9);
		m.AssertNotNull();
		((ExecutionMessage)m).ExpiryDate.AssertEqual(expiry);
	}

	[TestMethod]
	public void ReplaceOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 10,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});
		emu.SendInMessage(new OrderReplaceMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = 11,
			OriginalTransactionId = 10,
			OldOrderId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 2,
			OrderType = OrderTypes.Limit
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 11);
		m.AssertNotNull();
	}

	[TestMethod]
	public void CancelOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 12,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});
		emu.SendInMessage(new OrderCancelMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = 13,
			OrderId = 1,
			OriginalTransactionId = 12
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 13);
		m.AssertNotNull();
		((ExecutionMessage)m).OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void CandleExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OpenTime = now.AddMinutes(-5),
			CloseTime = now,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 104,
			TotalVolume = 100
		});
		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 14,
			Side = Sides.Buy,
			Price = 104,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 14);
		m.AssertNotNull();
	}

	[TestMethod]
	public void TickExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.Ticks,
			TradePrice = 105,
			TradeVolume = 2
		});
		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 15,
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 15);
		m.AssertNotNull();
	}

	[TestMethod]
	public void Level1Execution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new Level1ChangeMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now
		}
		.Add(Level1Fields.BestBidPrice, 104m)
		.Add(Level1Fields.BestAskPrice, 105m)
		.Add(Level1Fields.BestBidVolume, 1m)
		.Add(Level1Fields.BestAskVolume, 2m));

		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 16,
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 16);
		m.AssertNotNull();
	}

	[TestMethod]
	public void OrderLogExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.OrderLog,
			OrderPrice = 106,
			OrderVolume = 4,
			Side = Sides.Buy
		});
		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 17,
			Side = Sides.Buy,
			Price = 106,
			Volume = 4,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 17);
		m.AssertNotNull();
	}

	[TestMethod]
	public void MarketOrderOnTickExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.Ticks,
			TradePrice = 107,
			TradeVolume = 1
		});
		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 18,
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 18);
		m.AssertNotNull();
	}

	[TestMethod]
	public void MarketOrderOnOrderLogExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;

		emu.SendInMessage(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.OrderLog,
			OrderPrice = 108,
			OrderVolume = 2,
			Side = Sides.Sell
		});
		emu.SendInMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = 19,
			Side = Sides.Sell,
			Volume = 2,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		});

		var m = res.FindLast(x => x is ExecutionMessage em && em.TransactionId == 19);
		m.AssertNotNull();
	}
}
