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

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер, получающий сообщения из хранилища <see cref="IStorageRegistry"/>.
	/// </summary>
	public class HistoryMessageAdapter : MessageAdapter
	{
		private readonly CachedSynchronizedDictionary<SecurityId, MarketDepthGenerator> _depthGenerators = new CachedSynchronizedDictionary<SecurityId, MarketDepthGenerator>();
		private readonly CachedSynchronizedDictionary<SecurityId, TradeGenerator> _tradeGenerators = new CachedSynchronizedDictionary<SecurityId, TradeGenerator>();
		private readonly CachedSynchronizedDictionary<SecurityId, OrderLogGenerator> _orderLogGenerators = new CachedSynchronizedDictionary<SecurityId, OrderLogGenerator>();

		private Thread _loadingThread;
		private bool _disconnecting;

		private IEnumerable<ExchangeBoard> Boards
		{
			get
			{
				return SecurityProvider
					.LookupAll()
					.Select(s => s.Board)
					.Distinct();
			}
		}

		/// <summary>
		/// Число загруженных событий.
		/// </summary>
		public int LoadedMessageCount { get; private set; }

		private int _postTradeMarketTimeChangedCount = 2;

		/// <summary>
		/// Количество вызовов события <see cref="IConnector.MarketTimeChanged"/> после окончания торгов. По-умолчанию равно 2.
		/// </summary>
		/// <remarks>
		/// Необходимо для активации пост-трейд правил (правила, которые опираются на события, происходящие после окончания торгов).
		/// </remarks>
		public int PostTradeMarketTimeChangedCount
		{
			get { return _postTradeMarketTimeChangedCount; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_postTradeMarketTimeChangedCount = value;
			}
		}

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// Хранилище данных.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _storageRegistry; }
			set
			{
				_storageRegistry = value;

				if (value != null)
					Drive = value.DefaultDrive;
			}
		}

		private IMarketDataDrive _drive;

		/// <summary>
		/// Хранилище, которое используется по-умолчанию. По умолчанию используется <see cref="IStorageRegistry.DefaultDrive"/>.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get { return _drive; }
			set
			{
				if (value == null && StorageRegistry != null)
					throw new ArgumentNullException();

				_drive = value;
			}
		}

		/// <summary>
		/// Формат маркет-данных. По умолчанию используется <see cref="StorageFormats.Binary"/>.
		/// </summary>
		public StorageFormats StorageFormat { get; set; }

		/// <summary>
		/// Хранилище-агрегатор.
		/// </summary>
		public BasketMarketDataStorage<Message> BasketStorage { get; private set; }

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; private set; }

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromSeconds(1);

		/// <summary>
		/// Интервал генерации сообщения <see cref="TimeMessage"/>. По-умолчанию равно 1 секунде.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// Создать <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			BasketStorage = new BasketMarketDataStorage<Message>();
			BasketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<TimeMessage>(d => GetTimeLine(d)));

			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;
		}

		/// <summary>
		/// Создать <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
			: this(transactionIdGenerator)
		{
			SecurityProvider = securityProvider;
			
			this.AddMarketDataSupport();
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState);
			this.AddSupportedMessage(ExtendedMessageTypes.Generator);
		}

		/// <summary>
		/// Дата в истории, с которой необходимо начать эмуляцию.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		private DateTimeOffset _currentTime;

		/// <summary>
		/// Текущее время.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get { return _currentTime; }
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			BasketStorage.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					LoadedMessageCount = 0;
					_disconnecting = _loadingThread != null;
					_loadingThread = null;

					if (!_disconnecting)
						SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_loadingThread != null)
						throw new InvalidOperationException(LocalizedStrings.Str1116);

					SendOutMessage(new ConnectMessage { LocalTime = StartDate.LocalDateTime });
					return;
				}

				case MessageTypes.Disconnect:
				{
					_disconnecting = true;
					return;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					SecurityProvider.LookupAll().ForEach(security =>
					{
						SendOutMessage(security.Board.ToMessage());

						var secMsg = security.ToMessage();
						secMsg.OriginalTransactionId = lookupMsg.TransactionId;
						SendOutMessage(secMsg);

						//SendOutMessage(new Level1ChangeMessage { SecurityId = security.ToSecurityId() }
						//	.Add(Level1Fields.StepPrice, security.StepPrice)
						//	.Add(Level1Fields.MinPrice, security.MinPrice)
						//	.Add(Level1Fields.MaxPrice, security.MaxPrice)
						//	.Add(Level1Fields.MarginBuy, security.MarginBuy)
						//	.Add(Level1Fields.MarginSell, security.MarginSell));
					});
					SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
					return;
				}

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					return;

				case ExtendedMessageTypes.EmulationState:
					var stateMsg = (EmulationStateMessage)message;

					switch (stateMsg.State)
					{
						case EmulationStates.Starting:
							if (_loadingThread != null)
								break;

							_loadingThread = ThreadingHelper
								.Thread(OnLoad)
								.Name("HistoryMessageAdapter")
								.Launch();

							break;
					}

					SendOutMessage(message);
					return;
			}

			//SendOutMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var generatorMessage = message as GeneratorMarketDataMessage;

			if (generatorMessage != null)
			{
				if (generatorMessage.Generator == null)
					throw new ArgumentException("message");

				var tradeGen = generatorMessage.Generator as TradeGenerator;

				if (tradeGen != null)
				{
					if (generatorMessage.IsSubscribe)
						_tradeGenerators.Add(generatorMessage.SecurityId, tradeGen);
					else
						_tradeGenerators.Remove(generatorMessage.SecurityId);
				}
				else
				{
					var depthGen = generatorMessage.Generator as MarketDepthGenerator;

					if (depthGen != null)
					{
						if (generatorMessage.IsSubscribe)
							_depthGenerators.Add(generatorMessage.SecurityId, depthGen);
						else
							_depthGenerators.Remove(generatorMessage.SecurityId);
					}
					else
					{
						var olGen = generatorMessage.Generator as OrderLogGenerator;

						if (olGen != null)
						{
							if (generatorMessage.IsSubscribe)
								_orderLogGenerators.Add(generatorMessage.SecurityId, olGen);
							else
								_orderLogGenerators.Remove(generatorMessage.SecurityId);
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
				}

				return;
			}

			var security = SecurityProvider.LookupById(message.SecurityId.SecurityCode + "@" + message.SecurityId.BoardCode);

			if (security == null)
			{
				RaiseMarketDataMessage(message,  new InvalidOperationException(LocalizedStrings.Str704Params.Put(message.SecurityId)));
				return;
			}

			if (TryGetGenerator(message) != null)
			{
				RaiseMarketDataMessage(message, null);
				return;
			}

			if (StorageRegistry == null)
			{
				RaiseMarketDataMessage(message, new InvalidOperationException(LocalizedStrings.Str1117Params.Put(message.DataType, message.SecurityId)));
				return;
			}

			Exception error = null;

			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
				{
					if (message.IsSubscribe)
					{
						BasketStorage.InnerStorages.Add(StorageRegistry.GetLevel1MessageStorage(security, Drive, StorageFormat));

						BasketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<ClearingMessage>(date => new[]
						{
							new ClearingMessage
							{
								LocalTime = date.Date + security.Board.ExpiryTime,
								SecurityId = message.SecurityId,
								ClearMarketDepth = true
							}
						}));
					}
					else
					{
						RemoveStorage<IMarketDataStorage<Level1ChangeMessage>>(security, MessageTypes.Level1Change, message.Arg);
						RemoveStorage<InMemoryMarketDataStorage<ClearingMessage>>(security, ExtendedMessageTypes.Clearing, message.Arg);
					}

					break;
				}

				case MarketDataTypes.MarketDepth:
				{
					if (message.IsSubscribe)
						BasketStorage.InnerStorages.Add((IMarketDataStorage<QuoteChangeMessage>)StorageRegistry.GetMarketDepthStorage(security, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<QuoteChangeMessage>>(security, MessageTypes.QuoteChange, message.Arg);
					
					break;
				}

				case MarketDataTypes.Trades:
				{
					if (message.IsSubscribe)
						BasketStorage.InnerStorages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetTradeStorage(security, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, message.Arg);
					
					break;
				}

				case MarketDataTypes.OrderLog:
				{
					if (message.IsSubscribe)
						BasketStorage.InnerStorages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetOrderLogStorage(security, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, message.Arg);

					break;
				}

				case MarketDataTypes.CandleTimeFrame:
				case MarketDataTypes.CandleTick:
				case MarketDataTypes.CandleVolume:
				case MarketDataTypes.CandleRange:
				case MarketDataTypes.CandlePnF:
				case MarketDataTypes.CandleRenko:
				{
					Type candleMessageType;
					MessageTypes msgType;

					switch (message.DataType)
					{
						case MarketDataTypes.CandleTimeFrame:
							msgType = MessageTypes.CandleTimeFrame;
							candleMessageType = typeof(TimeFrameCandleMessage);
							break;
						case MarketDataTypes.CandleTick:
							msgType = MessageTypes.CandleTick;
							candleMessageType = typeof(TickCandleMessage);
							break;
						case MarketDataTypes.CandleVolume:
							msgType = MessageTypes.CandleVolume;
							candleMessageType = typeof(VolumeCandleMessage);
							break;
						case MarketDataTypes.CandleRange:
							msgType = MessageTypes.CandleRange;
							candleMessageType = typeof(RangeCandleMessage);
							break;
						case MarketDataTypes.CandlePnF:
							msgType = MessageTypes.CandlePnF;
							candleMessageType = typeof(PnFCandleMessage);
							break;
						case MarketDataTypes.CandleRenko:
							msgType = MessageTypes.CandleRenko;
							candleMessageType = typeof(RenkoCandleMessage);
							break;
						default:
							throw new InvalidOperationException();
					}

					if (message.IsSubscribe)
						BasketStorage.InnerStorages.Add(StorageRegistry.GetCandleMessageStorage(candleMessageType, security, message.Arg, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<CandleMessage>>(security, msgType, message.Arg);

					break;
				}

				default:
					error = new InvalidOperationException(LocalizedStrings.Str1118Params.Put(message.DataType));
					break;
			}

			RaiseMarketDataMessage(message, error);
		}

		private void RemoveStorage<T>(Security security, MessageTypes messageType, object arg)
			where T : class, IMarketDataStorage
		{
			var storage = BasketStorage
				.InnerStorages
				.OfType<T>()
				.FirstOrDefault(s => s.Security == security && s.Arg.Compare(arg) == 0);

			if (storage != null)
				BasketStorage.InnerStorages.Remove(storage);

			SendOutMessage(new ClearQueueMessage
			{
				ClearMessageType = messageType,
				SecurityId = security.ToSecurityId(),
				Arg = arg
			});
		}

		private MarketDataGenerator TryGetGenerator(MarketDataMessage message)
		{
			switch (message.DataType)
			{
				case MarketDataTypes.Trades:
					return _tradeGenerators.TryGetValue(message.SecurityId);

				case MarketDataTypes.MarketDepth:
					return _depthGenerators.TryGetValue(message.SecurityId);

				case MarketDataTypes.OrderLog:
					return _orderLogGenerators.TryGetValue(message.SecurityId);

				default:
					return null;
			}
		}

		private void RaiseMarketDataMessage(MarketDataMessage message, Exception error)
		{
			var reply = (MarketDataMessage)message.Clone();
			reply.OriginalTransactionId = message.TransactionId;
			reply.Error = error;
			SendOutMessage(reply);
		}

		private void OnLoad()
		{
			try
			{
				var loadDate = StartDate;

				EnqueueGenerators(_tradeGenerators.CachedPairs, MarketDataTypes.Trades);
				EnqueueGenerators(_depthGenerators.CachedPairs, MarketDataTypes.MarketDepth);
				EnqueueGenerators(_orderLogGenerators.CachedPairs, MarketDataTypes.OrderLog);

				var messageTypes = new[] { MessageTypes.Time, ExtendedMessageTypes.Clearing };

				while (loadDate.Date <= StopDate.Date && !_disconnecting)
				{
					if (Boards.Any(b => b.IsTradeDate(loadDate, true)))
					{
						this.AddInfoLog("Loading {0} Events: {1}", loadDate.Date, LoadedMessageCount);

						var enumerator = BasketStorage.Load(loadDate.Date);

						// хранилище за указанную дату содержит только время и клиринг
						var noData = !enumerator.DataTypes.Except(messageTypes).Any();

						if (noData)
							SendOutMessages(loadDate, GetSimpleTimeLine(loadDate).GetEnumerator());
						else
							SendOutMessages(loadDate, enumerator);
					}

					loadDate = loadDate.Date.AddDays(1);
				}

				SendOutMessage(new LastMessage { LocalTime = StopDate.LocalDateTime });
			}
			catch (Exception ex)
			{
				SendOutError(ex);
				SendOutMessage(new LastMessage { IsError = true });
			}

			if (_disconnecting)
				SendOutMessage(new DisconnectMessage());

			_disconnecting = false;

			if (_loadingThread == null)
				SendOutMessage(new ResetMessage());

			_loadingThread = null;
		}

		/// <summary>
		/// Отправить исходящее сообщение, вызвав событие <see cref="MessageAdapter.NewOutMessage"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public override void SendOutMessage(Message message)
		{
			LoadedMessageCount++;
			
			var serverTime = message.GetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			base.SendOutMessage(message);
		}

		private void SendOutMessages(DateTimeOffset loadDate, IEnumerator<Message> enumerator)
		{
			var checkFromTime = loadDate.Date == StartDate.Date && loadDate.Date != loadDate;
			var checkToTime = loadDate.Date == StopDate.Date;

			while (enumerator.MoveNext() && !_disconnecting)
			{
				var msg = enumerator.Current;

				var serverTime = msg.GetServerTime();

				if (serverTime == null)
					throw new InvalidOperationException();

				msg.LocalTime = serverTime.Value.LocalDateTime;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < StartDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > StopDate)
						break;
				}

				SendOutMessage(msg);
			}
		}

		private void EnqueueGenerators<TGenerator>(IEnumerable<KeyValuePair<SecurityId, TGenerator>> generators, MarketDataTypes type)
			where TGenerator : MarketDataGenerator
		{
			foreach (var pair in generators)
			{
				SendOutMessage(new GeneratorMessage
				{
					SecurityId = pair.Key,
					Generator = pair.Value,
					IsSubscribe = true,
					TransactionId = TransactionIdGenerator.GetNextId(),
					DataType = type,
				});
			}
		}

		private IEnumerable<Tuple<ExchangeBoard, Range<TimeSpan>>> GetOrderedRanges(DateTimeOffset date)
		{
			var orderedRanges = Boards
				.Where(b => b.IsTradeDate(date, true))
				.SelectMany(board =>
				{
					var period = board.WorkingTime.GetPeriod(board.Exchange.ToExchangeTime(date));

					return period == null || period.Times.Length == 0
						? new[] { Tuple.Create(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)) }
						: period.Times.Select(t => Tuple.Create(board, ToUtc(board, t)));
				})
				.OrderBy(i => i.Item2.Min)
				.ToList();

			for (var i = 0; i < orderedRanges.Count - 1; )
			{
				if (orderedRanges[i].Item2.Contains(orderedRanges[i + 1].Item2))
				{
					orderedRanges.RemoveAt(i + 1);
				}
				else if (orderedRanges[i + 1].Item2.Contains(orderedRanges[i].Item2))
				{
					orderedRanges.RemoveAt(i);
				}
				else if (orderedRanges[i].Item2.Intersect(orderedRanges[i + 1].Item2) != null)
				{
					orderedRanges[i] = Tuple.Create(orderedRanges[i].Item1, new Range<TimeSpan>(orderedRanges[i].Item2.Min, orderedRanges[i + 1].Item2.Max));
					orderedRanges.RemoveAt(i + 1);
				}
				else
					i++;
			}

			return orderedRanges;
		}

		private static Range<TimeSpan> ToUtc(ExchangeBoard board, Range<TimeSpan> range)
		{
			var min = DateTime.MinValue + range.Min;
			var max = DateTime.MinValue + range.Max;

			var utcMin = min.To(board.Exchange.TimeZoneInfo);
			var utcMax = max.To(board.Exchange.TimeZoneInfo);

			return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
		}

		private IEnumerable<TimeMessage> GetTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				for (var time = range.Item2.Min; time <= range.Item2.Max; time += MarketTimeChangedInterval)
				{
					var serverTime = GetTime(date, time);

					if (serverTime.Date < date)
						continue;

					lastTime = serverTime.TimeOfDay;
					yield return new TimeMessage { ServerTime = serverTime };
				}
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private IEnumerable<TimeMessage> GetSimpleTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				var time = GetTime(date.Date, range.Item2.Min);
				if (time.Date >= date)
					yield return new TimeMessage { ServerTime = time };

				time = GetTime(date.Date, range.Item2.Max);
				if (time.Date >= date)
					yield return new TimeMessage { ServerTime = time };

				lastTime = range.Item2.Max;
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private static DateTimeOffset GetTime(DateTimeOffset date, TimeSpan timeOfDay)
		{
			return new DateTime(date.Date.Ticks + timeOfDay.Ticks).ApplyTimeZone(date.Offset);
		}

		private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTimeOffset date, TimeSpan lastTime)
		{
			for (var i = 0; i < PostTradeMarketTimeChangedCount; i++)
			{
				lastTime += MarketTimeChangedInterval;

				if (lastTime > TimeHelper.LessOneDay)
					break;

				yield return new TimeMessage
				{
					ServerTime = GetTime(date, lastTime)
				};
			}
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}