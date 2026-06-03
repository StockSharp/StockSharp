namespace StockSharp.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Configuration;

[TestClass]
public class OptimizerPauseTests : BaseTestClass
{
	private class PauseSma : Strategy
	{
		private bool? _isShortLessThenLong;

		private readonly StrategyParam<int> _longSma;
		private readonly StrategyParam<int> _shortSma;
		private readonly StrategyParam<DataType> _candleType;

		public PauseSma()
		{
			_longSma = Param(nameof(LongSma), 80).SetCanOptimize(true).SetOptimize(50, 100, 5);
			_shortSma = Param(nameof(ShortSma), 30).SetCanOptimize(true).SetOptimize(20, 40, 1);
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetRequired();
		}

		public int LongSma { get => _longSma.Value; set => _longSma.Value = value; }
		public int ShortSma { get => _shortSma.Value; set => _shortSma.Value = value; }
		public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

		protected override void OnReseted()
		{
			base.OnReseted();
			_isShortLessThenLong = null;
		}

		protected override void OnStarted2(DateTime time)
		{
			base.OnStarted2(time);

			var subscription = new Subscription(CandleType, Security) { MarketData = { IsFinishedOnly = true } };
			var longSma = new SMA { Length = LongSma };
			var shortSma = new SMA { Length = ShortSma };
			SubscribeCandles(subscription).Bind(longSma, shortSma, OnProcess).Start();
		}

		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var isShortLessThenLong = shortValue < longValue;

			if (_isShortLessThenLong == null)
				_isShortLessThenLong = isShortLessThenLong;
			else if (_isShortLessThenLong != isShortLessThenLong)
			{
				if (isShortLessThenLong)
					SellLimit(candle.ClosePrice, Volume);
				else
					BuyLimit(candle.ClosePrice, Volume);

				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}

	[TestMethod]
	[DoNotParallelize] // heavy optimizer run: needs full CPU so pause/resume timing is reliable
	public async Task PauseHaltsBruteForce()
	{
		if (Paths.HistoryDataPath == null)
		{
			Console.WriteLine("SKIP: no history data");
			return;
		}

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var security = new Security { Id = Paths.HistoryDefaultSecurity, PriceStep = 0.01m };
		var portfolio = Portfolio.CreateSimulator();
		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		var start = Paths.HistoryBeginDate;
		var stop = Paths.HistoryBeginDate.AddDays(14); // each iteration takes a couple seconds

		var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
		optimizer.EmulationSettings.BatchSize = 8; // a full set of long iterations in-flight at pause
		optimizer.AdapterCache = new();

		var completed = 0;
		optimizer.SingleProgressChanged += (s, p, prog) => { if (prog == 100) Interlocked.Increment(ref completed); };

		var baseStrategy = new PauseSma
		{
			Volume = 1,
			Security = security,
			Portfolio = portfolio,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			UnrealizedPnLInterval = ((stop - start).Ticks / 1000).To<TimeSpan>(),
		};

		var longParam = (StrategyParam<int>)baseStrategy.Parameters[nameof(baseStrategy.LongSma)];
		var shortParam = (StrategyParam<int>)baseStrategy.Parameters[nameof(baseStrategy.ShortSma)];
		longParam.SetOptimize(50, 100, 5);  // 11
		shortParam.SetOptimize(20, 40, 1);  // 21  => 231 combos (big queue, won't finish during the test)
		var strategies = baseStrategy.ToBruteForce(new IStrategyParam[] { longParam, shortParam }, out _, out _);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var runTask = Task.Run(async () =>
		{
			try
			{
				await foreach (var _ in optimizer.RunAsync(start, stop, strategies, cts.Token)) { }
			}
			catch (OperationCanceledException) { }
		}, CancellationToken);

		// run into steady state (cache warm, all workers busy) before pausing, so a "soft" pause
		// would reliably have a full batch of in-flight iterations to drain
		for (var i = 0; i < 600 && Volatile.Read(ref completed) < 16; i++)
			await Task.Delay(50, CancellationToken);

		IsTrue(Volatile.Read(ref completed) >= 8, "optimizer did not reach steady state before pause");

		// Snapshot immediately, before any drain. A responsive pause suspends the in-flight
		// backtests, so almost nothing more should complete; a "soft" pause that only blocks new
		// starts would let the whole in-flight batch (~8) run to completion.
		await optimizer.Pause();
		var atPause = Volatile.Read(ref completed);

		// wait well past one iteration's duration so a soft pause would have drained its batch
		await Task.Delay(TimeSpan.FromSeconds(8), CancellationToken);
		var afterPause = Volatile.Read(ref completed);

		Console.WriteLine($"PAUSE: completedAtPause={atPause} afterWait={afterPause} (+{afterPause - atPause}) runDone={runTask.IsCompleted}");

		IsTrue(afterPause - atPause <= 3, $"paused optimizer kept completing iterations: +{afterPause - atPause} after pause (in-flight backtests were not suspended)");
		IsFalse(runTask.IsCompleted, "run completed while paused (231-combo queue should be far from done)");

		// resume -> it must continue (the suspended backtests finish their remaining replay first,
		// which can take a few seconds, so poll rather than assume a fixed delay)
		await optimizer.Resume();
		for (var i = 0; i < 500 && Volatile.Read(ref completed) <= afterPause; i++)
			await Task.Delay(50, CancellationToken);
		IsTrue(Volatile.Read(ref completed) > afterPause, "resume did not continue the optimization");

		// stop the (large) run
		cts.Cancel();
		await runTask;
		optimizer.Dispose();
	}

	[TestMethod]
	[DoNotParallelize] // heavy optimizer run: needs full CPU so pause/resume timing is reliable
	public async Task PauseHaltsGenetic()
	{
		if (Paths.HistoryDataPath == null)
		{
			Console.WriteLine("SKIP: no history data");
			return;
		}

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var security = new Security { Id = Paths.HistoryDefaultSecurity, PriceStep = 0.01m };
		var portfolio = Portfolio.CreateSimulator();
		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		var start = Paths.HistoryBeginDate;
		var stop = Paths.HistoryBeginDate.AddDays(14);

		var optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);
		optimizer.EmulationSettings.BatchSize = 8;
		optimizer.AdapterCache = new();
		optimizer.Settings.Population = 8;
		optimizer.Settings.PopulationMax = 16;
		optimizer.Settings.GenerationsMax = 50;
		// Disable the fitness-stagnation termination: on this trivial SMA strategy the fitness plateaus
		// after a couple of generations, so with it enabled the whole run finishes before we can pause
		// mid-flight (it is the OR-termination that actually ends these short runs, not GenerationsMax).
		optimizer.Settings.GenerationsStagnation = 0;

		var completed = 0;
		optimizer.SingleProgressChanged += (s, p, prog) => { if (prog == 100) Interlocked.Increment(ref completed); };

		var baseStrategy = new PauseSma
		{
			Volume = 1,
			Security = security,
			Portfolio = portfolio,
			CandleType = TimeSpan.FromMinutes(5).TimeFrame(),
			UnrealizedPnLInterval = ((stop - start).Ticks / 1000).To<TimeSpan>(),
		};

		var longParam = (StrategyParam<int>)baseStrategy.Parameters[nameof(baseStrategy.LongSma)];
		var shortParam = (StrategyParam<int>)baseStrategy.Parameters[nameof(baseStrategy.ShortSma)];
		var geneticParams = baseStrategy.ToGeneticParameters([(longParam, null), (shortParam, null)]);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		Exception runError = null;

		var runTask = Task.Run(async () =>
		{
			try
			{
				await foreach (var _ in optimizer.RunAsync(start, stop, baseStrategy, geneticParams, s => s.PnL, cancellationToken: cts.Token)) { }
			}
			catch (OperationCanceledException) { }
			catch (Exception ex) { runError = ex; }
		}, CancellationToken);

		for (var i = 0; i < 1200 && Volatile.Read(ref completed) < 16; i++)
			await Task.Delay(50, CancellationToken);

		IsNull(runError, $"genetic run failed: {runError?.Message}");
		IsTrue(Volatile.Read(ref completed) >= 8, "genetic optimizer did not reach steady state before pause");

		await optimizer.Pause();
		var atPause = Volatile.Read(ref completed);

		await Task.Delay(TimeSpan.FromSeconds(8), CancellationToken);
		var afterPause = Volatile.Read(ref completed);

		Console.WriteLine($"PAUSE-GA: completedAtPause={atPause} afterWait={afterPause} (+{afterPause - atPause}) runDone={runTask.IsCompleted}");

		IsTrue(afterPause - atPause <= 3, $"paused genetic optimizer kept completing iterations: +{afterPause - atPause} after pause");
		IsFalse(runTask.IsCompleted, "genetic run completed while paused");

		await optimizer.Resume();
		for (var i = 0; i < 500 && Volatile.Read(ref completed) <= afterPause; i++)
			await Task.Delay(50, CancellationToken);
		IsTrue(Volatile.Read(ref completed) > afterPause, "resume did not continue the genetic optimization");

		cts.Cancel();
		await runTask;
		optimizer.Dispose();
	}
}
