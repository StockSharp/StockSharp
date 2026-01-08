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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6); // Short period for faster test

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
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
		var security2 = new Security { Id = Paths.HistoryDefaultSecurity2 };

		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security1, security2]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(14);

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
			Connector = connector,
			Security = security1,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var strategy2 = new SmaStrategy
		{
			Connector = connector,
			Security = security2,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
			{
				strategy1.Stop();
				strategy2.Stop();
				tcs.TrySetResult(true);
			}
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

	/// <summary>
	/// Tests that all event times are monotonically increasing during backtesting.
	/// Time should always increase - both within same event type and across different event types.
	/// </summary>
	[TestMethod]
	public async Task BacktestEventTimesAreMonotonicallyIncreasing()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

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
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		// Track all event times globally
		var allEventTimes = new List<(DateTime time, string eventType)>();
		var syncLock = new object();

		// Track times per event type
		DateTime? lastOrderTime = null;
		DateTime? lastTradeTime = null;
		DateTime? lastPnLTime = null;
		DateTime? lastPositionTime = null;

		var orderTimeErrors = new List<string>();
		var tradeTimeErrors = new List<string>();
		var pnlTimeErrors = new List<string>();
		var positionTimeErrors = new List<string>();
		var globalTimeErrors = new List<string>();

		strategy.OrderReceived += (sub, order) =>
		{
			var time = order.Time;
			lock (syncLock)
			{
				// Check within order events
				if (lastOrderTime.HasValue && time < lastOrderTime.Value)
				{
					orderTimeErrors.Add($"Order time decreased: {lastOrderTime.Value} -> {time}");
				}
				lastOrderTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Order time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Order"));
			}
		};

		strategy.OwnTradeReceived += (sub, trade) =>
		{
			var time = trade.Trade.ServerTime;
			lock (syncLock)
			{
				// Check within trade events
				if (lastTradeTime.HasValue && time < lastTradeTime.Value)
				{
					tradeTimeErrors.Add($"Trade time decreased: {lastTradeTime.Value} -> {time}");
				}
				lastTradeTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Trade time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Trade"));
			}
		};

		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			lock (syncLock)
			{
				// Check within PnL events
				if (lastPnLTime.HasValue && time < lastPnLTime.Value)
				{
					pnlTimeErrors.Add($"PnL time decreased: {lastPnLTime.Value} -> {time}");
				}
				lastPnLTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"PnL time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "PnL"));
			}
		};

		strategy.PositionReceived += (s, pos) =>
		{
			var time = pos.ServerTime != default ? pos.ServerTime : DateTime.MinValue;
			if (time == DateTime.MinValue)
				return;

			lock (syncLock)
			{
				// Check within position events
				if (lastPositionTime.HasValue && time < lastPositionTime.Value)
				{
					positionTimeErrors.Add($"Position time decreased: {lastPositionTime.Value} -> {time}");
				}
				lastPositionTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Position time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Position"));
			}
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

		// Verify we received some events
		IsTrue(allEventTimes.Count > 0, "Expected to receive some events");

		// Report all errors
		var allErrors = new List<string>();
		allErrors.AddRange(orderTimeErrors);
		allErrors.AddRange(tradeTimeErrors);
		allErrors.AddRange(pnlTimeErrors);
		allErrors.AddRange(positionTimeErrors);
		allErrors.AddRange(globalTimeErrors);

		if (allErrors.Count > 0)
		{
			Fail($"Time ordering violations detected:\n{string.Join("\n", allErrors.Take(20))}");
		}
	}
}