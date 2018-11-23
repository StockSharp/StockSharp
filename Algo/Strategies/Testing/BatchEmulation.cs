#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Testing.Algo
File: BatchEmulation.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The batch emulator of strategies.
	/// </summary>
	public class BatchEmulation
	{
		private sealed class BasketEmulationAdapter : BasketMessageAdapter
		{
			private readonly HistoryEmulationConnector _parent;

			public override DateTimeOffset CurrentTime => _parent.CurrentTime;

			public BasketEmulationAdapter(HistoryEmulationConnector parent)
				: base(parent.TransactionIdGenerator, new InMemoryMessageAdapterProvider(), new CandleBuilderProvider(new InMemoryExchangeInfoProvider()))
			{
				_parent = parent;
			}

			protected override void OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						break;

					case MessageTypes.Security:
					case MessageTypes.Board:
					case MessageTypes.Level1Change:
					case MessageTypes.QuoteChange:
					case MessageTypes.Time:
					{
						if (message.Adapter == _parent.MarketDataAdapter)
							SendMessageToEmulationAdapters(message);

						break;
					}

					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
					{
						if (message.Adapter != _parent.MarketDataAdapter)
							break;

						SendMessageToEmulationAdapters(message);
						return;
					}

					case MessageTypes.Execution:
					{
						if (message.Adapter != _parent.MarketDataAdapter)
						{
							var execMsg = (ExecutionMessage)message;

							if (execMsg.ExecutionType != ExecutionTypes.Transaction)
							{
								if (innerAdapter != InnerAdapters.LastOrDefault())
									return;
							}
						}
						else
							SendMessageToEmulationAdapters(message);

						break;
					}
				}

				base.OnInnerAdapterNewOutMessage(innerAdapter, message);
			}

			private void SendMessageToEmulationAdapters(Message message)
			{
				InnerAdapters
					.OfType<EmulationMessageAdapter>()
					.ForEach(a => a.SendInMessage(message));
			}
		}

		private sealed class OptimizationEmulationConnector : HistoryEmulationConnector
		{
			public OptimizationEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry) 
				: base(securityProvider, portfolios, storageRegistry)
			{
				Adapter = new BasketEmulationAdapter(this);
				Adapter.InnerAdapters.Add(EmulationAdapter);
				Adapter.InnerAdapters.Add(HistoryMessageAdapter);

				Adapter.LatencyManager = null;
				Adapter.CommissionManager = null;
				Adapter.PnLManager = null;
				Adapter.SlippageManager = null;
			}
		}

		private readonly SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>> _strategyInfo = new SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>>();

		private EmulationStates _prev = EmulationStates.Stopped;

		private IEnumerator<IEnumerable<Strategy>> _batches;
		private Strategy[] _batch = ArrayHelper.Empty<Strategy>();
		private bool _cancelEmulation;
		private TimeSpan _progressStep;
		private DateTime _nextTime;
		private int _totalBatches;
		private int _currentBatch;

		private IEnumerable<Security> EmulatorSecurities => ((HistoryMessageAdapter)EmulationConnector.MarketDataAdapter).SecurityProvider.LookupAll();

		/// <summary>
		/// Emulation settings.
		/// </summary>
		public EmulationSettings EmulationSettings { get; }

		/// <summary>
		/// The emulation connection.
		/// </summary>
		public HistoryEmulationConnector EmulationConnector { get; }

		/// <summary>
		/// The strategies for testing.
		/// </summary>
		public IEnumerable<Strategy> Strategies { get; set; }

		/// <summary>
		/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="BatchEmulation.Stop"/>method.
		/// </summary>
		public bool IsFinished => EmulationConnector.IsFinished;

		private int _progress;

		/// <summary>
		/// The current progress of paper trade process.
		/// </summary>
		public int CurrentProgress
		{
			get => _progress;
			set
			{
				if (_progress == value)
					return;

				_progress = value;

				TotalProgress = (int)((100m / _totalBatches) * (_currentBatch + _progress / 100m));

				ProgressChanged?.Invoke(_progress, TotalProgress);
			}
		}

		/// <summary>
		/// The general progress of paper trade.
		/// </summary>
		public int TotalProgress { get; set; }

		private EmulationStates _state = EmulationStates.Stopped;
		
		/// <summary>
		/// The emulator state.
		/// </summary>
		public EmulationStates State
		{
			get => _state;
			private set
			{
				if (_state == value)
					return;

				var oldState = _state;
				_state = value;
				StateChanged?.Invoke(oldState, _state);
			}
		}

		/// <summary>
		/// Current tested strategies.
		/// </summary>
		public IEnumerable<Strategy> BatchStrategies => _batch;

		/// <summary>
		/// The event on change of paper trade state.
		/// </summary>
		public event Action<EmulationStates, EmulationStates> StateChanged;

		/// <summary>
		/// The event of paper trade progress change.
		/// </summary>
		public event Action<int, int> ProgressChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securities">Instruments, the operation will be performed with.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BatchEmulation(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this((ISecurityProvider)new CollectionSecurityProvider(securities), portfolios, storageRegistry)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
		/// <param name="storageRegistry">Market data storage.</param>
		public BatchEmulation(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		{
			if (securityProvider == null)
				throw new ArgumentNullException(nameof(securityProvider));

			if (portfolios == null)
				throw new ArgumentNullException(nameof(portfolios));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			Strategies = Enumerable.Empty<Strategy>();

			EmulationSettings = new EmulationSettings();
			EmulationConnector = new OptimizationEmulationConnector(securityProvider, portfolios, storageRegistry)
			{
				UpdateSecurityLastQuotes = false,
				UpdateSecurityByLevel1 = false
			};

			EmulationConnector.StateChanged += EmulationConnectorOnStateChanged;
			EmulationConnector.MarketTimeChanged += EmulationConnectorOnMarketTimeChanged;
			EmulationConnector.Disconnected += EmulationConnectorOnDisconnected;
			EmulationConnector.NewSecurity += EmulationConnectorOnNewSecurity;
		}

		private void EmulationConnectorOnStateChanged()
		{
			switch (EmulationConnector.State)
			{
				case EmulationStates.Starting:
				{
					if (_prev != EmulationStates.Stopped)
						break;

					_nextTime = EmulationSettings.StartTime + _progressStep;
					CurrentProgress = 0;

					ApplySettings();
					break;
				}

				case EmulationStates.Started:
					State = EmulationStates.Started;
					break;

				case EmulationStates.Stopping:
					break;

				case EmulationStates.Stopped:
				{
					if (!_cancelEmulation)
						CurrentProgress = 100;

					OnEmulationStopped();
					break;
				}
			}

			_prev = EmulationConnector.State;
		}

		private void EmulationConnectorOnMarketTimeChanged(TimeSpan timeSpan)
		{
			if (EmulationConnector.CurrentTime < _nextTime && 
			    EmulationConnector.CurrentTime < EmulationSettings.StopTime || 
			    EmulationConnector.State != EmulationStates.Started)
				return;

			_nextTime += _progressStep;
			CurrentProgress++;
		}

		private void EmulationConnectorOnDisconnected()
		{
			DisposeAdapters();
			TryStartNextBatch();
		}

		private void EmulationConnectorOnNewSecurity(Security security)
		{
			var level1Info = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = EmulationSettings.StartTime
			};

			if (security.PriceStep != null)
				level1Info.TryAdd(Level1Fields.PriceStep, security.PriceStep.Value);

			if (security.StepPrice != null)
				level1Info.TryAdd(Level1Fields.StepPrice, security.StepPrice.Value);

			level1Info.TryAdd(Level1Fields.MinPrice, security.MinPrice ?? 1m);
			level1Info.TryAdd(Level1Fields.MaxPrice, security.MaxPrice ?? 1000000m);

			if (security.MarginBuy != null)
				level1Info.TryAdd(Level1Fields.MarginBuy, security.MarginBuy.Value);

			if (security.MarginSell != null)
				level1Info.TryAdd(Level1Fields.MarginSell, security.MarginSell.Value);

			// fill level1 values
			EmulationConnector.SendInMessage(level1Info);
		}

		/// <summary>
		/// Start emulation.
		/// </summary>
		/// <param name="strategies">The strategies.</param>
		/// <param name="iterationCount">Iteration count.</param>
		public void Start(IEnumerable<Strategy> strategies, int iterationCount)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));

			_progressStep = ((EmulationSettings.StopTime - EmulationSettings.StartTime).Ticks / 100).To<TimeSpan>();
			
			_cancelEmulation = false;
			_totalBatches = (int)((decimal)iterationCount / EmulationSettings.BatchSize).Ceiling();
			_currentBatch = -1;

			CurrentProgress = 0;
			
			State = EmulationStates.Starting;

			_batches = strategies.Batch(EmulationSettings.BatchSize).GetEnumerator();

			TryStartNextBatch();
		}

		private void TryStartNextBatch()
		{
			if (!_batches.MoveNext() || _cancelEmulation)
			{
				State = EmulationStates.Stopping;
				State = EmulationStates.Stopped;
				return;
			}

			_batch = _batches.Current.ToArray();
			_currentBatch ++;

			EmulationConnector.ClearCache();

			InitAdapters();

			EmulationConnector.HistoryMessageAdapter.StartDate = EmulationSettings.StartTime;
			EmulationConnector.HistoryMessageAdapter.StopDate = EmulationSettings.StopTime;

			EmulationConnector.LookupSecuritiesResult += OnEmulationConnectorOnLookupSecuritiesResult;

			EmulationConnector.Connect();
		}

		private void InitAdapters()
		{
			var adapter = EmulationConnector.Adapter;
			
			var id = _currentBatch * EmulationSettings.BatchSize;
			var portfolios = new List<Portfolio>();

			foreach (var strategy in _batch)
			{
				_strategyInfo[strategy] = new Tuple<Portfolio, Security>(strategy.Portfolio, strategy.Security);

				var portfolio = strategy.Portfolio.Clone();
				portfolio.Name += "_" + ++id;
				portfolios.Add(portfolio);
				
				var strategyAdapter = new EmulationMessageAdapter(EmulationConnector.TransactionIdGenerator);
				strategyAdapter.Emulator.Settings.Load(EmulationSettings.Save());

				adapter.InnerAdapters.Add(strategyAdapter);
				adapter.AdapterProvider.SetAdapter(portfolio.Name, strategyAdapter);

				strategy.Connector = EmulationConnector;
				strategy.Portfolio = portfolio;
				//strategy.Security = EmulationConnector.LookupById(strategy.Security.Id);
			}

			foreach (var portfolio in portfolios)
				EmulationConnector.RegisterPortfolio(portfolio);
		}

		private void ApplySettings()
		{
			var securities = EmulatorSecurities.ToArray();

			if (EmulationConnector.State == EmulationStates.Stopped || EmulationConnector.State == EmulationStates.Starting)
			{
				var realData = false;

				switch (EmulationSettings.TradeDataMode)
				{
					case EmulationMarketDataModes.Generate:
					{
						foreach (var sec in securities)
							EmulationConnector.RegisterTrades(new RandomWalkTradeGenerator(EmulationConnector.GetSecurityId(sec)));

						break;
					}

					case EmulationMarketDataModes.Storage:
					{
						realData = true;
						break;
					}
				}

				switch (EmulationSettings.DepthDataMode)
				{
					case EmulationMarketDataModes.Generate:
					{
						foreach (var sec in securities)
							EmulationConnector.RegisterMarketDepth(new TrendMarketDepthGenerator(EmulationConnector.GetSecurityId(sec)));

						break;
					}
					case EmulationMarketDataModes.Storage:
						realData = true;
						break;
				}

				if (EmulationSettings.OrderLogDataMode == EmulationMarketDataModes.Storage)
					realData = true;

				if (!realData)
					EmulationConnector.HistoryMessageAdapter.StorageRegistry = null;
			}

			EmulationConnector.MarketTimeChangedInterval = EmulationSettings.MarketTimeChangedInterval;
			EmulationConnector.LogLevel = EmulationSettings.LogLevel;

			MemoryStatistics.Instance.LogLevel = EmulationSettings.LogLevel;
		}

		private void OnEmulationConnectorOnLookupSecuritiesResult(SecurityLookupMessage message, IEnumerable<Security> securities, Exception exception)
		{
			EmulationConnector.LookupSecuritiesResult -= OnEmulationConnectorOnLookupSecuritiesResult;

			// start strategy before emulation started
			OnEmulationStarting();

			// start historical data loading when connection established successfully and all data subscribed
			EmulationConnector.Start();
		}

		private void OnEmulationStarting()
		{
			MemoryStatistics.Instance.Clear(false);

			//ResetState(EmulatorSecurities);

			foreach (var strategy in _batch)
			{
				strategy.Reset();
				strategy.Start();
			}
		}

		private void OnEmulationStopped()
		{
			foreach (var strategy in _batch)
				strategy.Stop();
		}

		private void DisposeAdapters()
		{
			var adapter = EmulationConnector.Adapter;

			foreach (var strategy in _batch)
			{
				var strategyAdapter = adapter.AdapterProvider.GetAdapter(strategy.Portfolio);

				if (strategyAdapter != null)
				{
					adapter.InnerAdapters.Remove(strategyAdapter);

					adapter
						.AdapterProvider
						.RemoveAssociation(strategy.Portfolio.Name);
				}

				var tuple = _strategyInfo.TryGetValue(strategy);

				if (tuple == null)
					continue;

				strategy.Security = tuple.Item2;
				strategy.Portfolio = tuple.Item1;
			}

			_batch = ArrayHelper.Empty<Strategy>();
			_strategyInfo.Clear();
		}

		/// <summary>
		/// To stop paper trading.
		/// </summary>
		public void Stop()
		{
			_cancelEmulation = true;
			EmulationConnector.Disconnect();
		}
	}
}
