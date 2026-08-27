namespace StockSharp.Samples.Testing.History.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

internal sealed class HistoryFeedDefinition
{
	public HistoryFeedDefinition(string name, System.Drawing.Color curveColor)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		CurveColor = curveColor;
	}

	public string Name { get; }

	public System.Drawing.Color CurveColor { get; }

	public bool UseTicks { get; init; }

	public bool UseMarketDepth { get; init; }

	public DataType CandleType { get; set; }

	public bool UseOrderLog { get; init; }

	public bool UseLevel1 { get; init; }

	public Level1Fields? BuildField { get; init; }

	public MarketDataStorageCache Cache { get; } = new();
}

internal sealed class HistoryTestingOptions
{
	public string HistoryPath { get; init; }

	public string SecurityId { get; init; }

	public DateTime StartDate { get; init; }

	public DateTime StopDate { get; init; }

	public bool DebugLog { get; init; }

	public bool GenerateDepths { get; init; }

	public int MaxDepth { get; init; }

	public int MaxVolume { get; init; }

	public bool UseServerSideStops { get; init; }
}

internal sealed class HistoryFeedRun
{
	private readonly SemaphoreSlim _strategyStopGate = new(1, 1);
	private int _strategyStopped;

	internal HistoryFeedRun(
		HistoryFeedDefinition definition,
		HistoryEmulationConnector connector,
		Strategy strategy,
		Action<Subscription, Security> securityReceived)
	{
		Definition = definition;
		Connector = connector;
		Strategy = strategy;
		SecurityReceived = securityReceived;
	}

	public HistoryFeedDefinition Definition { get; }

	public HistoryEmulationConnector Connector { get; }

	public Strategy Strategy { get; }

	internal Action<Subscription, Security> SecurityReceived { get; }

	public async ValueTask StopStrategyAsync(CancellationToken cancellationToken)
	{
		await _strategyStopGate.WaitAsync(cancellationToken);
		try
		{
			if (Volatile.Read(ref _strategyStopped) != 0)
				return;

			if (Strategy.ProcessState != ProcessStates.Stopped)
				await Strategy.StopAsync(cancellationToken);

			Volatile.Write(ref _strategyStopped, 1);
		}
		finally
		{
			_strategyStopGate.Release();
		}
	}

	internal void DetachConnectorEvents()
		=> Connector.SecurityReceived -= SecurityReceived;

	internal void DisposeStopGate()
		=> _strategyStopGate.Dispose();
}

internal sealed class HistoryTestingSession : IAsyncDisposable
{
	private readonly StorageRegistry _storageRegistry;
	private readonly IReadOnlyList<HistoryFeedRun> _runs;
	private int _disposeState;

	private HistoryTestingSession(StorageRegistry storageRegistry, IReadOnlyList<HistoryFeedRun> runs)
	{
		_storageRegistry = storageRegistry;
		_runs = runs;
	}

	public IReadOnlyList<HistoryFeedRun> Runs => _runs;

	public static HistoryTestingSession Create(
		HistoryTestingOptions options,
		IReadOnlyCollection<HistoryFeedDefinition> definitions)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(definitions);
		if (string.IsNullOrWhiteSpace(options.HistoryPath))
			throw new ArgumentException("Select a history data folder.", nameof(options));
		if (!Paths.FileSystem.DirectoryExists(options.HistoryPath))
			throw new ArgumentException($"History folder does not exist: {options.HistoryPath}", nameof(options));
		if (string.IsNullOrWhiteSpace(options.SecurityId))
			throw new ArgumentException("Enter a security identifier.", nameof(options));
		if (definitions.Count == 0)
			throw new ArgumentException("Select at least one history feed.", nameof(definitions));
		if (options.StopDate < options.StartDate)
			throw new ArgumentOutOfRangeException(nameof(options), "The end date must not precede the begin date.");
		if (options.MaxDepth <= 0)
			throw new ArgumentOutOfRangeException(nameof(options.MaxDepth));
		if (options.MaxVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(options.MaxVolume));

		StorageRegistry storageRegistry = null;
		var runs = new List<HistoryFeedRun>(definitions.Count);
		try
		{
			var id = options.SecurityId.ToSecurityId();
			var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
			var security = new Security
			{
				Id = options.SecurityId,
				Code = id.SecurityCode,
				Board = exchangeInfoProvider.GetOrCreateBoard(id.BoardCode),
				PriceStep = 0.01m,
			};
			var securityId = security.ToSecurityId();
			var startDate = options.StartDate;
			if (definitions.Any(definition => definition.UseOrderLog))
			{
				startDate = startDate
					.Subtract(TimeSpan.FromDays(1))
					.AddHours(18)
					.AddMinutes(45)
					.AddTicks(1)
					.ApplyMoscow()
					.UtcDateTime;
			}

			storageRegistry = new StorageRegistry();
			storageRegistry.DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, options.HistoryPath);

			foreach (var definition in definitions)
			{
				var portfolio = Portfolio.CreateSimulator();
				portfolio.CurrentValue = 1_000m;
				HistoryEmulationConnector connector = null;
				Strategy strategy = null;
				Action<Subscription, Security> securityReceived = null;
				try
				{
					connector = new HistoryEmulationConnector(
						(ISecurityProvider)new CollectionSecurityProvider([security]),
						[portfolio]);
					connector.EmulationAdapter.Settings.MatchOnTouch = false;
					connector.EmulationAdapter.Settings.CommissionRules =
					[
						new CommissionTradeRule { Value = 0.01m },
					];
					connector.HistoryMessageAdapter.StorageRegistry = storageRegistry;
					connector.HistoryMessageAdapter.StorageFormat = StorageFormats.Binary;
					connector.HistoryMessageAdapter.StartDate = startDate;
					connector.HistoryMessageAdapter.StopDate = options.StopDate;
					connector.HistoryMessageAdapter.OrderLogMarketDepthBuilders.Add(
						securityId,
						new OrderLogMarketDepthBuilder(securityId));
					connector.HistoryMessageAdapter.AdapterCache = definition.Cache;
					connector.LogLevel = options.DebugLog ? LogLevels.Debug : LogLevels.Info;
					connector.SupportFilteredMarketDepth = true;

					strategy = CreateStrategy(options, definition, security, portfolio, connector, startDate);
					securityReceived = CreateSecurityReceivedHandler(
						options,
						definition,
						security,
						securityId,
						startDate,
						connector);
					connector.SecurityReceived += securityReceived;
					runs.Add(new(definition, connector, strategy, securityReceived));
				}
				catch (Exception initializationError)
				{
					var errors = new List<Exception> { initializationError };
					if (connector is not null && securityReceived is not null)
					{
						try
						{
							connector.SecurityReceived -= securityReceived;
						}
						catch (Exception error)
						{
							errors.Add(error);
						}
					}
					if (strategy is not null)
						TryDispose(strategy, errors);
					if (connector is not null)
						TryDispose(connector, errors);

					if (errors.Count > 1)
						throw new AggregateException("A historical feed failed to initialize and clean up.", errors);
					ExceptionDispatchInfo.Capture(initializationError).Throw();
					throw;
				}
			}

			return new(storageRegistry, runs);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };
			foreach (var run in runs)
			{
				try
				{
					run.DetachConnectorEvents();
				}
				catch (Exception error)
				{
					errors.Add(error);
				}
				TryDispose(run.Strategy, errors);
				TryDispose(run.Connector, errors);
				try
				{
					run.DisposeStopGate();
				}
				catch (Exception error)
				{
					errors.Add(error);
				}
			}
			if (storageRegistry is not null)
				TryDispose(storageRegistry, errors);

			if (errors.Count > 1)
				throw new AggregateException("Historical testing failed to initialize and clean up.", errors);
			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	private static Strategy CreateStrategy(
		HistoryTestingOptions options,
		HistoryFeedDefinition definition,
		Security security,
		Portfolio portfolio,
		HistoryEmulationConnector connector,
		DateTime startDate)
	{
		var unrealizedInterval = TimeSpan.FromTicks(Math.Max(1L, (options.StopDate - startDate).Ticks / 1000));
		if (options.UseServerSideStops)
		{
			var strategy = new SmaServerStopStrategy
			{
				LongSma = 80,
				ShortSma = 10,
				Volume = 1,
				Portfolio = portfolio,
				Security = security,
				Connector = connector,
				LogLevel = options.DebugLog ? LogLevels.Debug : LogLevels.Info,
				UnrealizedPnLInterval = unrealizedInterval,
			};
			ConfigureBuildSource(strategy, definition);
			return strategy;
		}

		var localStrategy = new SmaStrategy
		{
			LongSma = 80,
			ShortSma = 10,
			Volume = 1,
			Portfolio = portfolio,
			Security = security,
			Connector = connector,
			LogLevel = options.DebugLog ? LogLevels.Debug : LogLevels.Info,
			UnrealizedPnLInterval = unrealizedInterval,
		};
		ConfigureBuildSource(localStrategy, definition);
		return localStrategy;
	}

	private static void ConfigureBuildSource(SmaStrategy strategy, HistoryFeedDefinition definition)
	{
		if (definition.CandleType is not null)
		{
			strategy.CandleType = definition.CandleType;
			if (strategy.CandleType != TimeSpan.FromMinutes(1).TimeFrame())
				strategy.BuildFrom = TimeSpan.FromMinutes(1).TimeFrame();
		}
		else if (definition.UseTicks)
			strategy.BuildFrom = DataType.Ticks;
		else if (definition.UseLevel1)
		{
			strategy.BuildFrom = DataType.Level1;
			strategy.BuildField = definition.BuildField;
		}
		else if (definition.UseOrderLog)
			strategy.BuildFrom = DataType.OrderLog;
		else if (definition.UseMarketDepth)
			strategy.BuildFrom = DataType.MarketDepth;
	}

	private static void ConfigureBuildSource(SmaServerStopStrategy strategy, HistoryFeedDefinition definition)
	{
		if (definition.CandleType is not null)
		{
			strategy.CandleType = definition.CandleType;
			if (strategy.CandleType != TimeSpan.FromMinutes(1).TimeFrame())
				strategy.BuildFrom = TimeSpan.FromMinutes(1).TimeFrame();
		}
		else if (definition.UseTicks)
			strategy.BuildFrom = DataType.Ticks;
		else if (definition.UseLevel1)
		{
			strategy.BuildFrom = DataType.Level1;
			strategy.BuildField = definition.BuildField;
		}
		else if (definition.UseOrderLog)
			strategy.BuildFrom = DataType.OrderLog;
		else if (definition.UseMarketDepth)
			strategy.BuildFrom = DataType.MarketDepth;
	}

	private static Action<Subscription, Security> CreateSecurityReceivedHandler(
		HistoryTestingOptions options,
		HistoryFeedDefinition definition,
		Security security,
		SecurityId securityId,
		DateTime startDate,
		HistoryEmulationConnector connector)
	{
		var initialized = 0;
		var level1 = new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = startDate,
		}
		.TryAdd(Level1Fields.MinPrice, 0.01m)
		.TryAdd(Level1Fields.MaxPrice, 1_000_000m)
		.TryAdd(Level1Fields.MarginBuy, 10_000m)
		.TryAdd(Level1Fields.MarginSell, 10_000m);

		return (subscription, receivedSecurity) =>
		{
			if ((!ReferenceEquals(receivedSecurity, security) && receivedSecurity.Id != security.Id) ||
				Interlocked.Exchange(ref initialized, 1) != 0)
			{
				return;
			}

			_ = connector.EmulationAdapter.SendInMessageAsync(level1, default);
			if (definition.UseMarketDepth)
			{
				connector.Subscribe(new(DataType.MarketDepth, security));
				if (options.GenerateDepths || definition.CandleType is not null)
				{
					connector.RegisterMarketDepth(new TrendMarketDepthGenerator(connector.GetSecurityId(security))
					{
						Interval = TimeSpan.FromSeconds(1),
						MaxAsksDepth = options.MaxDepth,
						MaxBidsDepth = options.MaxDepth,
						UseTradeVolume = true,
						MaxVolume = options.MaxVolume,
						MinSpreadStepCount = 2,
						MaxSpreadStepCount = 5,
						MaxPriceStepCount = 3,
					});
				}
			}

			if (definition.UseOrderLog)
				connector.Subscribe(new(DataType.OrderLog, security));
			if (definition.UseTicks)
				connector.Subscribe(new(DataType.Ticks, security));
			if (definition.UseLevel1)
				connector.Subscribe(new(DataType.Level1, security));
		};
	}

	public async ValueTask StartAsync(CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		foreach (var run in _runs)
			await run.Strategy.StartAsync(cancellationToken);

		foreach (var run in _runs)
		{
			cancellationToken.ThrowIfCancellationRequested();
			run.Connector.Connect();
			await run.Connector.StartAsync(cancellationToken);
		}
	}

	public async ValueTask SuspendAsync()
	{
		ThrowIfDisposed();
		foreach (var run in _runs)
			await run.Connector.SuspendAsync();
	}

	public async ValueTask ResumeAsync()
	{
		ThrowIfDisposed();
		foreach (var run in _runs)
			await run.Connector.StartAsync();
	}

	public ValueTask StopStrategyAsync(HistoryFeedRun run, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(run);
		if (!_runs.Contains(run))
			throw new ArgumentException("The feed does not belong to this session.", nameof(run));
		return run.StopStrategyAsync(cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposeState, 1) != 0)
			return;

		var errors = new List<Exception>();
		foreach (var run in _runs)
		{
			try
			{
				run.DetachConnectorEvents();
			}
			catch (Exception error)
			{
				errors.Add(error);
			}
		}

		foreach (var run in _runs)
		{
			try
			{
				using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				await run.StopStrategyAsync(timeout.Token);
			}
			catch (Exception error)
			{
				errors.Add(error);
			}
		}

		foreach (var run in _runs)
		{
			try
			{
				if (run.Connector.ConnectionState == ConnectionStates.Connected)
					run.Connector.Disconnect();
			}
			catch (Exception error)
			{
				errors.Add(error);
			}

			TryDispose(run.Strategy, errors);
			TryDispose(run.Connector, errors);
			try
			{
				run.DisposeStopGate();
			}
			catch (Exception error)
			{
				errors.Add(error);
			}
		}

		TryDispose(_storageRegistry, errors);
		if (errors.Count > 0)
			throw new AggregateException("Historical testing cleanup encountered errors.", errors);
	}

	private void ThrowIfDisposed()
		=> ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);

	private static void TryDispose(IDisposable disposable, ICollection<Exception> errors)
	{
		try
		{
			disposable.Dispose();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}
}
