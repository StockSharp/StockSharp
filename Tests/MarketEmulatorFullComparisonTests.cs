namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Full comparison tests between MarketEmulator (V1) and MarketEmulator2 (V2).
/// These tests verify that both emulators produce IDENTICAL output messages.
/// </summary>
[TestClass]
public class MarketEmulatorFullComparisonTests : BaseTestClass
{
	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;
	private static readonly IdGenerator _idGenerator = new IncrementalIdGenerator();

	private static (IMarketEmulator emu, List<Message> results) CreateEmulator(bool useV2, SecurityId secId)
	{
		var securities = new[] { new Security { Id = secId.ToStringId() } };
		var secProvider = new CollectionSecurityProvider(securities);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchProvider = new InMemoryExchangeInfoProvider();

		// Use fixed ID generators to eliminate randomness
		var idGen = new IncrementalIdGenerator();

		IMarketEmulator emu;

		if (useV2)
		{
			var emu2 = new MarketEmulator2(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true };
			emu2.OrderIdGenerator = new IncrementalIdGenerator();
			emu2.TradeIdGenerator = new IncrementalIdGenerator();
			emu = emu2;
		}
		else
		{
			var emu1 = new MarketEmulator(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true };
			// Disable randomness
			emu1.Settings.Failing = 0;
			emu1.Settings.Latency = TimeSpan.Zero;
			emu = emu1;
		}

		var results = new List<Message>();
		emu.NewOutMessage += results.Add;

		return (emu, results);
	}

	/// <summary>
	/// Full comparison: run same scenario through both emulators
	/// and compare ALL output messages.
	/// </summary>
	[TestMethod]
	public async Task FullMessageComparison_BuyAndSell()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		// Send order book
		var book = new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		};

		await emuV1.SendInMessageAsync(book.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(book.TypedClone(), CancellationToken);

		// Buy order that executes
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
		book = new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(102m, 100m)],
			Asks = [new(103m, 100m)]
		};

		await emuV1.SendInMessageAsync(book.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(book.TypedClone(), CancellationToken);

		// Sell order - should generate RealizedPnL
		var sellOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			Side = Sides.Sell,
			Price = 102m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);

		// Compare message counts first
		AreEqual(resV1.Count, resV2.Count, $"Message count mismatch: V1={resV1.Count}, V2={resV2.Count}");

		// Compare each message
		for (var i = 0; i < resV1.Count; i++)
		{
			var msgV1 = resV1[i];
			var msgV2 = resV2[i];

			AreEqual(msgV1.Type, msgV2.Type, $"Message[{i}] type mismatch: V1={msgV1.Type}, V2={msgV2.Type}");

			CompareMessage(msgV1, msgV2, i);
		}
	}

	/// <summary>
	/// Test that RealizedPnL is calculated correctly after closing a position.
	/// </summary>
	[TestMethod]
	public async Task RealizedPnL_AfterClosingPosition()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		// Send order book
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		// Buy at 101
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		now = now.AddSeconds(1);

		// Update book - price went up
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(110m, 100m)],
			Asks = [new(111m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(110m, 100m)],
			Asks = [new(111m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Sell at 110 - profit of (110-101)*10 = 90
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Sell,
			Price = 110m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 2,
			Side = Sides.Sell,
			Price = 110m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		// Find RealizedPnL in V1 output
		var pnlMsgV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		var pnlMsgV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		IsNotNull(pnlMsgV1, "V1 should produce RealizedPnL message");
		IsNotNull(pnlMsgV2, "V2 should produce RealizedPnL message");

		var realizedV1 = (decimal)pnlMsgV1.Changes[PositionChangeTypes.RealizedPnL];
		var realizedV2 = (decimal)pnlMsgV2.Changes[PositionChangeTypes.RealizedPnL];

		AreEqual(realizedV1, realizedV2, $"RealizedPnL mismatch: V1={realizedV1}, V2={realizedV2}");
		IsTrue(realizedV1 > 0, $"RealizedPnL should be positive (profit), got {realizedV1}");
	}

	/// <summary>
	/// Test that position CurrentValue and AveragePrice are tracked correctly.
	/// </summary>
	[TestMethod]
	public async Task PositionTracking_CurrentValueAndAveragePrice()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		// Book
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Buy 10 at 101
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		// Check position in V1
		var posV1 = resV1.OfType<PositionChangeMessage>()
			.LastOrDefault(m => m.SecurityId != SecurityId.Money && m.Changes.ContainsKey(PositionChangeTypes.CurrentValue));

		var posV2 = resV2.OfType<PositionChangeMessage>()
			.LastOrDefault(m => m.SecurityId != SecurityId.Money && m.Changes.ContainsKey(PositionChangeTypes.CurrentValue));

		// V1 might send position change with CurrentValue
		// V2 should also send it
		if (posV1 != null)
		{
			IsNotNull(posV2, "V2 should produce position CurrentValue message if V1 does");

			var curValV1 = (decimal)posV1.Changes[PositionChangeTypes.CurrentValue];
			var curValV2 = (decimal)posV2.Changes[PositionChangeTypes.CurrentValue];

			AreEqual(curValV1, curValV2, $"Position CurrentValue mismatch: V1={curValV1}, V2={curValV2}");

			if (posV1.Changes.TryGetValue(PositionChangeTypes.AveragePrice, out var avgV1Obj))
			{
				IsTrue(posV2.Changes.ContainsKey(PositionChangeTypes.AveragePrice),
					"V2 should produce AveragePrice if V1 does");

				var avgV1 = (decimal)avgV1Obj;
				var avgV2 = (decimal)posV2.Changes[PositionChangeTypes.AveragePrice];

				AreEqual(avgV1, avgV2, $"AveragePrice mismatch: V1={avgV1}, V2={avgV2}");
			}
		}
	}

	/// <summary>
	/// Test Commission calculation matches between V1 and V2.
	/// </summary>
	[TestMethod]
	public async Task Commission_MatchesBetweenVersions()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		// Book
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Execute order
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		// Find Commission in portfolio messages
		var commV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.Commission));

		var commV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.Commission));

		// If V1 reports commission, V2 should too
		if (commV1 != null)
		{
			IsNotNull(commV2, "V2 should produce Commission if V1 does");

			var commValV1 = (decimal)commV1.Changes[PositionChangeTypes.Commission];
			var commValV2 = (decimal)commV2.Changes[PositionChangeTypes.Commission];

			AreEqual(commValV1, commValV2, $"Commission mismatch: V1={commValV1}, V2={commValV2}");
		}
	}

	/// <summary>
	/// Test UnrealizedPnL calculation.
	/// </summary>
	[TestMethod]
	public async Task UnrealizedPnL_WithOpenPosition()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		// Book at 100/101
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		// Buy at 101
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		now = now.AddSeconds(1);
		resV1.Clear();
		resV2.Clear();

		// Price goes up to 110/111 - should have UnrealizedPnL
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(110m, 100m)],
			Asks = [new(111m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(110m, 100m)],
			Asks = [new(111m, 100m)]
		}, CancellationToken);

		// Check UnrealizedPnL
		var unrealV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.UnrealizedPnL));

		var unrealV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.UnrealizedPnL));

		if (unrealV1 != null)
		{
			IsNotNull(unrealV2, "V2 should produce UnrealizedPnL if V1 does");

			var unrealValV1 = (decimal)unrealV1.Changes[PositionChangeTypes.UnrealizedPnL];
			var unrealValV2 = (decimal)unrealV2.Changes[PositionChangeTypes.UnrealizedPnL];

			AreEqual(unrealValV1, unrealValV2, $"UnrealizedPnL mismatch: V1={unrealValV1}, V2={unrealValV2}");
		}
	}

	/// <summary>
	/// Test BlockedValue (margin) tracking.
	/// </summary>
	[TestMethod]
	public async Task BlockedValue_WithActiveOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var now = DateTime.UtcNow;

		// Set initial money
		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		// Book
		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		// Place order that stays in book (buy at bid)
		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100m, // at bid, won't execute
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		}, CancellationToken);

		// Check BlockedValue
		var blockedV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.BlockedValue));

		var blockedV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.BlockedValue));

		if (blockedV1 != null)
		{
			IsNotNull(blockedV2, "V2 should produce BlockedValue if V1 does");

			var blockedValV1 = (decimal)blockedV1.Changes[PositionChangeTypes.BlockedValue];
			var blockedValV2 = (decimal)blockedV2.Changes[PositionChangeTypes.BlockedValue];

			AreEqual(blockedValV1, blockedValV2, $"BlockedValue mismatch: V1={blockedValV1}, V2={blockedValV2}");
		}
	}

	/// <summary>
	/// Test with historical candle data - full scenario comparison.
	/// </summary>
	[TestMethod]
	public async Task FullScenario_WithCandles()
	{
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var (emuV1, resV1) = CreateEmulator(false, secId);
		var (emuV2, resV2) = CreateEmulator(true, secId);

		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);
		var candleStorage = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1));

		var candles = await candleStorage.LoadAsync(Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1))
			.Take(100)
			.ToArrayAsync(CancellationToken);

		if (candles.Length == 0)
		{
			// Skip if no history data available
			return;
		}

		// Set initial money
		var initTime = (DateTime)candles[0].OpenTime;
		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = initTime,
			ServerTime = initTime,
		}.Add(PositionChangeTypes.BeginValue, 10000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = initTime,
			ServerTime = initTime,
		}.Add(PositionChangeTypes.BeginValue, 10000000m), CancellationToken);

		long trId = 1;
		var position = 0m;

		// Simple strategy: buy when price goes up, sell when goes down
		decimal? prevClose = null;

		foreach (var candle in candles)
		{
			var time = (DateTime)candle.OpenTime;

			// Send candle to both
			await emuV1.SendInMessageAsync(candle.TypedClone(), CancellationToken);
			await emuV2.SendInMessageAsync(candle.TypedClone(), CancellationToken);

			if (prevClose != null)
			{
				if (candle.ClosePrice > prevClose && position <= 0)
				{
					// Buy signal
					var order = new OrderRegisterMessage
					{
						SecurityId = secId,
						LocalTime = time,
						TransactionId = trId++,
						Side = Sides.Buy,
						Price = candle.ClosePrice,
						Volume = 1,
						OrderType = OrderTypes.Limit,
						PortfolioName = _pfName,
					};

					await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
					await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);
					position++;
				}
				else if (candle.ClosePrice < prevClose && position >= 0)
				{
					// Sell signal
					var order = new OrderRegisterMessage
					{
						SecurityId = secId,
						LocalTime = time,
						TransactionId = trId++,
						Side = Sides.Sell,
						Price = candle.ClosePrice,
						Volume = 1,
						OrderType = OrderTypes.Limit,
						PortfolioName = _pfName,
					};

					await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
					await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);
					position--;
				}
			}

			prevClose = candle.ClosePrice;
		}

		// Compare all messages
		AreEqual(resV1.Count, resV2.Count, $"Total message count mismatch: V1={resV1.Count}, V2={resV2.Count}");

		var mismatches = new List<string>();

		for (var i = 0; i < resV1.Count; i++)
		{
			var msgV1 = resV1[i];
			var msgV2 = resV2[i];

			if (msgV1.Type != msgV2.Type)
			{
				mismatches.Add($"[{i}] Type: V1={msgV1.Type}, V2={msgV2.Type}");
				continue;
			}

			try
			{
				CompareMessage(msgV1, msgV2, i);
			}
			catch (Exception ex)
			{
				mismatches.Add($"[{i}] {msgV1.Type}: {ex.Message}");
			}
		}

		if (mismatches.Count > 0)
		{
			Fail($"Found {mismatches.Count} mismatches:\n" + string.Join("\n", mismatches.Take(20)));
		}
	}

	private void CompareMessage(Message msgV1, Message msgV2, int index)
	{
		switch (msgV1)
		{
			case ExecutionMessage execV1:
				CompareExecution(execV1, (ExecutionMessage)msgV2, index);
				break;

			case PositionChangeMessage posV1:
				ComparePositionChange(posV1, (PositionChangeMessage)msgV2, index);
				break;

			case QuoteChangeMessage quoteV1:
				CompareQuoteChange(quoteV1, (QuoteChangeMessage)msgV2, index);
				break;
		}
	}

	private void CompareExecution(ExecutionMessage v1, ExecutionMessage v2, int index)
	{
		AreEqual(v1.SecurityId, v2.SecurityId, $"[{index}] Exec.SecurityId");
		AreEqual(v1.OriginalTransactionId, v2.OriginalTransactionId, $"[{index}] Exec.OriginalTransactionId");
		AreEqual(v1.OrderState, v2.OrderState, $"[{index}] Exec.OrderState");
		AreEqual(v1.Balance, v2.Balance, $"[{index}] Exec.Balance");
		AreEqual(v1.OrderVolume, v2.OrderVolume, $"[{index}] Exec.OrderVolume");
		AreEqual(v1.TradePrice, v2.TradePrice, $"[{index}] Exec.TradePrice");
		AreEqual(v1.TradeVolume, v2.TradeVolume, $"[{index}] Exec.TradeVolume");
		AreEqual(v1.Side, v2.Side, $"[{index}] Exec.Side");
		AreEqual(v1.OrderId, v2.OrderId, $"[{index}] Exec.OrderId");
		AreEqual(v1.TradeId, v2.TradeId, $"[{index}] Exec.TradeId");
	}

	private void ComparePositionChange(PositionChangeMessage v1, PositionChangeMessage v2, int index)
	{
		AreEqual(v1.SecurityId, v2.SecurityId, $"[{index}] Pos.SecurityId");
		AreEqual(v1.PortfolioName, v2.PortfolioName, $"[{index}] Pos.PortfolioName");

		foreach (var change in v1.Changes)
		{
			IsTrue(v2.Changes.ContainsKey(change.Key),
				$"[{index}] Pos.Changes missing key {change.Key} in V2");

			if (v2.Changes.TryGetValue(change.Key, out var v2Value))
			{
				AreEqual(change.Value, v2Value,
					$"[{index}] Pos.Changes[{change.Key}]: V1={change.Value}, V2={v2Value}");
			}
		}

		// Check V2 doesn't have extra keys
		foreach (var change in v2.Changes)
		{
			IsTrue(v1.Changes.ContainsKey(change.Key),
				$"[{index}] Pos.Changes has extra key {change.Key} in V2 (value={change.Value})");
		}
	}

	private void CompareQuoteChange(QuoteChangeMessage v1, QuoteChangeMessage v2, int index)
	{
		AreEqual(v1.SecurityId, v2.SecurityId, $"[{index}] Quote.SecurityId");
		AreEqual(v1.Bids.Length, v2.Bids.Length, $"[{index}] Quote.Bids.Length");
		AreEqual(v1.Asks.Length, v2.Asks.Length, $"[{index}] Quote.Asks.Length");
	}
}
