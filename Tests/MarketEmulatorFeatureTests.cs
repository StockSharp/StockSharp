namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Comprehensive feature comparison tests between MarketEmulatorOld (V1) and MarketEmulator (V2).
/// All tests create BOTH emulators and compare results 1:1.
/// </summary>
[TestClass]
public class MarketEmulatorFeatureTests : BaseTestClass
{
	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;

	private (IMarketEmulator emuV1, List<Message> resV1, IMarketEmulator emuV2, List<Message> resV2) CreateBothEmulators(SecurityId secId)
		=> CreateBothEmulators([secId]);

	private (IMarketEmulator emuV1, List<Message> resV1, IMarketEmulator emuV2, List<Message> resV2) CreateBothEmulators(IEnumerable<SecurityId> secIds)
	{
		var securities = secIds.Select(id => new Security { Id = id.ToStringId() });
		var secProvider = new CollectionSecurityProvider(securities);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchProvider = new InMemoryExchangeInfoProvider();

		// Mock random for reproducible tests
		var mockRandom = new MockRandomProvider();

		// V1
		var idGen1 = new IncrementalIdGenerator();
		var emu1 = new MarketEmulatorOld(secProvider, pfProvider, exchProvider, idGen1) { VerifyMode = false };
		emu1.Settings.Failing = 0;
		emu1.Settings.Latency = TimeSpan.Zero;
		emu1.RandomProvider = mockRandom;
		var res1 = new List<Message>();
		emu1.NewOutMessageAsync += (m, ct) => { res1.Add(m); return default; };

		// V2
		var idGen2 = new IncrementalIdGenerator();
		var emu2 = new MarketEmulator(secProvider, pfProvider, exchProvider, idGen2) { VerifyMode = false };
		emu2.OrderIdGenerator = new IncrementalIdGenerator();
		emu2.TradeIdGenerator = new IncrementalIdGenerator();
		emu2.RandomProvider = mockRandom;
		var res2 = new List<Message>();
		emu2.NewOutMessageAsync += (m, ct) => { res2.Add(m); return default; };

		return (emu1, res1, emu2, res2);
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

	private async Task SendTick(IMarketEmulator emu, SecurityId secId, DateTime time, decimal price, decimal volume = 10m)
	{
		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = secId,
			LocalTime = time,
			ServerTime = time,
			DataTypeEx = DataType.Ticks,
			TradePrice = price,
			TradeVolume = volume
		}, CancellationToken);
	}

	private async Task SendCandle(IMarketEmulator emu, SecurityId secId, DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume = 100m)
	{
		// Subscribe to candles first (required by emulator)
		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = DateTime.UtcNow.Ticks,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = secId,
			IsSubscribe = true,
		}, CancellationToken);

		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = secId,
			OpenTime = time.AddMinutes(-5),
			CloseTime = time,
			OpenPrice = open,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = close,
			TotalVolume = volume,
		}, CancellationToken);
	}

	private async Task SendLevel1(IMarketEmulator emu, SecurityId secId, DateTime time, decimal bid, decimal ask)
	{
		await emu.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = secId,
			LocalTime = time,
			ServerTime = time
		}
		.Add(Level1Fields.BestBidPrice, bid)
		.Add(Level1Fields.BestAskPrice, ask)
		.Add(Level1Fields.BestBidVolume, 100m)
		.Add(Level1Fields.BestAskVolume, 100m), CancellationToken);
	}

	private async Task SendOrderLog(IMarketEmulator emu, SecurityId secId, DateTime time, Sides side, decimal price, decimal volume)
	{
		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = secId,
			LocalTime = time,
			ServerTime = time,
			DataTypeEx = DataType.OrderLog,
			Side = side,
			OrderPrice = price,
			OrderVolume = volume,
			OrderState = OrderStates.Active
		}, CancellationToken);
	}

	/// <summary>
	/// Compare ALL fields of two ExecutionMessages and return differences.
	/// </summary>
	private static List<string> CompareExecutionMessages(ExecutionMessage v1, ExecutionMessage v2, string context)
	{
		var diffs = new List<string>();

		// Security & Portfolio
		if (v1.SecurityId != v2.SecurityId)
			diffs.Add($"{context}: SecurityId V1={v1.SecurityId}, V2={v2.SecurityId}");
		if (v1.PortfolioName != v2.PortfolioName)
			diffs.Add($"{context}: PortfolioName V1={v1.PortfolioName}, V2={v2.PortfolioName}");
		if (v1.ClientCode != v2.ClientCode)
			diffs.Add($"{context}: ClientCode V1={v1.ClientCode}, V2={v2.ClientCode}");
		if (v1.BrokerCode != v2.BrokerCode)
			diffs.Add($"{context}: BrokerCode V1={v1.BrokerCode}, V2={v2.BrokerCode}");
		if (v1.DepoName != v2.DepoName)
			diffs.Add($"{context}: DepoName V1={v1.DepoName}, V2={v2.DepoName}");

		// Transaction IDs
		if (v1.TransactionId != v2.TransactionId)
			diffs.Add($"{context}: TransactionId V1={v1.TransactionId}, V2={v2.TransactionId}");
		if (v1.OriginalTransactionId != v2.OriginalTransactionId)
			diffs.Add($"{context}: OriginalTransactionId V1={v1.OriginalTransactionId}, V2={v2.OriginalTransactionId}");

		// Skip OrderId/TradeId comparison - generated IDs may differ
		if (v1.OrderStringId != v2.OrderStringId)
			diffs.Add($"{context}: OrderStringId V1={v1.OrderStringId}, V2={v2.OrderStringId}");
		if (v1.OrderBoardId != v2.OrderBoardId)
			diffs.Add($"{context}: OrderBoardId V1={v1.OrderBoardId}, V2={v2.OrderBoardId}");

		// Order info flag
		if (v1.HasOrderInfo != v2.HasOrderInfo)
			diffs.Add($"{context}: HasOrderInfo V1={v1.HasOrderInfo}, V2={v2.HasOrderInfo}");
		if (v1.IsCancellation != v2.IsCancellation)
			diffs.Add($"{context}: IsCancellation V1={v1.IsCancellation}, V2={v2.IsCancellation}");

		// Order details
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
		if (v1.OrderStatus != v2.OrderStatus)
			diffs.Add($"{context}: OrderStatus V1={v1.OrderStatus}, V2={v2.OrderStatus}");
		if (v1.OrderState != v2.OrderState)
			diffs.Add($"{context}: OrderState V1={v1.OrderState}, V2={v2.OrderState}");
		if (v1.TimeInForce != v2.TimeInForce)
			diffs.Add($"{context}: TimeInForce V1={v1.TimeInForce}, V2={v2.TimeInForce}");
		if (v1.ExpiryDate != v2.ExpiryDate)
			diffs.Add($"{context}: ExpiryDate V1={v1.ExpiryDate}, V2={v2.ExpiryDate}");

		// Trade details
		if (v1.TradeStringId != v2.TradeStringId)
			diffs.Add($"{context}: TradeStringId V1={v1.TradeStringId}, V2={v2.TradeStringId}");
		if (v1.TradePrice != v2.TradePrice)
			diffs.Add($"{context}: TradePrice V1={v1.TradePrice}, V2={v2.TradePrice}");
		if (v1.TradeVolume != v2.TradeVolume)
			diffs.Add($"{context}: TradeVolume V1={v1.TradeVolume}, V2={v2.TradeVolume}");
		if (v1.TradeStatus != v2.TradeStatus)
			diffs.Add($"{context}: TradeStatus V1={v1.TradeStatus}, V2={v2.TradeStatus}");
		if (v1.OriginSide != v2.OriginSide)
			diffs.Add($"{context}: OriginSide V1={v1.OriginSide}, V2={v2.OriginSide}");
		if (v1.OpenInterest != v2.OpenInterest)
			diffs.Add($"{context}: OpenInterest V1={v1.OpenInterest}, V2={v2.OpenInterest}");

		// Comments
		if (v1.Comment != v2.Comment)
			diffs.Add($"{context}: Comment V1={v1.Comment}, V2={v2.Comment}");
		if (v1.SystemComment != v2.SystemComment)
			diffs.Add($"{context}: SystemComment V1={v1.SystemComment}, V2={v2.SystemComment}");

		// Flags
		if (v1.IsSystem != v2.IsSystem)
			diffs.Add($"{context}: IsSystem V1={v1.IsSystem}, V2={v2.IsSystem}");
		if (v1.IsUpTick != v2.IsUpTick)
			diffs.Add($"{context}: IsUpTick V1={v1.IsUpTick}, V2={v2.IsUpTick}");

		// Financial
		if (v1.Commission != v2.Commission)
			diffs.Add($"{context}: Commission V1={v1.Commission}, V2={v2.Commission}");
		if (v1.CommissionCurrency != v2.CommissionCurrency)
			diffs.Add($"{context}: CommissionCurrency V1={v1.CommissionCurrency}, V2={v2.CommissionCurrency}");
		if (v1.Slippage != v2.Slippage)
			diffs.Add($"{context}: Slippage V1={v1.Slippage}, V2={v2.Slippage}");

		// Latency
		if (v1.Latency != v2.Latency)
			diffs.Add($"{context}: Latency V1={v1.Latency}, V2={v2.Latency}");

		// User order ID
		if (v1.UserOrderId != v2.UserOrderId)
			diffs.Add($"{context}: UserOrderId V1={v1.UserOrderId}, V2={v2.UserOrderId}");

		// Error
		var err1 = v1.Error?.Message;
		var err2 = v2.Error?.Message;
		if (err1 != err2)
			diffs.Add($"{context}: Error V1={err1}, V2={err2}");

		// Condition type (if any)
		var cond1 = v1.Condition?.GetType().Name;
		var cond2 = v2.Condition?.GetType().Name;
		if (cond1 != cond2)
			diffs.Add($"{context}: Condition V1={cond1}, V2={cond2}");

		return diffs;
	}

	/// <summary>
	/// Compare ALL fields of two PositionChangeMessages and return differences.
	/// </summary>
	private static List<string> ComparePositionMessages(PositionChangeMessage v1, PositionChangeMessage v2, string context)
	{
		var diffs = new List<string>();

		// Basic identifiers
		if (v1.SecurityId != v2.SecurityId)
			diffs.Add($"{context}: SecurityId V1={v1.SecurityId}, V2={v2.SecurityId}");
		if (v1.PortfolioName != v2.PortfolioName)
			diffs.Add($"{context}: PortfolioName V1={v1.PortfolioName}, V2={v2.PortfolioName}");
		if (v1.ClientCode != v2.ClientCode)
			diffs.Add($"{context}: ClientCode V1={v1.ClientCode}, V2={v2.ClientCode}");
		if (v1.DepoName != v2.DepoName)
			diffs.Add($"{context}: DepoName V1={v1.DepoName}, V2={v2.DepoName}");
		if (v1.LimitType != v2.LimitType)
			diffs.Add($"{context}: LimitType V1={v1.LimitType}, V2={v2.LimitType}");
		if (v1.Description != v2.Description)
			diffs.Add($"{context}: Description V1={v1.Description}, V2={v2.Description}");
		if (v1.BoardCode != v2.BoardCode)
			diffs.Add($"{context}: BoardCode V1={v1.BoardCode}, V2={v2.BoardCode}");
		if (v1.StrategyId != v2.StrategyId)
			diffs.Add($"{context}: StrategyId V1={v1.StrategyId}, V2={v2.StrategyId}");
		if (v1.Side != v2.Side)
			diffs.Add($"{context}: Side V1={v1.Side}, V2={v2.Side}");

		// Compare ALL position change types in Changes dictionary
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
	/// Compare a single message from V1 and V2.
	/// </summary>
	private static List<string> CompareMessage(Message v1, Message v2, string context)
	{
		var diffs = new List<string>();

		// Check types match
		if (v1.GetType() != v2.GetType())
		{
			diffs.Add($"{context}: Type mismatch V1={v1.GetType().Name}, V2={v2.GetType().Name}");
			return diffs;
		}

		// Compare based on message type
		if (v1 is ExecutionMessage exec1 && v2 is ExecutionMessage exec2)
		{
			diffs.AddRange(CompareExecutionMessages(exec1, exec2, context));
		}
		else if (v1 is PositionChangeMessage pos1 && v2 is PositionChangeMessage pos2)
		{
			diffs.AddRange(ComparePositionMessages(pos1, pos2, context));
		}

		return diffs;
	}

	/// <summary>
	/// Full 1:1 comparison of message sequences from V1 and V2.
	/// </summary>
	private static List<string> Compare(List<Message> v1Messages, List<Message> v2Messages)
	{
		var diffs = new List<string>();

		if (v1Messages.Count != v2Messages.Count)
			diffs.Add($"Message count: V1={v1Messages.Count}, V2={v2Messages.Count}");

		var maxCount = Math.Max(v1Messages.Count, v2Messages.Count);
		for (int i = 0; i < maxCount; i++)
		{
			if (i >= v1Messages.Count)
			{
				diffs.Add($"[{i}]: V1=MISSING, V2={v2Messages[i].GetType().Name}");
				continue;
			}
			if (i >= v2Messages.Count)
			{
				diffs.Add($"[{i}]: V1={v1Messages[i].GetType().Name}, V2=MISSING");
				continue;
			}

			diffs.AddRange(CompareMessage(v1Messages[i], v2Messages[i], $"[{i}]"));
		}

		return diffs;
	}

	private void AssertEqual(List<Message> resV1, List<Message> resV2, string testName)
	{
		var diffs = Compare(resV1, resV2);

		Console.WriteLine($"\n=== {testName} ===");
		Console.WriteLine($"V1 messages: {resV1.Count}, V2 messages: {resV2.Count}");
		Console.WriteLine($"Differences: {diffs.Count}");

		if (diffs.Count > 0)
		{
			Console.WriteLine("\nDifferences:");
			foreach (var diff in diffs.Take(50))
				Console.WriteLine($"  {diff}");
			if (diffs.Count > 50)
				Console.WriteLine($"  ... and {diffs.Count - 50} more");
		}

		AreEqual(0, diffs.Count, $"{testName}: Found {diffs.Count} differences between V1 and V2");
	}

	#region Full Message Comparison Tests

	[TestMethod]
	public async Task FullComparison_LimitBuyExecute()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitBuyExecute");
	}

	[TestMethod]
	public async Task FullComparison_LimitBuyInQueue()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 99m,  // Below bid, won't match
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "LimitBuyInQueue");
	}

	[TestMethod]
	public async Task FullComparison_BuyThenSell()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		// Buy
		var buyOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 100,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);

		now = now.AddSeconds(1);

		// Update book
		await SendBook(emuV1, secId, now, 110m, 111m);
		await SendBook(emuV2, secId, now, 110m, 111m);

		// Sell to close position
		var sellOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			Side = Sides.Sell,
			Price = 110m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "BuyThenSell");
	}

	[TestMethod]
	public async Task FullComparison_IOC()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 12345,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,  // IOC
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "IOC");
	}

	[TestMethod]
	public async Task FullComparison_FOK()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 12345,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,  // FOK
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "FOK");
	}

	[TestMethod]
	public async Task FullComparison_MarketOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 12345,
			Side = Sides.Buy,
			Price = 0,
			Volume = 10m,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "MarketOrder");
	}

	[TestMethod]
	public async Task FullComparison_CancelOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		// Register order that stays in queue
		var regTrId = 100L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = regTrId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Cancel it
		var cancelMsg = new OrderCancelMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			OriginalTransactionId = regTrId,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "CancelOrder");
	}

	#endregion

	#region Data Source Tests

	[TestMethod]
	public async Task Feature_TickExecution()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendTick(emuV1, secId, now, 105m);
		await SendTick(emuV2, secId, now, 105m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 105m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "TickExecution");
	}

	[TestMethod]
	public async Task Feature_CandleExecution()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		// Candle: O=100, H=105, L=95, C=104
		await SendCandle(emuV1, secId, now, 100m, 105m, 95m, 104m);
		await SendCandle(emuV2, secId, now, 100m, 105m, 95m, 104m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 104m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "CandleExecution");
	}

	[TestMethod]
	public async Task Feature_Level1Execution()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendLevel1(emuV1, secId, now, 104m, 105m);
		await SendLevel1(emuV2, secId, now, 104m, 105m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 105m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "Level1Execution");
	}

	[TestMethod]
	public async Task Feature_OrderLogExecution()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		// Add sell order to book via order log
		await SendOrderLog(emuV1, secId, now, Sides.Sell, 105m, 100m);
		await SendOrderLog(emuV2, secId, now, Sides.Sell, 105m, 100m);

		resV1.Clear();
		resV2.Clear();

		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 105m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "OrderLogExecution");
	}

	#endregion

	#region Order Expiration Tests

	[TestMethod]
	public async Task Feature_OrderExpiration()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		resV1.Clear();
		resV2.Clear();

		// Order with expiry in the past
		var expiry = now.AddMinutes(-1);
		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "OrderExpiration");
	}

	[TestMethod]
	public async Task Feature_OrderExpirationOnTime()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		// Order expires in future
		var expiry = now.AddMinutes(1);
		var trId = 12345L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = trId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Time passes - send tick after expiry
		var laterTime = now.AddMinutes(2);
		await SendTick(emuV1, secId, laterTime, 100m);
		await SendTick(emuV2, secId, laterTime, 100m);

		AssertEqual(resV1, resV2, "OrderExpirationOnTime");
	}

	#endregion

	#region Order Operations Tests

	[TestMethod]
	public async Task Feature_CancelOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		var regTrId = 100L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = regTrId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Cancel the order
		var cancelMsg = new OrderCancelMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			OriginalTransactionId = regTrId,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(cancelMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "Feature_CancelOrder");
	}

	[TestMethod]
	public async Task Feature_ReplaceOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		var regTrId = 100L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = regTrId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Replace order - change price
		var replaceMsg = new OrderReplaceMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			OriginalTransactionId = regTrId,
			Price = 98m,
			Volume = 10m,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(replaceMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(replaceMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "ReplaceOrder");
	}

	[TestMethod]
	public async Task Feature_ReplaceOrderAndMatch()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		var regTrId = 100L;
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = regTrId,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Replace to crossing price - should match
		var replaceMsg = new OrderReplaceMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			OriginalTransactionId = regTrId,
			Price = 101m,
			Volume = 10m,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(replaceMsg.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(replaceMsg.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "ReplaceOrderAndMatch");
	}

	#endregion

	#region OrderGroup Tests

	[TestMethod]
	public async Task Feature_OrderGroupCancelOrders()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		// Register two orders
		var order1 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 100,
			Side = Sides.Buy,
			Price = 99m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		var order2 = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			Side = Sides.Buy,
			Price = 98m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order1.TypedClone(), CancellationToken);
		await emuV1.SendInMessageAsync(order2.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order2.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var groupCancel = new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = 200,
			Mode = OrderGroupCancelModes.CancelOrders,
		};

		await emuV1.SendInMessageAsync(groupCancel.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(groupCancel.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "OrderGroupCancelOrders");
	}

	[TestMethod]
	public async Task Feature_OrderGroupClosePositions()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendBook(emuV1, secId, now, 100m, 101m);
		await SendBook(emuV2, secId, now, 100m, 101m);

		// Build position
		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 100,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		resV1.Clear();
		resV2.Clear();

		var closePositions = new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = 200,
			Mode = OrderGroupCancelModes.ClosePositions,
		};

		await emuV1.SendInMessageAsync(closePositions.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(closePositions.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "OrderGroupClosePositions");
	}

	#endregion

	#region Market Order on Alternative Data

	[TestMethod]
	public async Task Feature_MarketOrderOnTick()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendTick(emuV1, secId, now, 105m);
		await SendTick(emuV2, secId, now, 105m);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 12345,
			Side = Sides.Buy,
			Price = 0,
			Volume = 10m,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "MarketOrderOnTick");
	}

	[TestMethod]
	public async Task Feature_MarketOrderOnOrderLog()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
		var now = DateTime.UtcNow;

		await InitMoney(emuV1, now);
		await InitMoney(emuV2, now);

		await SendOrderLog(emuV1, secId, now, Sides.Sell, 105m, 100m);
		await SendOrderLog(emuV2, secId, now, Sides.Sell, 105m, 100m);

		resV1.Clear();
		resV2.Clear();

		var order = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 12345,
			Side = Sides.Buy,
			Price = 0,
			Volume = 10m,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);

		AssertEqual(resV1, resV2, "MarketOrderOnOrderLog");
	}

	#endregion

	#region Summary Test

	[TestMethod]
	public async Task FeatureSummary()
	{
		var secId = Helper.CreateSecurityId();
		var now = DateTime.UtcNow;
		var results = new List<(string feature, int diffCount)>();

		// Test each feature and count differences
		async Task<int> TestFeature(Func<IMarketEmulator, IMarketEmulator, List<Message>, List<Message>, SecurityId, DateTime, Task> test)
		{
			try
			{
				var (emuV1, resV1, emuV2, resV2) = CreateBothEmulators(secId);
				await InitMoney(emuV1, now);
				await InitMoney(emuV2, now);
				resV1.Clear();
				resV2.Clear();
				await test(emuV1, emuV2, resV1, resV2, secId, now);
				return Compare(resV1, resV2).Count;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Test error: {ex.Message}");
				return -1;
			}
		}

		// Tick execution
		results.Add(("Tick Execution", await TestFeature(async (e1, e2, r1, r2, s, t) =>
		{
			await SendTick(e1, s, t, 105m);
			await SendTick(e2, s, t, 105m);
			r1.Clear();
			r2.Clear();
			var order = new OrderRegisterMessage
			{
				SecurityId = s, LocalTime = t, TransactionId = 12345,
				Side = Sides.Buy, Price = 105m, Volume = 10m,
				OrderType = OrderTypes.Limit, PortfolioName = _pfName,
			};
			await e1.SendInMessageAsync(order.TypedClone(), CancellationToken);
			await e2.SendInMessageAsync(order.TypedClone(), CancellationToken);
		})));

		// Candle execution
		results.Add(("Candle Execution", await TestFeature(async (e1, e2, r1, r2, s, t) =>
		{
			await SendCandle(e1, s, t, 100m, 105m, 95m, 104m);
			await SendCandle(e2, s, t, 100m, 105m, 95m, 104m);
			r1.Clear();
			r2.Clear();
			var order = new OrderRegisterMessage
			{
				SecurityId = s, LocalTime = t, TransactionId = 12345,
				Side = Sides.Buy, Price = 104m, Volume = 10m,
				OrderType = OrderTypes.Limit, PortfolioName = _pfName,
			};
			await e1.SendInMessageAsync(order.TypedClone(), CancellationToken);
			await e2.SendInMessageAsync(order.TypedClone(), CancellationToken);
		})));

		// Level1 execution
		results.Add(("Level1 Execution", await TestFeature(async (e1, e2, r1, r2, s, t) =>
		{
			await SendLevel1(e1, s, t, 104m, 105m);
			await SendLevel1(e2, s, t, 104m, 105m);
			r1.Clear();
			r2.Clear();
			var order = new OrderRegisterMessage
			{
				SecurityId = s, LocalTime = t, TransactionId = 12345,
				Side = Sides.Buy, Price = 105m, Volume = 10m,
				OrderType = OrderTypes.Limit, PortfolioName = _pfName,
			};
			await e1.SendInMessageAsync(order.TypedClone(), CancellationToken);
			await e2.SendInMessageAsync(order.TypedClone(), CancellationToken);
		})));

		// OrderLog execution
		results.Add(("OrderLog Execution", await TestFeature(async (e1, e2, r1, r2, s, t) =>
		{
			await SendOrderLog(e1, s, t, Sides.Sell, 105m, 100m);
			await SendOrderLog(e2, s, t, Sides.Sell, 105m, 100m);
			r1.Clear();
			r2.Clear();
			var order = new OrderRegisterMessage
			{
				SecurityId = s, LocalTime = t, TransactionId = 12345,
				Side = Sides.Buy, Price = 105m, Volume = 10m,
				OrderType = OrderTypes.Limit, PortfolioName = _pfName,
			};
			await e1.SendInMessageAsync(order.TypedClone(), CancellationToken);
			await e2.SendInMessageAsync(order.TypedClone(), CancellationToken);
		})));

		// Order book execution
		results.Add(("OrderBook Execution", await TestFeature(async (e1, e2, r1, r2, s, t) =>
		{
			await SendBook(e1, s, t, 100m, 101m);
			await SendBook(e2, s, t, 100m, 101m);
			r1.Clear();
			r2.Clear();
			var order = new OrderRegisterMessage
			{
				SecurityId = s, LocalTime = t, TransactionId = 12345,
				Side = Sides.Buy, Price = 101m, Volume = 10m,
				OrderType = OrderTypes.Limit, PortfolioName = _pfName,
			};
			await e1.SendInMessageAsync(order.TypedClone(), CancellationToken);
			await e2.SendInMessageAsync(order.TypedClone(), CancellationToken);
		})));

		// Print summary
		Console.WriteLine("\n========== FEATURE COMPARISON SUMMARY ==========\n");
		Console.WriteLine($"{"Feature",-25} {"Differences",-15} {"Status",-10}");
		Console.WriteLine(new string('-', 50));

		var failedFeatures = 0;
		foreach (var (feature, diffCount) in results)
		{
			var status = diffCount switch
			{
				0 => "OK",
				-1 => "ERROR",
				_ => "DIFF"
			};

			if (diffCount != 0) failedFeatures++;

			Console.WriteLine($"{feature,-25} {diffCount,-15} {status,-10}");
		}

		Console.WriteLine(new string('-', 50));
		Console.WriteLine($"\nTotal features with differences: {failedFeatures}");
		Console.WriteLine("\n================================================\n");

		// Fail if there are differences
		AreEqual(0, failedFeatures, $"Found {failedFeatures} features with differences between V1 and V2");
	}

	#endregion
}
