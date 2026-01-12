namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Comparison tests between MarketEmulatorOld (V1) and MarketEmulator (V2).
/// All tests run both emulators and compare results 1:1.
/// </summary>
[TestClass]
public class MarketEmulatorComparisonTests : BaseTestClass
{
	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;

	#region Helpers

	private (IMarketEmulator emuV1, List<Message> resV1, IMarketEmulator emuV2, List<Message> resV2) CreateBothEmulators(SecurityId secId)
	{
		var securities = new[] { new Security { Id = secId.ToStringId() } };
		var secProvider = new CollectionSecurityProvider(securities);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchProvider = new InMemoryExchangeInfoProvider();

		// Mock random for reproducible tests
		var mockRandom = new MockRandomProvider();

		// V1
		var emuV1 = new MarketEmulatorOld(secProvider, pfProvider, exchProvider, new IncrementalIdGenerator()) { VerifyMode = false };
		emuV1.Settings.Failing = 0;
		emuV1.Settings.Latency = TimeSpan.Zero;
		emuV1.RandomProvider = mockRandom;
		var resV1 = new List<Message>();
		emuV1.NewOutMessage += resV1.Add;

		// V2
		var emuV2 = new MarketEmulator(secProvider, pfProvider, exchProvider, new IncrementalIdGenerator()) { VerifyMode = false };
		emuV2.OrderIdGenerator = new IncrementalIdGenerator();
		emuV2.TradeIdGenerator = new IncrementalIdGenerator();
		emuV2.RandomProvider = mockRandom;
		var resV2 = new List<Message>();
		emuV2.NewOutMessage += resV2.Add;

		return (emuV1, resV1, emuV2, resV2);
	}

	private async Task InitMoney(IMarketEmulator emu, DateTime time, decimal amount = 1000000m)
	{
		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = time,
			ServerTime = time,
		}.Add(PositionChangeTypes.BeginValue, amount), CancellationToken);
	}

	private async Task SendBook(IMarketEmulator emu, SecurityId secId, DateTime time, decimal bid, decimal ask, decimal volume = 100m)
	{
		await emu.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = time,
			ServerTime = time,
			Bids = [new(bid, volume)],
			Asks = [new(ask, volume)]
		}, CancellationToken);
	}

	private void AssertEqual(List<Message> resV1, List<Message> resV2, string testName)
	{
		Console.WriteLine($"\n=== {testName} ===");
		Console.WriteLine($"V1: {resV1.Count} messages, V2: {resV2.Count} messages");

		resV1.Count.AssertEqual(resV2.Count);

		for (var i = 0; i < resV1.Count; i++)
			Helper.CheckEqual(resV1[i], resV2[i]);
	}

	#endregion

	#region Limit Order Tests

	[TestMethod]
	public async Task LimitBuyPutInQueue()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitBuyPutInQueue");
	}

	[TestMethod]
	public async Task LimitSellPutInQueue()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitSellPutInQueue");
	}

	[TestMethod]
	public async Task LimitBuyExecute()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitBuyExecute");
	}

	[TestMethod]
	public async Task LimitSellExecute()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitSellExecute");
	}

	#endregion

	#region IOC Tests

	[TestMethod]
	public async Task IOC_FullFill()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "IOC_FullFill");
	}

	[TestMethod]
	public async Task IOC_PartialFill()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101, 10);
		await SendBook(emuV2, secId, now, 100, 101, 10);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "IOC_PartialFill");
	}

	[TestMethod]
	public async Task IOC_NoFill()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "IOC_NoFill");
	}

	#endregion

	#region FOK Tests

	[TestMethod]
	public async Task FOK_FullFill()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "FOK_FullFill");
	}

	[TestMethod]
	public async Task FOK_Reject()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101, 10);
		await SendBook(emuV2, secId, now, 100, 101, 10);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "FOK_Reject");
	}

	#endregion

	#region Market Order Tests

	[TestMethod]
	public async Task MarketBuy()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Volume = 5,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "MarketBuy");
	}

	[TestMethod]
	public async Task MarketSell()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Sell,
			Volume = 5,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "MarketSell");
	}

	#endregion

	#region PostOnly Tests

	[TestMethod]
	public async Task PostOnly_NoMatch()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "PostOnly_NoMatch");
	}

	[TestMethod]
	public async Task PostOnly_WouldMatch_Reject()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "PostOnly_WouldMatch_Reject");
	}

	#endregion

	#region Cancel Order Tests

	[TestMethod]
	public async Task CancelOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		var regOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var cancelOrder = new OrderCancelMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			OriginalTransactionId = 1,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(cancelOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(cancelOrder.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "CancelOrder");
	}

	#endregion

	#region Replace Order Tests

	[TestMethod]
	public async Task ReplaceOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		var regOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var replaceOrder = new OrderReplaceMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			OriginalTransactionId = 1,
			Price = 98,
			Volume = 10,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(replaceOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(replaceOrder.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "ReplaceOrder");
	}

	[TestMethod]
	public async Task ReplaceOrder_AndMatch()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		var regOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(regOrder.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var replaceOrder = new OrderReplaceMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			OriginalTransactionId = 1,
			Price = 101,
			Volume = 5,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(replaceOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(replaceOrder.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "ReplaceOrder_AndMatch");
	}

	#endregion

	#region Position & PnL Tests

	[TestMethod]
	public async Task BuyThenSell_RealizedPnL()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		// Buy
		var buyOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);

		now = now.AddSeconds(1);

		await SendBook(emuV1, secId, now, 110, 111);
		await SendBook(emuV2, secId, now, 110, 111);

		// Sell
		var sellOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Sell,
			Price = 110,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "BuyThenSell_RealizedPnL");
	}

	[TestMethod]
	public async Task OpenPosition_UnrealizedPnL()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		// Buy
		var buyOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		now = now.AddSeconds(1);

		// Price moves up
		await SendBook(emuV1, secId, now, 110, 111);
		await SendBook(emuV2, secId, now, 110, 111);

		AssertEqual(resV1, resV2, "OpenPosition_UnrealizedPnL");
	}

	[TestMethod]
	public async Task ActiveOrder_BlockedValue()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "ActiveOrder_BlockedValue");
	}

	#endregion

	#region OrderStatus Tests

	[TestMethod]
	public async Task OrderStatus()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		// Create orders
		var order1 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		var order2 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Sell,
			Price = 102,
			Volume = 3,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV1.SendInMessageAsync(order2.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order2.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var statusMsg = new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
		};

		await emuV1.SendInMessageAsync(statusMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(statusMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "OrderStatus");
	}

	#endregion

	#region GroupCancel Tests

	[TestMethod]
	public async Task GroupCancelOrders()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);
		await SendBook(emuV1, secId, now, 100, 101);
		await SendBook(emuV2, secId, now, 100, 101);

		var order1 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		var order2 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Buy,
			Price = 98,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV1.SendInMessageAsync(order2.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order2.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var cancelMsg = new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = 100,
			Mode = OrderGroupCancelModes.CancelOrders,
		};

		await emuV1.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "GroupCancelOrders");
	}

	#endregion
}
