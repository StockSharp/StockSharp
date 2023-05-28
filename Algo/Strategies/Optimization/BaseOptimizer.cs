namespace StockSharp.Algo.Strategies.Optimization;

using System;
using System.Collections.Generic;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies.Testing;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

/// <summary>
/// The base optimizer of strategies.
/// </summary>
public abstract class BaseOptimizer : BaseLogReceiver
{
	private class CacheAllocator
	{
		private readonly SynchronizedQueue<MarketDataStorageCache> _cache;
		private readonly MarketDataStorageCache _original;

		public CacheAllocator(int capacity, MarketDataStorageCache original)
		{
			_cache = new();
			_original = original ?? throw new ArgumentNullException(nameof(original));

			for (var i = 0; i < capacity; i++)
				Free(Allocate());
		}

		public MarketDataStorageCache Allocate()
			=> _cache.TryDequeue() ?? _original.Clone();

		public void Free(MarketDataStorageCache cache)
			=> _cache.Enqueue(cache ?? throw new ArgumentNullException(nameof(cache)));
	}

	private readonly HashSet<HistoryEmulationConnector> _startedConnectors = new();
	private bool _cancelEmulation;
	private int _lastTotalProgress;
	private DateTime _startedAt;
	private int _iterCount;
	private int _doneIters;

	private MarketDataStorageCache _adapterCache;
	private MarketDataStorageCache _storageCache;

	private CacheAllocator _adapterCacheAllocator;
	private CacheAllocator _storageCacheAllocator;

	private readonly SyncObject _sync = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	protected BaseOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat, IMarketDataDrive drive)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		PortfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

		EmulationSettings = new();

		StorageSettings = new()
		{
			StorageRegistry = storageRegistry,
			Drive = drive,
			Format = storageFormat,
		};
	}

	/// <summary>
	/// Storage settings.
	/// </summary>
	public StorageCoreSettings StorageSettings { get; }

	/// <summary>
	/// Emulation settings.
	/// </summary>
	public EmulationSettings EmulationSettings { get; }

	/// <summary>
	/// <see cref="HistoryMessageAdapter.AdapterCache"/>.
	/// </summary>
	public MarketDataStorageCache AdapterCache
	{
		get => _adapterCache;
		set
		{
			_adapterCache = value;
			_adapterCacheAllocator = value is null ? null : new(10, value);
		}
	}

	/// <summary>
	/// <see cref="HistoryMessageAdapter.StorageCache"/>.
	/// </summary>
	public MarketDataStorageCache StorageCache
	{
		get => _storageCache;
		set
		{
			_storageCache = value;
			_storageCacheAllocator = value is null ? null : new(10, value);
		}
	}

	/// <summary>
	/// Allocate <see cref="AdapterCache"/>.
	/// </summary>
	/// <returns><see cref="AdapterCache"/></returns>
	protected internal MarketDataStorageCache AllocateAdapterCache()
		=> _adapterCacheAllocator?.Allocate();

	/// <summary>
	/// Allocate <see cref="StorageCache"/>.
	/// </summary>
	/// <returns><see cref="StorageCache"/></returns>
	protected internal MarketDataStorageCache AllocateStorageCache()
		=> _storageCacheAllocator?.Allocate();

	/// <summary>
	/// Free <see cref="AdapterCache"/>.
	/// </summary>
	/// <param name="cache"><see cref="AdapterCache"/></param>
	protected internal void FreeAdapterCache(MarketDataStorageCache cache)
		=> _adapterCacheAllocator?.Free(cache);

	/// <summary>
	/// Free <see cref="StorageCache"/>.
	/// </summary>
	/// <param name="cache"><see cref="StorageCache"/></param>
	protected internal void FreeStorageCache(MarketDataStorageCache cache)
		=> _storageCacheAllocator?.Free(cache);

	/// <summary>
	/// <see cref="IPortfolioProvider"/>
	/// </summary>
	public ISecurityProvider SecurityProvider { get; }

	/// <summary>
	/// <see cref="IPortfolioProvider"/>
	/// </summary>
	public IPortfolioProvider PortfolioProvider { get; }

	/// <summary>
	/// <see cref="IExchangeInfoProvider"/>
	/// </summary>
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <summary>
	/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="Stop"/> method.
	/// </summary>
	public bool IsCancelled { get; private set; }

	private ChannelStates _state = ChannelStates.Stopped;

	/// <summary>
	/// The emulator state.
	/// </summary>
	public ChannelStates State
	{
		get => _state;
		protected set
		{
			if (_state == value)
				return;

			var oldState = _state;
			_state = value;
			StateChanged?.Invoke(oldState, _state);
		}
	}

	/// <summary>
	/// The event on change of paper trade state.
	/// </summary>
	public event Action<ChannelStates, ChannelStates> StateChanged;

	/// <summary>
	/// The event of total progress change.
	/// </summary>
	public event Action<int, TimeSpan, TimeSpan> TotalProgressChanged;

	/// <summary>
	/// The event of single progress change.
	/// </summary>
	public event Action<Strategy, IStrategyParam[], int> SingleProgressChanged;

	/// <summary>
	/// Start optimization.
	/// </summary>
	/// <param name="iterationCount">Iterations count.</param>
	protected void OnStart(int iterationCount)
	{
		if (iterationCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(iterationCount));

		_cancelEmulation = false;
		_startedAt = DateTime.UtcNow;
		_lastTotalProgress = -1;
		_iterCount = iterationCount;
		_doneIters = 0;

		State = ChannelStates.Starting;
		State = ChannelStates.Started;
	}

	/// <summary>
	/// To suspend the optimization.
	/// </summary>
	public virtual void Suspend()
	{
		lock (_sync)
		{
			if (State != ChannelStates.Started)
				return;

			State = ChannelStates.Suspending;

			foreach (var connector in _startedConnectors)
			{
				if (connector.State == ChannelStates.Started)
					connector.Suspend();
			}

			State = ChannelStates.Suspended;
		}
	}

	/// <summary>
	/// To resume the optimization.
	/// </summary>
	public virtual void Resume()
	{
		lock (_sync)
		{
			if (State != ChannelStates.Suspended)
				return;

			State = ChannelStates.Starting;

			foreach (var connector in _startedConnectors)
			{
				if (connector.State == ChannelStates.Suspended)
					connector.Start();
			}

			State = ChannelStates.Started;
		}
	}

	/// <summary>
	/// To stop optimization.
	/// </summary>
	public virtual void Stop()
	{
		lock (_sync)
		{
			if (!(State is ChannelStates.Started or ChannelStates.Suspended))
				return;

			State = ChannelStates.Stopping;

			_cancelEmulation = true;

			foreach (var connector in _startedConnectors)
			{
				if (connector.State is
					ChannelStates.Started or
					ChannelStates.Starting or
					ChannelStates.Suspended or
					ChannelStates.Suspending)
					connector.Disconnect();
			}
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Stop();
		base.DisposeManaged();
	}

	/// <summary>
	/// Try start next iteration.
	/// </summary>
	/// <param name="tryGetNext">Handler to try to get next strategy object.</param>
	/// <param name="adapterCache"><see cref="HistoryMessageAdapter.AdapterCache"/></param>
	/// <param name="storageCache"><see cref="HistoryMessageAdapter.StorageCache"/></param>
	/// <param name="iterationFinished">Callback to notify the iteration was finished.</param>
	protected internal void TryNextRun(Func<(Strategy, IStrategyParam[])?> tryGetNext,
		MarketDataStorageCache adapterCache, MarketDataStorageCache storageCache,
		Action iterationFinished)
	{
		if (tryGetNext is null)
			throw new ArgumentNullException(nameof(tryGetNext));

		if (iterationFinished is null)
			throw new ArgumentNullException(nameof(iterationFinished));

		Strategy strategy;
		IStrategyParam[] parameters;
		HistoryEmulationConnector connector;

		lock (_sync)
		{
			if (State == ChannelStates.Suspending || State == ChannelStates.Suspended)
				return;

			(Strategy, IStrategyParam[])? t;

			if (_cancelEmulation || (t = tryGetNext()) is null)
			{
				if (State == ChannelStates.Stopped)
					return;

				if (State != ChannelStates.Stopping)
					State = ChannelStates.Stopping;

				if (_startedConnectors.Count == 0)
				{
					IsCancelled = _cancelEmulation;
					State = ChannelStates.Stopped;
				}

				return;
			}

			strategy = t.Value.Item1;
			parameters = t.Value.Item2;

			strategy.Parent ??= this;

			connector = new(SecurityProvider, PortfolioProvider, ExchangeInfoProvider)
			{
				Parent = this,

				HistoryMessageAdapter =
				{
					StorageRegistry = StorageSettings.StorageRegistry,
					Drive = StorageSettings.Drive,
					StorageFormat = StorageSettings.Format,

					StartDate = EmulationSettings.StartTime,
					StopDate = EmulationSettings.StopTime,

					AdapterCache = adapterCache,
					StorageCache = storageCache,
				},
			};

			_startedConnectors.Add(connector);
		}

		connector.EmulationAdapter.Settings.Load(EmulationSettings.Save());

		strategy.Connector = connector;

		strategy.Reset();
		strategy.Start();

		var lastStep = 0;

		connector.ProgressChanged += step => SingleProgressChanged?.Invoke(strategy, parameters, lastStep = step);

		connector.StateChanged += () =>
		{
			if (connector.State != ChannelStates.Stopped)
				return;

			if (lastStep < 100)
				SingleProgressChanged?.Invoke(strategy, parameters, 100);

			int? progress;

			lock (_sync)
			{
				_startedConnectors.Remove(connector);

				_doneIters++;

				progress = (int)((_doneIters * 100.0) / _iterCount);

				if (_lastTotalProgress > progress)
					progress = null;
				else
					_lastTotalProgress = progress.Value;
			}

			if (progress is not null)
			{
				var evt = TotalProgressChanged;

				if (evt is not null)
				{
					var duration = DateTime.UtcNow - _startedAt;
					evt(progress.Value, duration, _iterCount == int.MaxValue ? TimeSpan.MaxValue : (_iterCount * 1.0 / _doneIters - 1) * duration);
				}
			}
			
			iterationFinished();
		};

		connector.Connect();
		connector.Start();
	}
}