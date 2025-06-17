namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

[TestClass]
public class MarketEmulatorTests
{
	private static IMarketEmulator CreateEmuWithEvents(SecurityId secId, out List<Message> result)
	{
		var emu = new MarketEmulator(new CollectionSecurityProvider([new() { Id = secId.ToStringId() }]), new CollectionPortfolioProvider([Portfolio.CreateSimulator()]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator()) { VerifyMode = true };
		var result2 = new List<Message>();
		emu.NewOutMessage += result2.Add;
		result = result2;
		return emu;
	}

	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;
	private static readonly IdGenerator _idGenerator = new IncrementalIdGenerator();

	private static void AddBook(IMarketEmulator emu, SecurityId secId, DateTimeOffset now, decimal bid = 100, decimal ask = 101)
	{
		emu.SendInMessage(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(bid, 10)],
			Asks = [new(ask, 10)]
		});
	}

	[TestMethod]
	public void OrderMatcher()
	{
		static ExecutionMessage CreateQuote(Sides side, decimal price, decimal volume, SecurityId secId)
		{
			return new ExecutionMessage
			{
				LocalTime = DateTimeOffset.UtcNow,
				SecurityId = secId,
				Side = side,
				OrderPrice = price,
				OrderVolume = volume,
				DataTypeEx = DataType.OrderLog,
			};
		}

		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);

		emu.SendInMessage(CreateQuote(Sides.Buy, 90, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Buy, 91, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Buy, 92, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Buy, 93, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Buy, 94, 1, id));

		emu.SendInMessage(CreateQuote(Sides.Sell, 96, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Sell, 97, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Sell, 98, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Sell, 99, 1, id));
		emu.SendInMessage(CreateQuote(Sides.Sell, 100, 1, id));

		emu.SendInMessage(new ExecutionMessage
		{
			LocalTime = DateTimeOffset.UtcNow,
			SecurityId = id,
			Side = Sides.Buy,
			TransactionId = _idGenerator.GetNextId(),
			OrderPrice = 96,
			OrderVolume = 2,
			PortfolioName = "test",
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});

		res.Count.AssertEqual(6);

		emu.SendInMessage(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = "test",
			LocalTime = DateTimeOffset.UtcNow,
		}
		.Add(PositionChangeTypes.BeginValue, 100000000m)
		.Add(PositionChangeTypes.CurrentValue, 100000000m));

		emu.SendInMessage(new ExecutionMessage
		{
			LocalTime = DateTimeOffset.UtcNow,
			SecurityId = id,
			Side = Sides.Buy,
			TransactionId = _idGenerator.GetNextId(),
			OrderPrice = 96,
			OrderVolume = 2,
			PortfolioName = "test",
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});

		res.Count.AssertEqual(9);
	}

	[TestMethod]
	public void LimitBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void LimitSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void LimitBuyFOKFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void LimitBuyFOKNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 102,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void LimitSellFOKFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void LimitSellFOKNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 98,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void LimitBuyIOCFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void LimitBuyIOCPartial()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(5);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(10);
	}

	[TestMethod]
	public void LimitBuyIOCNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(15);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();
	}

	[TestMethod]
	public void LimitSellIOCFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void LimitSellIOCPartial()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(5);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(10);
	}

	[TestMethod]
	public void LimitSellIOCNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(15);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();
	}

	[TestMethod]
	public void LimitBuyPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 99,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void LimitSellPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 102,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void MarketBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void MarketSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public void ExpiryDateLimitOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		var expiry = DateTime.UtcNow.AddDays(1);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();
		emu.SendInMessage(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = expiry.AddSeconds(1),
			ServerTime = expiry.AddSeconds(1),
			DataTypeEx = DataType.Ticks,
			TradePrice = 105,
			TradeVolume = 2
		});
		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void ExpiryDateInvalidLimitOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow.AddDays(1);
		var expiry = DateTime.UtcNow;

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void ReplaceOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		var replace = new OrderReplaceMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OriginalTransactionId = reg.TransactionId,
			OldOrderId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		res.Clear();

		emu.SendInMessage(replace);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void ReplaceOrderAndMatch()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		var replace = new OrderReplaceMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OriginalTransactionId = reg.TransactionId,
			OldOrderId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		res.Clear();

		emu.SendInMessage(replace);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(replace.Volume);
	}

	[TestMethod]
	public void CancelOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		AddBook(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		emu.SendInMessage(new OrderCancelMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OrderId = 1,
			OriginalTransactionId = reg.TransactionId,
			PortfolioName = _pfName,
		});

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public void CandleExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTimeOffset.UtcNow;
		emu.SendInMessage(new MarketDataMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
		});
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
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 104,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
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
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
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

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
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
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 105,
			Volume = 4,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
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
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
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
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 2,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(2);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 2,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		emu.SendInMessage(reg);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}
}
