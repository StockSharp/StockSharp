namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Эмуляционное подключение. Использует исторические данные и/или случайно сгенерированные.
	/// </summary>
	public class HistoryEmulationConnector : BaseEmulationConnector, IEmulationConnector, IExternalCandleSource
	{
		private sealed class EmulationEntityFactory : EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;
			private readonly IDictionary<string, Portfolio> _portfolios;

			public EmulationEntityFactory(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
			{
				_securityProvider = securityProvider;
				_portfolios = portfolios.ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);
			}

			public override Security CreateSecurity(string id)
			{
				return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolios.TryGetValue(name) ?? base.CreatePortfolio(name);
			}
		}

		private sealed class NonThreadMessageProcessor : IMessageProcessor
		{
			private readonly IMessageProcessor _messageProcessor;
			private bool _isStarted;

			bool IMessageProcessor.IsStarted
			{
				get { return _isStarted; }
			}

			int IMessageProcessor.MessageCount
			{
				get { return _messageProcessor.MessageCount; }
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
				_messageProcessor.Clear(message);
			}

			public NonThreadMessageProcessor(IMessageProcessor messageProcessor)
			{
				if (messageProcessor == null)
					throw new ArgumentNullException("messageProcessor");

				_messageProcessor = messageProcessor;
			}
		}

		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, TimeSpan>, int> _subscribedCandles = new CachedSynchronizedDictionary<Tuple<SecurityId, TimeSpan>, int>();
		private readonly SyncObject _suspendLock = new SyncObject();
		
		private readonly HistorySessionHolder _sessionHolder;
		private readonly HistoryMessageAdapter _marketDataAdapter;

		/// <summary>
		/// Создать <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securities">Инструменты, которые будут переданы через событие <see cref="IConnector.NewSecurities"/>.</param>
		/// <param name="portfolios">Портфели, которые будут переданы через событие <see cref="IConnector.NewPortfolios"/>.</param>
		public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios)
			: this(securities, portfolios, new StorageRegistry())
		{
		}

		/// <summary>
		/// Создать <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securities">Инструменты, с которыми будет вестись работа.</param>
		/// <param name="portfolios">Портфели, с которыми будет вестись работа.</param>
		/// <param name="storageRegistry">Хранилище данных.</param>
		public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
			: this(new CollectionSecurityProvider(securities), portfolios, storageRegistry)
		{
		}

		/// <summary>
		/// Создать <see cref="HistoryEmulationConnector"/>.
		/// </summary>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="portfolios">Портфели, с которыми будет вестись работа.</param>
		/// <param name="storageRegistry">Хранилище данных.</param>
		public HistoryEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

			if (portfolios == null)
				throw new ArgumentNullException("portfolios");

			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			// чтобы каждый раз при повторной эмуляции получать одинаковые номера транзакций
			TransactionIdGenerator = new IncrementalIdGenerator();

			_initialMoney = portfolios.ToDictionary(pf => pf, pf => pf.BeginValue);
			EntityFactory = new EmulationEntityFactory(securityProvider, _initialMoney.Keys);

			_sessionHolder = new HistorySessionHolder(TransactionIdGenerator, securityProvider);

			MarketDataAdapter = _marketDataAdapter = new HistoryMessageAdapter(_sessionHolder) { StorageRegistry = storageRegistry };

			_sessionHolder.MarketTimeChangedInterval = TimeSpan.FromSeconds(1);
			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			_sessionHolder.ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true, new NonThreadMessageProcessor(TransactionAdapter.InMessageProcessor));

			((MessageAdapter<IMessageSessionHolder>)TransactionAdapter).SessionHolder = _sessionHolder;
		}

		private readonly Dictionary<Portfolio, decimal> _initialMoney;

		/// <summary>
		/// Первоначальный размер денежных средств на счетах.
		/// </summary>
		public IDictionary<Portfolio, decimal> InitialMoney
		{
			get { return _initialMoney; }
		}

		/// <summary>
		/// Производить расчет данных на основе <see cref="ManagedMessageAdapter"/>. По-умолчанию включено.
		/// </summary>
		public override bool CalculateMessages
		{
			get { return false; }
		}

		/// <summary>
		/// Число загруженных событий.
		/// </summary>
		public int LoadedEventCount { get { return _marketDataAdapter.LoadedEventCount; } }

		/// <summary>
		/// Число обработанных событий.
		/// </summary>
		public int ProcessedEventCount { get; private set; }

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

				//var oldState = _state;
				_state = value;

				try
				{
					StateChanged.SafeInvoke();
				}
				catch (Exception ex)
				{
					RaiseProcessDataError(ex);
				}
			}
		}

		/// <summary>
		/// Событие о изменении состояния эмулятора <see cref="State"/>.
		/// </summary>
		public event Action StateChanged;

		/// <summary>
		/// Хранилище данных.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _marketDataAdapter.StorageRegistry; }
			set { _marketDataAdapter.StorageRegistry = value; }
		}

		/// <summary>
		/// Хранилище, которое используется по-умолчанию. По умолчанию используется <see cref="IStorageRegistry.DefaultDrive"/>.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _marketDataAdapter.Drive; }
			set { _marketDataAdapter.Drive = value; }
		}

		/// <summary>
		/// Формат маркет-данных. По умолчанию используется <see cref="StorageFormats.Binary"/>.
		/// </summary>
		public StorageFormats StorageFormat
		{
			get { return _marketDataAdapter.StorageFormat; }
			set { _marketDataAdapter.StorageFormat = value; }
		}

		/// <summary>
		/// Закончил ли эмулятор свою работу по причине окончания данных или он был прерван через метод <see cref="Stop"/>.
		/// </summary>
		public bool IsFinished { get; private set; }

		/// <summary>
		/// Включить возможность выдавать свечи напрямую в <see cref="ICandleManager"/>.
		/// Ускоряет работу, но будут отсутствовать события изменения свечек.
		/// По умолчанию выключено.
		/// </summary>
		public bool UseExternalCandleSource { get; set; }

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		protected override void OnStartExport()
		{
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу.
		/// </summary>
		protected override void OnStopExport()
		{
		}

		private bool CheckState(params EmulationStates[] states)
		{
			return states.Contains(State);

			//throw new InvalidOperationException("Невозможно выполнить операцию так как текущее состояние {0}.".Put(State));
		}

		/// <summary>
		/// Начать эмуляцию.
		/// </summary>
		/// <param name="startDate">Дата в истории, с которой необходимо начать эмуляцию.</param>
		/// <param name="stopDate">Дата в истории, на которой необходимо закончить эмуляцию (дата включается).</param>
		public void Start(DateTime startDate, DateTime stopDate)
		{
			if (stopDate < startDate)
				throw new ArgumentOutOfRangeException("startDate", startDate, LocalizedStrings.Str1119Params.Put(startDate, stopDate));

			//f (stopDate == startDate)
			//throw new ArgumentOutOfRangeException("startDate", startDate, "Дата начала {0} равна дате окончания.".Put(startDate));

			if (!CheckState(EmulationStates.Stopped))
				throw new InvalidOperationException(LocalizedStrings.Str1120Params.Put(State));

			//_sessionHolder.StartDate = startDate;
			//_sessionHolder.StopDate = stopDate;

			ClearCache();

			((IncrementalIdGenerator)TransactionIdGenerator).Current = MarketEmulator.Settings.InitialTransactionId;

			TransactionAdapter.SendInMessage(new TimeMessage
			{
				ServerTime = startDate,
				LocalTime = startDate,
			});
			//base.OnProcessMessage(new TimeMessage { LocalTime = startDate }, MessageAdapterTypes.Transaction, MessageDirections.In);

			ProcessedEventCount = 0;

			//SetEmulationState(EmulationStates.Starting);
			MarketDataAdapter.SendInMessage(new EmulationStateMessage
			{
				//OldState = State,
				NewState = EmulationStates.Starting,
				StartDate = startDate,
				StopDate = stopDate,
				LocalTime = startDate,
			});
		}

		private void OnEmulationStarting()
		{
			IsFinished = false;

			// подписчики StateChanged запускают стратегии, которые интересуются MarketTime в OnRunning
			TransactionAdapter.SendInMessage(new ResetMessage());

			_sessionHolder.SecurityProvider.LookupAll().ForEach(SendSecurity);
			_initialMoney.ForEach(p => SendPortfolio(p.Key));

			SetEmulationState(EmulationStates.Started);
		}

		private void OnEmulationStarted()
		{
			//if (StorageRegistry == null)
			//{
			//	foreach (var security in RegisteredTrades)
			//	{
			//		if (!_marketDataAdapter.TradeGenerators.ContainsKey(GetSecurityId(security)))
			//			throw new InvalidOperationException("Для инструмента {0} не задан генератор сделок.".Put(security.Id));
			//	}

			//	foreach (var security in RegisteredMarketDepths)
			//	{
			//		if (!_marketDataAdapter.DepthGenerators.ContainsKey(GetSecurityId(security)))
			//			throw new InvalidOperationException("Для инструмента {0} не задан генератор стаканов.".Put(security.Id));
			//	}

			//	foreach (var security in RegisteredOrderLogs)
			//	{
			//		if (!_marketDataAdapter.OrderLogGenerators.ContainsKey(GetSecurityId(security)))
			//			throw new InvalidOperationException("Для инструмента {0} не задан генератор лога заявок.".Put(security.Id));
			//	}
			//}

			//_orderLogBuilders.Clear();

			//if (CreateDepthFromOrdersLog)
			//{
			//	foreach (var security in RegisteredMarketDepths.Intersect(RegisteredOrderLogs))
			//	{
			//		_orderLogBuilders.Add(security, new OrderLogMarketDepthBuilder(GetMarketDepth(security)));
			//	}
			//}

			//_orderLogTrades.Clear();

			//if (CreateTradesFromOrdersLog)
			//	_orderLogTrades.AddRange(RegisteredTrades.Intersect(RegisteredOrderLogs));

			MarketDataAdapter.SendInMessage(new ConnectMessage());
		}

		/// <summary>
		/// Остановить эмуляцию.
		/// </summary>
		public void Stop()
		{
			SetEmulationState(EmulationStates.Stopping);
			_suspendLock.PulseAll();
		}

		/// <summary>
		/// Приостановить эмуляцию.
		/// </summary>
		public void Suspend()
		{
			SetEmulationState(EmulationStates.Suspending);
		}

		/// <summary>
		/// Возобновить эмуляцию.
		/// </summary>
		public void Resume()
		{
			SetEmulationState(EmulationStates.Started);
			_suspendLock.PulseAll();
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			try
			{
				if (adapterType == MessageAdapterTypes.Transaction)
				{
					switch (message.Type)
					{
						case ExtendedMessageTypes.Last:
						{
							var lastMsg = (LastMessage)message;

							if (State == EmulationStates.Started)
							{
								IsFinished = !lastMsg.IsError;

								// все данных пришли без ошибок или в процессе чтения произошла ошибка - начинаем остановку
								SetEmulationState(EmulationStates.Stopping);
								SetEmulationState(EmulationStates.Stopped);
							}

							if (State == EmulationStates.Stopping)
							{
								// тестирование было отменено и пришли все ранее прочитанные данные
								SetEmulationState(EmulationStates.Stopped);
							}

							break;
						}

						case ExtendedMessageTypes.Clearing:
							break;

						case ExtendedMessageTypes.EmulationState:
							ProcessEmulationStateMessage((EmulationStateMessage)message);
							break;

						default:
						{
							var candleMsg = message as CandleMessage;
							if (candleMsg != null)
							{
								ProcessCandleMessage((CandleMessage)message);
								break;
							}

							if (State == EmulationStates.Stopping && message.Type != MessageTypes.Disconnect)
								break;

							base.OnProcessMessage(message, adapterType, direction);
							ProcessedEventCount++;
							break;
						}
					}
				}
				else
					base.OnProcessMessage(message, adapterType, direction);
			}
			catch (Exception ex)
			{
				RaiseProcessDataError(ex);
				SetEmulationState(EmulationStates.Stopping);
			}
		}

		private void SetEmulationState(EmulationStates state)
		{
			MarketDataAdapter.SendInMessage(new EmulationStateMessage { NewState = state });
		}

		private void ProcessEmulationStateMessage(EmulationStateMessage msg)
		{
			this.AddInfoLog(LocalizedStrings.Str1121Params, State, msg.NewState);

			switch (msg.NewState)
			{
				case EmulationStates.Stopped:
				{
					State = msg.NewState;
					break;
				}

				case EmulationStates.Stopping:
				{
					if (State == EmulationStates.Started ||
						State == EmulationStates.Suspended ||
						State == EmulationStates.Starting) // при ошибках при запуске эмуляции состояние может быть Starting
					{
						State = msg.NewState;
						MarketDataAdapter.SendInMessage(new DisconnectMessage());
					}

					break;
				}

				case EmulationStates.Starting:
				{
					if (State == EmulationStates.Stopped)
					{
						_sessionHolder.StartDate = msg.StartDate;
						_sessionHolder.StopDate = msg.StopDate;
						_sessionHolder.UpdateCurrentTime(msg.StartDate);

						State = msg.NewState;
						OnEmulationStarting();
					}

					break;
				}

				case EmulationStates.Started:
				{
					if (State == EmulationStates.Starting || State == EmulationStates.Suspended)
					{
						State = msg.NewState;
						OnEmulationStarted();
					}

					break;
				}

				case EmulationStates.Suspending:
				{
					if (State == EmulationStates.Started)
					{
						State = msg.NewState;
						SetEmulationState(EmulationStates.Suspended);
					}

					break;
				}

				case EmulationStates.Suspended:
				{
					if (State == EmulationStates.Suspending)
					{
						State = msg.NewState;

						lock (_suspendLock)
							Monitor.Wait(_suspendLock);
					}
					
					break;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessCandleMessage(CandleMessage message)
		{
			if (!UseExternalCandleSource)
				return;

			var security = GetSecurity(message.SecurityId);
			var series = _series.TryGetValue(security);

			if (series != null)
				_newCandles.SafeInvoke(series, new[] { message.ToCandle(series) });
		}

		private void SendPortfolio(Portfolio portfolio)
		{
			MarketDataAdapter.SendOutMessage(portfolio.ToMessage());

			var money = _initialMoney[portfolio];

			MarketDataAdapter.SendOutMessage(
				_sessionHolder
					.CreatePortfolioChangeMessage(portfolio.Name)
						.Add(PositionChangeTypes.BeginValue, money)
						.Add(PositionChangeTypes.CurrentValue, money)
						.Add(PositionChangeTypes.BlockedValue, 0m));
		}

		private void SendSecurity(Security security)
		{
			MarketDataAdapter.SendOutMessage(security.Board.ToMessage());

			MarketDataAdapter.SendOutMessage(security.ToMessage(security.ToSecurityId()));

			//MarketDataAdapter.SendOutMessage(new Level1ChangeMessage { SecurityId = security.ToSecurityId() }
			//	.Add(Level1Fields.StepPrice, security.StepPrice)
			//	.Add(Level1Fields.MinPrice, security.MinPrice)
			//	.Add(Level1Fields.MaxPrice, security.MaxPrice)
			//	.Add(Level1Fields.MarginBuy, security.MarginBuy)
			//	.Add(Level1Fields.MarginSell, security.MarginSell));
		}

		//private void InitOrderLogBuilders(DateTime loadDate)
		//{
		//	if (StorageRegistry == null || !MarketEmulator.Settings.UseMarketDepth)
		//		return;

		//	foreach (var security in RegisteredMarketDepths)
		//	{
		//		var builder = _orderLogBuilders.TryGetValue(security);

		//		if (builder == null)
		//			continue;

		//		// стакан из ОЛ строиться начиная с 18.45 предыдущей торговой сессии
		//		var olDate = loadDate.Date;

		//		do
		//		{
		//			olDate -= TimeSpan.FromDays(1);
		//		}
		//		while (!ExchangeBoard.Forts.WorkingTime.IsTradeDate(olDate));

		//		olDate += new TimeSpan(18, 45, 0);

		//		foreach (var item in StorageRegistry.GetOrderLogStorage(security, Drive).Load(olDate, loadDate - TimeSpan.FromTicks(1)))
		//		{
		//			builder.Update(item);
		//		}
		//	}
		//}

		private Security FindSecurity(SecurityId secId)
		{
			return this.LookupById(SecurityIdGenerator.GenerateId(secId.SecurityCode, secId.BoardCode));
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public override IEnumerable<Security> Lookup(Security criteria)
		{
			var securities = _sessionHolder.SecurityProvider.Lookup(criteria);

			if (State == EmulationStates.Started)
			{
				foreach (var security in securities)
					SendSecurity(security);	
			}

			return securities;
		}

		/// <summary>
		/// Зарегистрировать генератор сделок.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void RegisterTrades(TradeGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.TradeGenerators[generator.SecurityId] = generator;
			RegisterTrades(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Удалить генератор сделок, ранее зарегистрированный через <see cref="RegisterTrades"/>.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void UnRegisterTrades(TradeGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.TradeGenerators.Remove(generator.SecurityId);
			UnRegisterTrades(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Зарегистрировать генератор стаканов.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void RegisterMarketDepth(MarketDepthGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.DepthGenerators[generator.SecurityId] = generator;
			RegisterMarketDepth(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Удалить генератор стаканов, ранее зарегистрированный через <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.DepthGenerators.Remove(generator.SecurityId);
			UnRegisterMarketDepth(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Зарегистрировать генератор лога заявок.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void RegisterOrderLog(OrderLogGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.OrderLogGenerators[generator.SecurityId] = generator;
			RegisterOrderLog(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Удалить генератор лога заявок, ранее зарегистрированный через <see cref="RegisterOrderLog"/>.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void UnRegisterOrderLog(OrderLogGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			_marketDataAdapter.OrderLogGenerators.Remove(generator.SecurityId);
			UnRegisterOrderLog(FindSecurity(generator.SecurityId));
		}

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо начать получать новую информацию.</param>
		protected override void OnRegisterPortfolio(Portfolio portfolio)
		{
			_initialMoney.TryAdd(portfolio, portfolio.BeginValue);

			if (State == EmulationStates.Started)
				SendPortfolio(portfolio);
		}

		/// <summary>
		/// Подписаться на получение рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public override void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			var tf = MarketEmulator.Settings.UseCandlesTimeFrame;

			if (tf != null)
			{
				var securityId = GetSecurityId(security);
				var key = Tuple.Create(securityId, tf.Value);

				if (_subscribedCandles.ChangeSubscribers(key, 1) != 1)
					return;

				MarketDataAdapter.SendInMessage(new MarketDataMessage
				{
					//SecurityId = securityId,
					DataType = MarketDataTypes.CandleTimeFrame,
					Arg = tf.Value,
					IsSubscribe = true,
				}.FillSecurityInfo(this, security));
			}
			else
				base.SubscribeMarketData(security, type);
		}

		/// <summary>
		/// Отписаться от получения рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			var tf = MarketEmulator.Settings.UseCandlesTimeFrame;

			if (tf != null)
			{
				var securityId = GetSecurityId(security);
				var key = Tuple.Create(securityId, tf.Value);

				if (_subscribedCandles.ChangeSubscribers(key, -1) != 0)
					return;

				MarketDataAdapter.SendInMessage(new MarketDataMessage
				{
					//SecurityId = securityId,
					DataType = MarketDataTypes.CandleTimeFrame,
					Arg = tf.Value,
					IsSubscribe = false,
				}.FillSecurityInfo(this, security));
			}
			else
				base.UnSubscribeMarketData(security, type);
		}

		/// <summary>
		/// Начать получать новую информацию (например, <see cref="Security.LastTrade"/> или <see cref="Security.BestBid"/>) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <returns><see langword="true"/>, если удалось подписаться на получение данных, иначе, <see langword="false"/>.</returns>
		protected override bool OnRegisterSecurity(Security security)
		{
			return true;
		}

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через событие <see cref="IConnector.MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		/// <returns><see langword="true"/>, если удалось подписаться на получение данных, иначе, <see langword="false"/>.</returns>
		protected override bool OnRegisterMarketDepth(Security security)
		{
			return true;
		}

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через
		/// событие <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать сделки.</param>
		/// <returns><see langword="true"/>, если удалось подписаться на получение данных, иначе, <see langword="false"/>.</returns>
		protected override bool OnRegisterTrades(Security security)
		{
			return true;
		}

		/// <summary>
		/// Начать получать лог заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать лог заявок.</param>
		/// <returns><see langword="true"/>, если удалось подписаться на получение данных, иначе, <see langword="false"/>.</returns>
		protected override bool OnRegisterOrderLog(Security security)
		{
			return true;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (State == EmulationStates.Started || State == EmulationStates.Suspended)
				Stop();

			base.DisposeManaged();
		}

		private readonly SynchronizedDictionary<Security, CandleSeries> _series = new SynchronizedDictionary<Security, CandleSeries>();

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (UseExternalCandleSource && series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && (TimeSpan)series.Arg == MarketEmulator.Settings.UseCandlesTimeFrame)
			{
				yield return new Range<DateTimeOffset>(_sessionHolder.StartDate, _sessionHolder.StopDate.EndOfDay());
			}
		}

		private Action<CandleSeries, IEnumerable<Candle>> _newCandles;
		
		event Action<CandleSeries, IEnumerable<Candle>> IExternalCandleSource.NewCandles
		{
			add { _newCandles += value; }
			remove { _newCandles -= value; }
		}

		void IExternalCandleSource.SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			_series.Add(series.Security, series);
		}

		void IExternalCandleSource.UnSubscribeCandles(CandleSeries series)
		{
			_series.Remove(series.Security);
		}
	}
}