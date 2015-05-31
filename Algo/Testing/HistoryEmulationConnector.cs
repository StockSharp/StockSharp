namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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
	public class HistoryEmulationConnector : BaseEmulationConnector, IExternalCandleSource
	{
		private class EmulationEntityFactory : EntityFactory
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

		private class HistoryBasketMessageAdapter : BasketMessageAdapter
		{
			private readonly HistoryEmulationConnector _parent;

			public HistoryBasketMessageAdapter(HistoryEmulationConnector parent)
				: base(parent.TransactionIdGenerator)
			{
				_parent = parent;
			}

			public override DateTimeOffset CurrentTime
			{
				get { return _parent.CurrentTime; }
			}
		}

		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, TimeSpan>, int> _subscribedCandles = new CachedSynchronizedDictionary<Tuple<SecurityId, TimeSpan>, int>();
		
		private readonly HistoryMessageAdapter _historyAdapter;
		private readonly EmulationMessageAdapter _emulationAdapter;

		private readonly InMemoryMessageChannel _historyChannel;

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

			OutMessageChannel = new PassThroughMessageChannel();

			_emulationAdapter = new EmulationMessageAdapter(TransactionIdGenerator);
			_historyAdapter = new HistoryMessageAdapter(TransactionIdGenerator, securityProvider) { StorageRegistry = storageRegistry };
			_historyChannel = new InMemoryMessageChannel("History Out", SendOutError);

			Adapter = new HistoryBasketMessageAdapter(this);
			Adapter.InnerAdapters.Add(_emulationAdapter);
			Adapter.InnerAdapters.Add(new ChannelMessageAdapter(_historyAdapter, new InMemoryMessageChannel("History In", SendOutError), _historyChannel));

			// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
			ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

			MaxMessageCount = 1000;
		}

		/// <summary>
		/// Интервал генерации сообщения <see cref="TimeMessage"/>. По-умолчанию равно 10 миллисекундам.
		/// </summary>
		public override TimeSpan MarketTimeChangedInterval
		{
			get { return _historyAdapter.MarketTimeChangedInterval; }
			set { _historyAdapter.MarketTimeChangedInterval = value; }
		}

		/// <summary>
		/// Дата в истории, с которой необходимо начать эмуляцию.
		/// </summary>
		public DateTimeOffset StartDate
		{
			get { return _historyAdapter.StartDate; }
			set { _historyAdapter.StartDate = value; }
		}

		/// <summary>
		/// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
		/// </summary>
		public DateTimeOffset StopDate
		{
			get { return _historyAdapter.StopDate; }
			set { _historyAdapter.StopDate = value; }
		}

		/// <summary>
		/// Максимальный размер очереди сообщений, до которого читаются исторические данные. По-умолчанию равно 1000.
		/// </summary>
		public int MaxMessageCount
		{
			get { return _historyChannel.MaxMessageCount; }
			set { _historyChannel.MaxMessageCount = value; }
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
		/// Число загруженных сообщений.
		/// </summary>
		public int LoadedMessageCount { get { return _historyAdapter.LoadedMessageCount; } }

		/// <summary>
		/// Число обработанных сообщений.
		/// </summary>
		public int ProcessedMessageCount { get { return _emulationAdapter.ProcessedMessageCount; } }

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

				bool throwError;

				switch (value)
				{
					case EmulationStates.Stopped:
						throwError = (_state != EmulationStates.Stopping);
						break;
					case EmulationStates.Stopping:
						throwError = (_state != EmulationStates.Started && _state != EmulationStates.Suspended
							&& State == EmulationStates.Starting);  // при ошибках при запуске эмуляции состояние может быть Starting
						break;
					case EmulationStates.Starting:
						throwError = (_state != EmulationStates.Stopped && _state != EmulationStates.Suspended);
						break;
					case EmulationStates.Started:
						throwError = (_state != EmulationStates.Starting);
						break;
					case EmulationStates.Suspending:
						throwError = (_state != EmulationStates.Started);
						break;
					case EmulationStates.Suspended:
						throwError = (_state != EmulationStates.Suspending);
						break;
					default:
						throw new ArgumentOutOfRangeException("value");
				}

				if (throwError)
					throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));

				_state = value;

				try
				{
					StateChanged.SafeInvoke();
				}
				catch (Exception ex)
				{
					SendOutError(ex);
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
			get { return _historyAdapter.StorageRegistry; }
			set { _historyAdapter.StorageRegistry = value; }
		}

		/// <summary>
		/// Хранилище, которое используется по-умолчанию. По умолчанию используется <see cref="IStorageRegistry.DefaultDrive"/>.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _historyAdapter.Drive; }
			set { _historyAdapter.Drive = value; }
		}

		/// <summary>
		/// Формат маркет-данных. По умолчанию используется <see cref="StorageFormats.Binary"/>.
		/// </summary>
		public StorageFormats StorageFormat
		{
			get { return _historyAdapter.StorageFormat; }
			set { _historyAdapter.StorageFormat = value; }
		}

		/// <summary>
		/// Закончил ли эмулятор свою работу по причине окончания данных или он был прерван через метод <see cref="IConnector.Disconnect"/>.
		/// </summary>
		public bool IsFinished { get; private set; }

		/// <summary>
		/// Включить возможность выдавать свечи напрямую в <see cref="ICandleManager"/>.
		/// Ускоряет работу, но будут отсутствовать события изменения свечек.
		/// По умолчанию выключено.
		/// </summary>
		public bool UseExternalCandleSource { get; set; }

		/// <summary>
		/// Очистить кэш данных.
		/// </summary>
		public override void ClearCache()
		{
			base.ClearCache();
			IsFinished = false;
		}

		/// <summary>
		/// Вызывать событие <see cref="Connector.Connected"/> при установке подключения первого адаптера в <see cref="Connector.Adapter"/>.
		/// </summary>
		protected override bool RaiseConnectedOnFirstAdapter
		{
			get { return false; }
		}

		///// <summary>
		///// Подключиться к торговой системе.
		///// </summary>
		//protected override void OnConnect()
		//{
		//	//SendInMessage(new TimeMessage { LocalTime = StartDate.LocalDateTime });
		//	SendInMessage(new ConnectMessage { LocalTime = StartDate.LocalDateTime });
		//}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		protected override void OnDisconnect()
		{
			SendEmulationState(EmulationStates.Stopping);
		}

		/// <summary>
		/// Запустить эмуляцию.
		/// </summary>
		public void Start()
		{
			SendEmulationState(EmulationStates.Starting);
		}

		/// <summary>
		/// Приостановить эмуляцию.
		/// </summary>
		public void Suspend()
		{
			SendEmulationState(EmulationStates.Suspending);
		}

		private void SendEmulationState(EmulationStates state)
		{
			SendInMessage(new EmulationStateMessage { State = state });
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageDirections direction)
		{
			try
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						base.OnProcessMessage(message, direction);

						if (message.Adapter == TransactionAdapter)
							_initialMoney.ForEach(p => SendPortfolio(p.Key));

						break;
					}

					case ExtendedMessageTypes.Last:
					{
						var lastMsg = (LastMessage)message;

						if (State == EmulationStates.Started)
						{
							IsFinished = !lastMsg.IsError;

							// все данных пришли без ошибок или в процессе чтения произошла ошибка - начинаем остановку
							SendEmulationState(EmulationStates.Stopping);
							SendEmulationState(EmulationStates.Stopped);
						}

						if (State == EmulationStates.Stopping)
						{
							// тестирование было отменено и пришли все ранее прочитанные данные
							SendEmulationState(EmulationStates.Stopped);
						}

						break;
					}

					case ExtendedMessageTypes.Clearing:
						break;

					case ExtendedMessageTypes.EmulationState:
						ProcessEmulationStateMessage(((EmulationStateMessage)message).State);
						break;

					case MessageTypes.Security:
					case MessageTypes.Board:
					case MessageTypes.Level1Change:
					case MessageTypes.QuoteChange:
					case MessageTypes.Time:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
					case MessageTypes.Execution:
					{
						if (message.Adapter == MarketDataAdapter)
							TransactionAdapter.SendInMessage(message);

						base.OnProcessMessage(message, direction);
						break;
					}

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

						base.OnProcessMessage(message, direction);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				SendOutError(ex);
				SendEmulationState(EmulationStates.Stopping);
			}
		}

		private void ProcessEmulationStateMessage(EmulationStates newState)
		{
			this.AddInfoLog(LocalizedStrings.Str1121Params, State, newState);

			State = newState;

			switch (newState)
			{
				case EmulationStates.Stopping:
				{
					SendInMessage(new DisconnectMessage());
					break;
				}

				case EmulationStates.Starting:
				{
					SendEmulationState(EmulationStates.Started);
					break;
				}
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
			SendInMessage(portfolio.ToMessage());

			var money = _initialMoney[portfolio];

			SendInMessage(
				_emulationAdapter
					.CreatePortfolioChangeMessage(portfolio.Name)
						.Add(PositionChangeTypes.BeginValue, money)
						.Add(PositionChangeTypes.CurrentValue, money)
						.Add(PositionChangeTypes.BlockedValue, 0m));
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

		///// <summary>
		///// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		///// </summary>
		///// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		///// <returns>Найденные инструменты.</returns>
		//public override IEnumerable<Security> Lookup(Security criteria)
		//{
		//	var securities = _historyAdapter.SecurityProvider.Lookup(criteria);

		//	if (State == EmulationStates.Started)
		//	{
		//		foreach (var security in securities)
		//			SendSecurity(security);	
		//	}

		//	return securities;
		//}

		private void SendInGeneratorMessage(MarketDataGenerator generator, bool isSubscribe)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			SendInMessage(new GeneratorMessage
			{
				IsSubscribe = isSubscribe,
				SecurityId = generator.SecurityId,
				Generator = generator,
				DataType = generator.DataType,
			});
		}

		/// <summary>
		/// Зарегистрировать генератор сделок.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void RegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор сделок, ранее зарегистрированный через <see cref="RegisterTrades"/>.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void UnRegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// Зарегистрировать генератор стаканов.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void RegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор стаканов, ранее зарегистрированный через <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// Зарегистрировать генератор лога заявок.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void RegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор лога заявок, ранее зарегистрированный через <see cref="RegisterOrderLog"/>.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void UnRegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
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

		///// <summary>
		///// Освободить занятые ресурсы.
		///// </summary>
		//protected override void DisposeManaged()
		//{
		//	if (State == EmulationStates.Started || State == EmulationStates.Suspended)
		//		Disconnect();

		//	base.DisposeManaged();
		//}

		private readonly SynchronizedDictionary<Security, CandleSeries> _series = new SynchronizedDictionary<Security, CandleSeries>();

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (UseExternalCandleSource && series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && (TimeSpan)series.Arg == MarketEmulator.Settings.UseCandlesTimeFrame)
			{
				yield return new Range<DateTimeOffset>(StartDate, StopDate.EndOfDay());
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