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

	/// <summary>
	/// Compare ALL fields of two ExecutionMessages.
	/// </summary>
	private static List<string> CompareExecutionMessages(ExecutionMessage v1, ExecutionMessage v2, string context)
	{
		var diffs = new List<string>();

		if (v1.SecurityId != v2.SecurityId)
			diffs.Add($"{context}: SecurityId V1={v1.SecurityId}, V2={v2.SecurityId}");
		if (v1.PortfolioName != v2.PortfolioName)
			diffs.Add($"{context}: PortfolioName V1={v1.PortfolioName}, V2={v2.PortfolioName}");
		if (v1.TransactionId != v2.TransactionId)
			diffs.Add($"{context}: TransactionId V1={v1.TransactionId}, V2={v2.TransactionId}");
		if (v1.OriginalTransactionId != v2.OriginalTransactionId)
			diffs.Add($"{context}: OriginalTransactionId V1={v1.OriginalTransactionId}, V2={v2.OriginalTransactionId}");
		if (v1.HasOrderInfo != v2.HasOrderInfo)
			diffs.Add($"{context}: HasOrderInfo V1={v1.HasOrderInfo}, V2={v2.HasOrderInfo}");
		if (v1.IsCancellation != v2.IsCancellation)
			diffs.Add($"{context}: IsCancellation V1={v1.IsCancellation}, V2={v2.IsCancellation}");
		if (v1.OrderPrice != v2.OrderPrice)
			diffs.Add($"{context}: OrderPrice V1={v1.OrderPrice}, V2={v2.OrderPrice}");
		if (v1.OrderVolume != v2.OrderVolume)
			diffs.Add($"{context}: OrderVolume V1={v1.OrderVolume}, V2={v2.OrderVolume}");
		if (v1.VisibleVolume != v2.VisibleVolume)
			diffs.Add($"{context}: VisibleVolume V1={v1.VisibleVolume}, V2={v2.VisibleVolume}");
		if (v1.Side != v2.Side)
			diffs.Add($"{context}: Side V1={v1.Side}, V2={v2.Side}");
		if (v1.Balance != v2.Balance)
			diffs.Add($"{context}: Balance V1={v1.Balance}, V2={v2.Balance}");
		if (v1.OrderType != v2.OrderType)
			diffs.Add($"{context}: OrderType V1={v1.OrderType}, V2={v2.OrderType}");
		if (v1.OrderState != v2.OrderState)
			diffs.Add($"{context}: OrderState V1={v1.OrderState}, V2={v2.OrderState}");
		if (v1.TimeInForce != v2.TimeInForce)
			diffs.Add($"{context}: TimeInForce V1={v1.TimeInForce}, V2={v2.TimeInForce}");
		if (v1.TradePrice != v2.TradePrice)
			diffs.Add($"{context}: TradePrice V1={v1.TradePrice}, V2={v2.TradePrice}");
		if (v1.TradeVolume != v2.TradeVolume)
			diffs.Add($"{context}: TradeVolume V1={v1.TradeVolume}, V2={v2.TradeVolume}");
		if (v1.Commission != v2.Commission)
			diffs.Add($"{context}: Commission V1={v1.Commission}, V2={v2.Commission}");
		if (v1.Slippage != v2.Slippage)
			diffs.Add($"{context}: Slippage V1={v1.Slippage}, V2={v2.Slippage}");

		var err1 = v1.Error?.Message;
		var err2 = v2.Error?.Message;
		if (err1 != err2)
			diffs.Add($"{context}: Error V1={err1}, V2={err2}");

		return diffs;
	}

	/// <summary>
	/// Compare ALL fields of two PositionChangeMessages.
	/// </summary>
	private static List<string> ComparePositionMessages(PositionChangeMessage v1, PositionChangeMessage v2, string context)
	{
		var diffs = new List<string>();

		if (v1.SecurityId != v2.SecurityId)
			diffs.Add($"{context}: SecurityId V1={v1.SecurityId}, V2={v2.SecurityId}");
		if (v1.PortfolioName != v2.PortfolioName)
			diffs.Add($"{context}: PortfolioName V1={v1.PortfolioName}, V2={v2.PortfolioName}");

		var allKeys = v1.Changes.Keys.Union(v2.Changes.Keys).ToList();
		foreach (var key in allKeys)
		{
			var hasV1 = v1.Changes.TryGetValue(key, out var val1);
			var hasV2 = v2.Changes.TryGetValue(key, out var val2);

			if (hasV1 && !hasV2)
				diffs.Add($"{context}: {key} V1={val1}, V2=MISSING");
			else if (!hasV1 && hasV2)
				diffs.Add($"{context}: {key} V1=MISSING, V2={val2}");
			else if (hasV1 && hasV2 && !Equals(val1, val2))
				diffs.Add($"{context}: {key} V1={val1}, V2={val2}");
		}

		return diffs;
	}

	/// <summary>
	/// Compare single message.
	/// </summary>
	private static List<string> CompareMessage(Message v1, Message v2, string context)
	{
		var diffs = new List<string>();

		if (v1.GetType() != v2.GetType())
		{
			diffs.Add($"{context}: Type V1={v1.GetType().Name}, V2={v2.GetType().Name}");
			return diffs;
		}

		if (v1 is ExecutionMessage exec1 && v2 is ExecutionMessage exec2)
			diffs.AddRange(CompareExecutionMessages(exec1, exec2, context));
		else if (v1 is PositionChangeMessage pos1 && v2 is PositionChangeMessage pos2)
			diffs.AddRange(ComparePositionMessages(pos1, pos2, context));

		return diffs;
	}

	/// <summary>
	/// Full 1:1 comparison of all messages.
	/// </summary>
	private static List<string> Compare(List<Message> v1, List<Message> v2)
	{
		var diffs = new List<string>();

		if (v1.Count != v2.Count)
			diffs.Add($"Count: V1={v1.Count}, V2={v2.Count}");

		var max = Math.Max(v1.Count, v2.Count);
		for (int i = 0; i < max; i++)
		{
			if (i >= v1.Count)
			{
				diffs.Add($"[{i}]: V1=MISSING, V2={v2[i].GetType().Name}");
				continue;
			}
			if (i >= v2.Count)
			{
				diffs.Add($"[{i}]: V1={v1[i].GetType().Name}, V2=MISSING");
				continue;
			}

			diffs.AddRange(CompareMessage(v1[i], v2[i], $"[{i}]"));
		}

		return diffs;
	}

	private void AssertEqual(List<Message> resV1, List<Message> resV2, string testName)
	{
		var diffs = Compare(resV1, resV2);

		Console.WriteLine($"\n=== {testName} ===");
		Console.WriteLine($"V1: {resV1.Count} messages, V2: {resV2.Count} messages");

		if (diffs.Count > 0)
		{
			Console.WriteLine($"Differences: {diffs.Count}");
			foreach (var d in diffs.Take(50))
				Console.WriteLine($"  {d}");
			if (diffs.Count > 50)
				Console.WriteLine($"  ... and {diffs.Count - 50} more");
		}

		AreEqual(0, diffs.Count, $"{testName}: {diffs.Count} differences");
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
