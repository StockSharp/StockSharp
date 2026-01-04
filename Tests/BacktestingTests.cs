namespace StockSharp.Tests;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Testing;
using StockSharp.Designer;

/// <summary>
/// Tests for backtesting functionality using <see cref="HistoryEmulationConnector"/> with <see cref="SmaStrategy"/>.
/// </summary>
[TestClass]
public class BacktestingTests : BaseTestClass
{
	private static Security CreateTestSecurity()
	{
		return new() { Id = Paths.HistoryDefaultSecurity };
	}

	private static Portfolio CreateTestPortfolio()
	{
		return Portfolio.CreateSimulator();
	}

	private static IStorageRegistry GetHistoryStorage()
	{
		var fs = Helper.FileSystem;
		return fs.GetStorage(Paths.HistoryDataPath);
	}

	/// <summary>
	/// Tests that orders are generated during backtesting when SMA crossover occurs.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesOrders()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var ordersReceived = new List<Order>();
		strategy.OrderReceived += (sub, order) =>
		{
			ordersReceived.Add(order);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that orders were generated
		IsTrue(ordersReceived.Count > 0, "Expected at least one order to be generated");
	}

	/// <summary>
	/// Tests that trades are executed during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesTrades()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tradesReceived = new List<MyTrade>();
		strategy.OwnTradeReceived += (sub, trade) =>
		{
			tradesReceived.Add(trade);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that trades were executed
		IsTrue(tradesReceived.Count > 0, "Expected at least one trade to be executed");
	}

	/// <summary>
	/// Tests that PnL changes occur during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesPnLChanges()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var pnlChanges = new List<decimal>();
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			pnlChanges.Add(realized + (unrealized ?? 0));
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// After trades, PnL should have been tracked
		// Note: PnL might be 0 if no trades, but if trades occurred, we expect PnL events
		if (strategy.MyTrades.Any())
		{
			IsTrue(pnlChanges.Count > 0, "Expected PnL changes when trades occurred");
		}
	}

	/// <summary>
	/// Tests that position changes occur during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesPositionChanges()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var positionChanges = new List<decimal>();
		strategy.PositionReceived += (s, pos) =>
		{
			positionChanges.Add(pos.CurrentValue ?? 0);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// If trades occurred, position should have changed
		if (strategy.MyTrades.Any())
		{
			IsTrue(positionChanges.Count > 0, "Expected position changes when trades occurred");
		}
	}

	/// <summary>
	/// Tests that statistics are tracked during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestTracksStatistics()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify statistics manager is populated
		var statisticManager = strategy.StatisticManager;
		IsNotNull(statisticManager, "StatisticManager should not be null");

		// Check that some statistics are available
		var parameters = statisticManager.Parameters.ToList();
		IsTrue(parameters.Count > 0, "Expected statistics parameters to be tracked");
	}

	/// <summary>
	/// Tests that commission rules are applied during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestAppliesCommission()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		// Add commission rule - 0.1% per trade
		connector.EmulationAdapter.Settings.CommissionRules = [new CommissionTradeRule { Value = 0.001m }];

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var totalCommission = 0m;
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			if (commission.HasValue)
				totalCommission = commission.Value;
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// If trades occurred, commission should be tracked
		if (strategy.MyTrades.Any())
		{
			IsNotNull(strategy.Commission, "Commission should be tracked when trades occurred");
		}
	}

	/// <summary>
	/// Tests that emulation properly finishes and sets IsFinished flag.
	/// </summary>
	[TestMethod]
	public async Task BacktestSetsIsFinished()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 7); // Short period for faster test

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that IsFinished is set properly
		IsTrue(connector.IsFinished, "IsFinished should be true after successful completion");
	}

	/// <summary>
	/// Tests that progress events are raised during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestRaisesProgressEvents()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		var progressValues = new List<int>();
		connector.ProgressChanged += progress =>
		{
			progressValues.Add(progress);
		};

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that progress events were raised
		IsTrue(progressValues.Count > 0, "Expected progress events to be raised");

		// Progress should eventually reach or approach 100
		var maxProgress = progressValues.Max();
		IsTrue(maxProgress >= 50, "Expected progress to reach at least 50%");
	}

	/// <summary>
	/// Tests that strategy can be suspended and resumed during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestCanBeSuspendedAndResumed()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 30);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var startedTcs = new TaskCompletionSource<bool>();
		var suspendedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		connector.StateChanged2 += state =>
		{
			switch (state)
			{
				case ChannelStates.Started:
					startedTcs.TrySetResult(true);
					break;
				case ChannelStates.Suspended:
					suspendedTcs.TrySetResult(true);
					break;
				case ChannelStates.Stopped:
					stoppedTcs.TrySetResult(true);
					break;
			}
		};

		connector.Connected += () =>
		{
			strategy.Start();
			connector.Start();
		};

		connector.Connect();

		// Wait for started
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not start in time");
		}

		// Suspend
		connector.Suspend();

		// Wait for suspended
		await Task.WhenAny(suspendedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!suspendedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not suspend in time");
		}

		AreEqual(ChannelStates.Suspended, connector.State, "State should be Suspended");

		// Resume
		connector.Start();

		// Wait for completion
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not complete after resume in time");
		}

		connector.IsFinished.AssertTrue("IsFinished should be true after completion");
	}

	/// <summary>
	/// Tests that multiple securities can be backtested.
	/// </summary>
	[TestMethod]
	public async Task BacktestWithMultipleSecurities()
	{
		var security1 = CreateTestSecurity();
		var security2 = new Security
		{
			Id = "GAZP@TQBR",
			Code = "GAZP",
			Name = "Gazprom",
			PriceStep = 0.01m,
			VolumeStep = 1,
			Board = ExchangeBoard.MicexTqbr,
		};

		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security1, security2]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = new DateTime(2020, 4, 1);
		var stopTime = new DateTime(2020, 4, 15);

		using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		var strategy1 = new SmaStrategy
		{
			Security = security1,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var strategy2 = new SmaStrategy
		{
			Security = security2,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connected += () =>
		{
			strategy1.Start();
			strategy2.Start();
			connector.Start();
		};

		connector.Connect();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Both strategies should have completed
		strategy1.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 1 should be stopped");
		strategy2.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 2 should be stopped");
	}
}