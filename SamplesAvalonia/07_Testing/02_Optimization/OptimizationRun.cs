namespace StockSharp.Samples.Testing.Optimization.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Logging;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Testing.History.Avalonia;

internal enum OptimizationMode
{
	BruteForce,
	Genetic,
}

internal sealed record OptimizationIterationSnapshot(
	Guid StrategyId,
	int LongSma,
	int ShortSma,
	TimeSpan? CandleTimeFrame,
	decimal PnL,
	decimal Position,
	DateTime CurrentTime,
	int OrderCount,
	int OwnTradeCount,
	int ClosedTradeCount,
	int ErrorCount,
	int ConnectorErrorCount,
	bool IsHistoryFinished,
	int Progress,
	bool IsCompleted);

/// <summary>
/// Owns one real StockSharp optimization run and every object created for it.
/// UI code receives real optimizer strategies only to bind the native statistics panel;
/// immutable snapshots remain the scalar result and validation contract.
/// </summary>
internal sealed class OptimizationRun : IAsyncDisposable
{
	private sealed class StrategyCounters : IDisposable
	{
		private readonly Strategy _strategy;
		private readonly Action<Subscription, MyTrade> _ownTradeReceived;
		private readonly Action<IStrategy, Exception> _errorReceived;
		private int _ownTradeCount;
		private int _errorCount;

		public StrategyCounters(Strategy strategy)
		{
			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			_ownTradeReceived = (_, _) => Interlocked.Increment(ref _ownTradeCount);
			_errorReceived = (_, _) => Interlocked.Increment(ref _errorCount);
			strategy.OwnTradeReceived += _ownTradeReceived;
			strategy.Error += _errorReceived;
		}

		public int OrderCount => StatisticInt(StatisticParameterTypes.OrderCount);

		public int OwnTradeCount => Volatile.Read(ref _ownTradeCount);

		public int ClosedTradeCount => StatisticInt(StatisticParameterTypes.TradeCount);

		public int ErrorCount
			=> Volatile.Read(ref _errorCount)
				+ StatisticInt(StatisticParameterTypes.OrderErrorCount)
				+ StatisticInt(StatisticParameterTypes.OrderCancelErrorCount)
				+ StatisticInt(StatisticParameterTypes.OrderInsufficientFundErrorCount);

		private int StatisticInt(StatisticParameterTypes type)
		{
			var value = _strategy.StatisticManager.Parameters
				.FirstOrDefault(parameter => parameter.Type == type)?.Value;
			return value is null ? 0 : Convert.ToInt32(value);
		}

		public void Dispose()
		{
			_strategy.OwnTradeReceived -= _ownTradeReceived;
			_strategy.Error -= _errorReceived;
		}
	}

	private readonly object _sync = new();
	private readonly StorageRegistry _storageRegistry;
	private readonly LogManager _logManager;
	private readonly BaseOptimizer _optimizer;
	private readonly SmaStrategy _seedStrategy;
	private readonly OptimizationMode _mode;
	private readonly GeneticSettings _geneticSettings;
	private readonly DateTime _startTime;
	private readonly DateTime _stopTime;
	private readonly HashSet<Strategy> _strategies = [];
	private readonly Dictionary<Guid, StrategyCounters> _strategyCounters = [];
	private readonly CancellationTokenSource _disposeCancellation = new();
	private readonly TaskCompletionSource _runFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private CancellationTokenSource _runCancellation;
	private int _started;
	private int _disposed;

	private OptimizationRun(
		string historyPath,
		OptimizationMode mode,
		GeneticSettings geneticSettings,
		DateTime startTime,
		DateTime stopTime)
	{
		try
		{
			_mode = mode;
			_geneticSettings = geneticSettings ?? throw new ArgumentNullException(nameof(geneticSettings));
			_startTime = startTime;
			_stopTime = stopTime;

			_storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, historyPath),
			};

			Security = new Security
			{
				Id = Paths.HistoryDefaultSecurity,
				PriceStep = 0.01m,
			};
			Portfolio = StockSharp.BusinessEntities.Portfolio.CreateSimulator();

			var securityProvider = new CollectionSecurityProvider([Security]);
			var portfolioProvider = new CollectionPortfolioProvider([Portfolio]);

			_optimizer = mode == OptimizationMode.BruteForce
				? new BruteForceOptimizer(securityProvider, portfolioProvider, _storageRegistry)
				: new GeneticOptimizer(securityProvider, portfolioProvider, _storageRegistry, Paths.FileSystem);

			_optimizer.EmulationSettings.MaxIterations = 100;
			_optimizer.EmulationSettings.CommissionRules =
			[
				new CommissionTradeRule { Value = 0.01m },
			];
			_optimizer.AdapterCache = new MarketDataStorageCache();
			_optimizer.SingleProgressChanged += OnSingleProgressChanged;
			_optimizer.StrategyInitialized += OnStrategyInitialized;
			_optimizer.ConnectorInitialized += OnConnectorInitialized;

			_optimizer.LogLevel = LogLevels.Error;
			_logManager = new LogManager();
			var fileLogListener = new FileLogListener("optimization.log");
			fileLogListener.Filters.Add(LoggingHelper.OnlyError);
			_logManager.Listeners.Add(fileLogListener);
			_logManager.Sources.Add(_optimizer);

			_seedStrategy = new SmaStrategy
			{
				Volume = 1,
				Security = Security,
				Portfolio = Portfolio,
				UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),
			};

			var longParam = (StrategyParam<int>)_seedStrategy.Parameters[nameof(SmaStrategy.LongSma)];
			var shortParam = (StrategyParam<int>)_seedStrategy.Parameters[nameof(SmaStrategy.ShortSma)];
			var timeFrameParam = (StrategyParam<TimeSpan?>)_seedStrategy.Parameters[nameof(SmaStrategy.CandleTimeFrame)];
			longParam.SetOptimize(50, 100, 5);
			shortParam.SetOptimize(20, 40, 1);
			timeFrameParam.SetOptimize(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5));
			OptimizeParameters = [longParam, shortParam, timeFrameParam];
		}
		catch
		{
			if (_optimizer is not null)
			{
				_optimizer.SingleProgressChanged -= OnSingleProgressChanged;
				_optimizer.StrategyInitialized -= OnStrategyInitialized;
				_optimizer.ConnectorInitialized -= OnConnectorInitialized;
				_optimizer.Dispose();
			}
			_seedStrategy?.Dispose();
			_storageRegistry?.Dispose();
			_logManager?.Dispose();
			_disposeCancellation.Dispose();
			throw;
		}
	}

	public Security Security { get; }

	public Portfolio Portfolio { get; }

	public IStrategyParam[] OptimizeParameters { get; }

	public bool IsPaused => _optimizer.IsPaused;

	public event Action<Strategy, OptimizationIterationSnapshot> IterationProgress;

	public event Action<int> TotalIterationsKnown;

	public event Action<int, int> CompletedCountChanged;

	public static OptimizationRun Create(
		string historyPath,
		OptimizationMode mode,
		GeneticSettings geneticSettings,
		DateTime startTime,
		DateTime stopTime)
	{
		if (string.IsNullOrWhiteSpace(historyPath))
			throw new ArgumentException("Select a history data folder.", nameof(historyPath));
		if (!Paths.FileSystem.DirectoryExists(historyPath))
			throw new ArgumentException($"History folder does not exist: {historyPath}", nameof(historyPath));
		if (stopTime <= startTime)
			throw new ArgumentOutOfRangeException(nameof(stopTime), "The end date must be later than the begin date.");

		return new OptimizationRun(historyPath, mode, geneticSettings, startTime, stopTime);
	}

	public async Task RunAsync(bool randomMode, int randomCount, CancellationToken cancellationToken)
	{
		if (Volatile.Read(ref _disposed) != 0)
			throw new ObjectDisposedException(nameof(OptimizationRun));
		if (Interlocked.Exchange(ref _started, 1) != 0)
			throw new InvalidOperationException("This optimization run has already been started.");

		_runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellation.Token);
		var token = _runCancellation.Token;
		var completed = 0;
		var total = 100;

		try
		{
			if (_mode == OptimizationMode.BruteForce)
			{
				var optimizer = (BruteForceOptimizer)_optimizer;
				IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> strategies;

				if (randomMode)
					strategies = _seedStrategy.ToBruteForceRandom(OptimizeParameters, randomCount, out _, out total);
				else
					strategies = _seedStrategy.ToBruteForce(OptimizeParameters, out _, out total);

				total = Math.Min(total, _optimizer.EmulationSettings.MaxIterations);
				TotalIterationsKnown?.Invoke(total);
				await foreach (var (strategy, _) in optimizer
					.RunAsync(_startTime, _stopTime, Track(strategies), token)
					.WithCancellation(token))
				{
					Track(strategy);
					IterationProgress?.Invoke(strategy, CreateSnapshot(strategy, 100, true));
					CompletedCountChanged?.Invoke(++completed, total);
				}
			}
			else
			{
				var optimizer = (GeneticOptimizer)_optimizer;
				TotalIterationsKnown?.Invoke(total);
				optimizer.Settings.Apply(_geneticSettings);
				var longParam = OptimizeParameters.Single(parameter => parameter.Id == nameof(SmaStrategy.LongSma));
				var shortParam = OptimizeParameters.Single(parameter => parameter.Id == nameof(SmaStrategy.ShortSma));
				var timeFrameParam = OptimizeParameters.Single(parameter => parameter.Id == nameof(SmaStrategy.CandleTimeFrame));
				var parameters = _seedStrategy.ToGeneticParameters(
				[
					(timeFrameParam, new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) }),
					(longParam, null),
					(shortParam, null),
				]);

				await foreach (var (strategy, _) in optimizer
					.RunAsync(_startTime, _stopTime, _seedStrategy, parameters, cancellationToken: token)
					.WithCancellation(token))
				{
					Track(strategy);
					IterationProgress?.Invoke(strategy, CreateSnapshot(strategy, 100, true));
					CompletedCountChanged?.Invoke(++completed, total);
				}
			}
		}
		finally
		{
			DisposeIterationObjects();
			_runFinished.TrySetResult();
		}
	}

	public Task PauseAsync()
		=> _optimizer.Pause();

	public Task ResumeAsync()
		=> _optimizer.Resume();

	private IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> Track(
		IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> source)
	{
		foreach (var item in source)
		{
			Track(item.strategy);
			yield return item;
		}
	}

	private void Track(Strategy strategy)
	{
		lock (_sync)
		{
			if (_strategies.Add(strategy))
				_strategyCounters.Add(strategy.Id, new StrategyCounters(strategy));
		}
	}

	private void OnSingleProgressChanged(Strategy strategy, IStrategyParam[] parameters, int progress)
	{
		Track(strategy);
		IterationProgress?.Invoke(strategy, CreateSnapshot(strategy, progress, false));
	}

	private void OnStrategyInitialized(Strategy strategy, IStrategyParam[] parameters)
		=> Track(strategy);

	private static void OnConnectorInitialized(Connector connector)
	{
		if (connector is not HistoryEmulationConnector)
			throw new InvalidOperationException($"Expected {nameof(HistoryEmulationConnector)}, got {connector?.GetType().Name ?? "null"}.");
	}

	private OptimizationIterationSnapshot CreateSnapshot(Strategy strategy, int progress, bool completed)
	{
		var sma = (SmaStrategy)strategy;
		var connector = strategy.Connector as HistoryEmulationConnector;
		StrategyCounters counters;
		lock (_sync)
			counters = _strategyCounters[strategy.Id];

		return new(
			strategy.Id,
			sma.LongSma,
			sma.ShortSma,
			sma.CandleTimeFrame,
			strategy.PnL,
			strategy.Position,
			strategy.CurrentTime,
			counters.OrderCount,
			counters.OwnTradeCount,
			counters.ClosedTradeCount,
			counters.ErrorCount,
			connector?.ErrorCount ?? 0,
			connector?.IsFinished == true,
			progress,
			completed);
	}

	private void DisposeIterationObjects()
	{
		Strategy[] strategies;
		StrategyCounters[] counters;
		lock (_sync)
		{
			strategies = [.. _strategies];
			counters = [.. _strategyCounters.Values];
			_strategies.Clear();
			_strategyCounters.Clear();
		}

		foreach (var counter in counters)
			counter.Dispose();

		foreach (var strategy in strategies)
		{
			var connector = strategy.Connector;
			strategy.Dispose();
			if (connector is IDisposable disposable)
				disposable.Dispose();
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		_disposeCancellation.Cancel();
		_runCancellation?.Cancel();
		if (Volatile.Read(ref _started) != 0)
		{
			try
			{
				await _runFinished.Task.ConfigureAwait(false);
			}
			catch
			{
			}
		}

		_optimizer.SingleProgressChanged -= OnSingleProgressChanged;
		_optimizer.StrategyInitialized -= OnStrategyInitialized;
		_optimizer.ConnectorInitialized -= OnConnectorInitialized;
		_logManager.Sources.Remove(_optimizer);
		_optimizer.Dispose();
		DisposeIterationObjects();
		_seedStrategy.Dispose();
		_storageRegistry.Dispose();
		_logManager.Dispose();
		_runCancellation?.Dispose();
		_disposeCancellation.Dispose();
	}
}
