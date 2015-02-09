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
	/// Пакетный эмулятор стратегий.
	/// </summary>
	public class BatchEmulation
	{
		private sealed class NonThreadMessageProcessor : IMessageProcessor
		{
			private bool _isStarted;

			bool IMessageProcessor.IsStarted
			{
				get { return _isStarted; }
			}

			int IMessageProcessor.MessageCount
			{
				get { return 0; }
			}

			int IMessageProcessor.MaxMessageCount
			{
				get { return 1; }
				set { }
			}

			private Action<Message, IMessageAdapter> _newMessage;

			event Action<Message, IMessageAdapter> IMessageProcessor.NewMessage
			{
				add { _newMessage += value; }
				remove { _newMessage -= value; }
			}

			private Action _stopped;

			event Action IMessageProcessor.Stopped
			{
				add { _stopped += value; }
				remove { _stopped -= value; }
			}

			void IMessageProcessor.EnqueueMessage(Message message, IMessageAdapter adapter, bool force)
			{
				_newMessage.SafeInvoke(message, adapter);
			}

			void IMessageProcessor.Start()
			{
				_isStarted = true;
			}

			void IMessageProcessor.Stop()
			{
				_isStarted = false;
				_stopped.SafeInvoke();
			}

			void IMessageProcessor.Clear(ClearMessageQueueMessage message)
			{
				throw new NotImplementedException();
			}
		}

		private sealed class HistoryBasketSessionHolder : BasketSessionHolder
		{
			private DateTimeOffset _currentTime;

			public override DateTimeOffset CurrentTime
			{
				get { return _currentTime; }
			}

			public HistoryBasketSessionHolder(IdGenerator transactionIdGenerator)
				: base(transactionIdGenerator)
			{
			}

			public void UpdateCurrentTime(DateTimeOffset currentTime)
			{
				_currentTime = currentTime;
			}
		}

		private sealed class BasketEmulationAdapter : BasketMessageAdapter
		{
			private readonly EmulationSettings _settings;
			private bool _isInitialized;

			public BasketEmulationAdapter(BasketSessionHolder sessionHolder, EmulationSettings settings)
				: base(MessageAdapterTypes.Transaction, sessionHolder)
			{
				_settings = settings;
			}

			protected override void OnSendInMessage(Message message)
			{
				SessionHolder.DoIf<IMessageSessionHolder, HistoryBasketSessionHolder>(s => s.UpdateCurrentTime(message.LocalTime));

				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						if (!_isInitialized)
						{
							CreateInnerAdapters();

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

						if (execMsg.ExecutionType != ExecutionTypes.Order && execMsg.ExecutionType != ExecutionTypes.Trade)
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

			protected override void CreateInnerAdapters()
			{
				var tradeIdGenerator = new IncrementalIdGenerator();
				var orderIdGenerator = new IncrementalIdGenerator();

				foreach (var session in SessionHolder.InnerSessions)
				{
					if (!session.IsTransactionEnabled)
						continue;

					var adapter = (EmulationMessageAdapter)session.CreateTransactionAdapter();

					ApplySettings(adapter, tradeIdGenerator, orderIdGenerator);
					AddInnerAdapter(adapter, SessionHolder.InnerSessions[session]);
				}
			}

			protected override void DisposeInnerAdapters()
			{
				_isInitialized = false;
				base.DisposeInnerAdapters();
			}

			private void ApplySettings(EmulationMessageAdapter adapter, IncrementalIdGenerator tradeIdGenerator, IncrementalIdGenerator orderIdGenerator)
			{
				adapter.InMessageProcessor = new NonThreadMessageProcessor();
				adapter.OutMessageProcessor = new NonThreadMessageProcessor();

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

			protected override void StartMarketTimer()
			{
			}
		}

		private readonly SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>> _strategyInfo = new SynchronizedDictionary<Strategy, Tuple<Portfolio, Security>>();
		private readonly HistoryBasketSessionHolder _basketSessionHolder;

		private EmulationStates _prev = EmulationStates.Stopped;

		private IEnumerator<IEnumerable<Strategy>> _batches;
		private Strategy[] _batch = ArrayHelper<Strategy>.EmptyArray;
		private bool _cancelEmulation;
		private TimeSpan _progressStep;
		private DateTime _nextTime;
		private int _totalBatches;
		private int _currentBatch;

		private IEnumerable<Security> EmulatorSecurities
		{
			get { return ((MessageAdapter<HistorySessionHolder>)EmulationConnector.MarketDataAdapter).SessionHolder.SecurityProvider.LookupAll(); }
		}

		/// <summary>
		/// Настройки эмуляции.
		/// </summary>
		public EmulationSettings EmulationSettings { get; private set; }

		/// <summary>
		/// Эмуляционное подключение.
		/// </summary>
		public HistoryEmulationConnector EmulationConnector { get; private set; }

		/// <summary>
		/// Стратегии для тестирования.
		/// </summary>
		public IEnumerableEx<Strategy> Strategies { get; set; }

		/// <summary>
		/// Закончил ли эмулятор свою работу по причине окончания данных или он был прерван через метод <see cref="Stop"/>.
		/// </summary>
		public bool IsFinished { get { return EmulationConnector.IsFinished; } }

		private int _progress;

		/// <summary>
		/// Текущий прогресс процесса эмуляции.
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

				ProgressChanged.SafeInvoke(_progress, _totalProgress);
			}
		}

		private int _totalProgress;

		/// <summary>
		/// Общий прогресс эмуляции.
		/// </summary>
		public int TotalProgress
		{
			get { return _totalProgress; }
			set { _totalProgress = value; }
		}

		private EmulationStates _state = EmulationStates.Stopped;
		
		/// <summary>
		/// Состояние эмулятора.
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
		/// Текущие тестируемые стратегии.
		/// </summary>
		public IEnumerable<Strategy> BatchStrategies { get { return _batch; } }

		/// <summary>
		/// Событие об изменении состояния эмуляции.
		/// </summary>
		public event Action<EmulationStates, EmulationStates> StateChanged;

		/// <summary>
		/// Событие изменения прогресса эмуляции.
		/// </summary>
		public event Action<int, int> ProgressChanged;

		/// <summary>
		/// Создать <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securities">Инструменты, с которыми будет вестись работа.</param>
		/// <param name="portfolios">Портфели, с которыми будет вестись работа.</param>
		/// <param name="storageRegistry">Хранилище данных.</param>
		public BatchEmulation(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this(new CollectionSecurityProvider(securities), portfolios, storageRegistry)
		{
		}

		/// <summary>
		/// Создать <see cref="BatchEmulation"/>.
		/// </summary>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="portfolios">Портфели, с которыми будет вестись работа.</param>
		/// <param name="storageRegistry">Хранилище данных.</param>
		public BatchEmulation(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

			if (portfolios == null)
				throw new ArgumentNullException("portfolios");

			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			Strategies = Enumerable.Empty<Strategy>().ToEx();

			EmulationSettings = new EmulationSettings();
			EmulationConnector = new HistoryEmulationConnector(securityProvider, portfolios, storageRegistry)
			{
				UpdateSecurityLastQuotes = false,
				UpdateSecurityByLevel1 = false
			};

			_basketSessionHolder = new HistoryBasketSessionHolder(EmulationConnector.TransactionIdGenerator);

			EmulationConnector.TransactionAdapter = new BasketEmulationAdapter(_basketSessionHolder, EmulationSettings)
			{
				OutMessageProcessor = new NonThreadMessageProcessor()
			};

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

					EmulationConnector.StartExport();
					OnEmulationStarting();

					break;
				}

				case EmulationStates.Started:
					State = EmulationStates.Started;
					break;

				case EmulationStates.Stopping:
					EmulationConnector.StopExport();
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
		/// Начать эмуляцию.
		/// </summary>
		public void Start(IEnumerableEx<Strategy> strategies)
		{
			if (strategies == null)
				throw new ArgumentNullException("strategies");

			_progressStep = ((EmulationSettings.StopTime - EmulationSettings.StartTime).Ticks / 100).To<TimeSpan>();
			
			_cancelEmulation = false;
			_totalBatches = (int)((decimal)strategies.Count / EmulationSettings.BatchSize).Ceiling();
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

			EmulationConnector.Connect();
			EmulationConnector.Start(EmulationSettings.StartTime, EmulationSettings.StopTime);
		}

		private void InitAdapters(IEnumerable<Strategy> strategies)
		{
			_basketSessionHolder.InnerSessions.Clear();
			_basketSessionHolder.Portfolios.Clear();

			var id = 0;

			foreach (var strategy in strategies)
			{
				//strategy.CheckCanStart();

				_strategyInfo[strategy] = new Tuple<Portfolio, Security>(strategy.Portfolio, strategy.Security);

				var portfolio = strategy.Portfolio.Clone();
				portfolio.Name += "_" + ++id;
				EmulationConnector.RegisterPortfolio(portfolio);

				AddHistorySessionHolder(portfolio.Name);

				strategy.Connector = EmulationConnector;
				strategy.Portfolio = portfolio;
				strategy.Security = EmulationConnector.LookupById(strategy.Security.Id);
			}
		}

		private void AddHistorySessionHolder(string portfolio)
		{
			var session = new HistorySessionHolder(EmulationConnector.TransactionIdGenerator)
			{
				IsMarketDataEnabled = false,
				IsTransactionEnabled = true,
			};

			_basketSessionHolder.InnerSessions.Add(session, 0);
			_basketSessionHolder.Portfolios[portfolio] = session;
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
					EmulationConnector.StorageRegistry = null;
			}

			EmulationConnector.MarketEmulator.Settings.UseCandlesTimeFrame = EmulationSettings.UseCandlesTimeFrame;
			EmulationConnector.MarketDataAdapter.SessionHolder.MarketTimeChangedInterval = EmulationSettings.MarketTimeChangedInterval;
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

			_batch = ArrayHelper<Strategy>.EmptyArray;
			_strategyInfo.Clear();

			EmulationConnector.Disconnect();

			TryStartNextBatch();
		}

		/// <summary>
		/// Остановить эмуляцию.
		/// </summary>
		public void Stop()
		{
			_cancelEmulation = true;
			EmulationConnector.Stop();
		}
	}
}
