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
			private readonly EmulationSettings _settings;
			private bool _isInitialized;

			public BasketEmulationAdapter(IdGenerator transactionIdGenerator, EmulationSettings settings)
				: base(transactionIdGenerator)
			{
				_settings = settings;
			}

			private DateTimeOffset _currentTime;

			public override DateTimeOffset CurrentTime => _currentTime;

			protected override void OnSendInMessage(Message message)
			{
				_currentTime = message.LocalTime;

				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						if (!_isInitialized)
						{
							//CreateInnerAdapters();

							_isInitialized = true;
						}
					
						GetSortedAdapters().ForEach(a => a.SendInMessage(message.Clone()));
						break;
					}

					case MessageTypes.OrderRegister:
					case MessageTypes.OrderReplace:
					case MessageTypes.OrderPairReplace:
					case MessageTypes.OrderCancel:
					case MessageTypes.OrderGroupCancel:
					case MessageTypes.MarketData:
						base.OnSendInMessage(message);
						break;

					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleVolume:
						GetSortedAdapters().ForEach(a => a.SendInMessage(message.Clone()));
						break;

					default:
						GetSortedAdapters().ForEach(a => a.SendInMessage(message)); //TODO Clone работает не для всех месседжей
						break;
				}
			}

			protected override void OnInnerAdapterNewMessage(IMessageAdapter innerAdapter, Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						base.OnInnerAdapterNewMessage(innerAdapter, message);
						break;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType != ExecutionTypes.Transaction)
						{
							if (innerAdapter != InnerAdapters.LastOrDefault())
								return;
						}

						base.OnInnerAdapterNewMessage(innerAdapter, message);
						break;
					}

					default:
					{
						// на выход данные идут только из одного адаптера
						if (innerAdapter != InnerAdapters.LastOrDefault())
							return;

						base.OnInnerAdapterNewMessage(innerAdapter, message);
						break;
					}
				}
			}

			//protected override void CreateInnerAdapters()
			//{
			//	var tradeIdGenerator = new IncrementalIdGenerator();
			//	var orderIdGenerator = new IncrementalIdGenerator();

			//	foreach (var session in SessionHolder.InnerSessions)
			//	{
			//		if (!session.IsTransactionEnabled)
			//			continue;

			//		var adapter = (EmulationMessageAdapter)session.CreateTransactionAdapter();

			//		ApplySettings(adapter, tradeIdGenerator, orderIdGenerator);
			//		AddInnerAdapter(adapter, SessionHolder.InnerSessions[session]);
			//	}
			//}

			private void ApplySettings(EmulationMessageAdapter adapter, IncrementalIdGenerator tradeIdGenerator, IncrementalIdGenerator orderIdGenerator)
			{
				adapter.Emulator.Settings.Load(_settings.Save());
				((MarketEmulator)adapter.Emulator).TradeIdGenerator = tradeIdGenerator;
				((MarketEmulator)adapter.Emulator).OrderIdGenerator = orderIdGenerator;
			}

			public override void SendOutMessage(Message message)
			{
				// обрабатываем только TimeMsg, которые получены из исторического адаптера
				// все, что приходит с незаполненным временем добавляется в других местах
				if (message.Type == MessageTypes.Time && message.LocalTime.IsDefault())
					return;

				base.SendOutMessage(message);
			}
		}

		private readonly SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>> _strategyInfo = new SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>>();
		//private readonly HistoryBasketSessionHolder _basketSessionHolder;

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
		/// The emulational connection.
		/// </summary>
		public HistoryEmulationConnector EmulationConnector { get; }

		/// <summary>
		/// The startegy for testing.
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
			get { return _progress; }
			set
			{
				if (_progress == value)
					return;

				_progress = value;

				TotalProgress = (int)((100m / _totalBatches) * (_currentBatch + _progress / 100m));

				ProgressChanged.SafeInvoke(_progress, TotalProgress);
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
			get { return _state; }
			private set
			{
				if (_state == value)
					return;

				var oldState = _state;
				_state = value;
				StateChanged.SafeInvoke(oldState, _state);
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
			EmulationConnector = new HistoryEmulationConnector(securityProvider, portfolios, storageRegistry)
			{
				UpdateSecurityLastQuotes = false,
				UpdateSecurityByLevel1 = false
			};

			//_basketSessionHolder = new HistoryBasketSessionHolder(EmulationConnector.TransactionIdGenerator);

			EmulationConnector.Adapter.InnerAdapters.Add(new BasketEmulationAdapter(EmulationConnector.TransactionIdGenerator, EmulationSettings));

			EmulationConnector.StateChanged += EmulationConnectorOnStateChanged;
			EmulationConnector.MarketTimeChanged += EmulationConnectorOnMarketTimeChanged;
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

					//EmulationConnector.StartExport();
					OnEmulationStarting();

					break;
				}

				case EmulationStates.Started:
					State = EmulationStates.Started;
					break;

				case EmulationStates.Stopping:
					//EmulationConnector.StopExport();
					break;

				case EmulationStates.Stopped:
					OnEmulationStopped();
					break;
			}

			_prev = EmulationConnector.State;
		}

		private void EmulationConnectorOnMarketTimeChanged(TimeSpan timeSpan)
		{
			if (EmulationConnector.CurrentTime < _nextTime && EmulationConnector.CurrentTime < EmulationSettings.StopTime)
				return;

			_nextTime += _progressStep;
			CurrentProgress++;
		}

		/// <summary>
		/// Start emulation.
		/// </summary>
		/// <param name="strategies">The strategies.</param>
		public void Start(IEnumerable<Strategy> strategies)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));

			_progressStep = ((EmulationSettings.StopTime - EmulationSettings.StartTime).Ticks / 100).To<TimeSpan>();
			
			_cancelEmulation = false;
			_totalBatches = (int)((decimal)strategies.Count() / EmulationSettings.BatchSize).Ceiling();
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

			InitAdapters(_batch);

			EmulationConnector.HistoryMessageAdapter.StartDate = EmulationSettings.StartTime;
			EmulationConnector.HistoryMessageAdapter.StopDate = EmulationSettings.StopTime;

			EmulationConnector.Connect();
		}

		private void InitAdapters(IEnumerable<Strategy> strategies)
		{
			//_basketSessionHolder.InnerSessions.Clear();
			//_basketSessionHolder.Portfolios.Clear();

			var id = 0;

			foreach (var strategy in strategies)
			{
				//strategy.CheckCanStart();

				_strategyInfo[strategy] = new Tuple<Portfolio, Security>(strategy.Portfolio, strategy.Security);

				var portfolio = strategy.Portfolio.Clone();
				portfolio.Name += "_" + ++id;
				EmulationConnector.RegisterPortfolio(portfolio);

				AddHistoryAdapter(portfolio.Name);

				strategy.Connector = EmulationConnector;
				strategy.Portfolio = portfolio;
				strategy.Security = EmulationConnector.LookupById(strategy.Security.Id);
			}
		}

		private void AddHistoryAdapter(string portfolio)
		{
			var session = new HistoryMessageAdapter(EmulationConnector.TransactionIdGenerator)
			{
			};

			//_basketSessionHolder.InnerSessions.Add(session, 0);
			//_basketSessionHolder.Portfolios[portfolio] = session;
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
			{
				strategy.Stop();

				var tuple = _strategyInfo.TryGetValue(strategy);

				if (tuple == null)
					continue;

				strategy.Security = tuple.Item2;
				strategy.Portfolio = tuple.Item1;
			}

			_batch = ArrayHelper.Empty<Strategy>();
			_strategyInfo.Clear();

			EmulationConnector.Disconnect();

			TryStartNextBatch();
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
