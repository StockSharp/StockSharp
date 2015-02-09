namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер, получающий сообщения из хранилища <see cref="IStorageRegistry"/>.
	/// </summary>
	public class HistoryMessageAdapter : MessageAdapter<HistorySessionHolder>
	{
		private readonly CachedSynchronizedDictionary<SecurityId, MarketDepthGenerator> _depthGenerators = new CachedSynchronizedDictionary<SecurityId, MarketDepthGenerator>();
		private readonly CachedSynchronizedDictionary<SecurityId, TradeGenerator> _tradeGenerators = new CachedSynchronizedDictionary<SecurityId, TradeGenerator>();
		private readonly CachedSynchronizedDictionary<SecurityId, OrderLogGenerator> _orderLogGenerators = new CachedSynchronizedDictionary<SecurityId, OrderLogGenerator>();

		private readonly BasketMarketDataStorage<Message> _basketStorage = new BasketMarketDataStorage<Message>();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly HistorySessionHolder _sessionHolder;

		private Thread _loadingThread;
		private bool _running;
		private bool _disconnecting;

		private IEnumerable<ExchangeBoard> Boards
		{
			get
			{
				return _sessionHolder
					.SecurityProvider
					.LookupAll()
					.Select(s => s.Board)
					.Distinct();
			}
		}

		/// <summary>
		/// Число загруженных событий.
		/// </summary>
		public int LoadedEventCount { get; private set; }

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

		/// <summary>
		/// Максимальный размер очереди сообщений, до которого читаются исторические данные.
		/// </summary>
		public int MaxMessageCount { get; set; }

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
		/// Генераторы стаканов.
		/// </summary>
		public IDictionary<SecurityId, MarketDepthGenerator> DepthGenerators { get { return _depthGenerators; } }

		/// <summary>
		/// Генераторы сделок.
		/// </summary>
		public IDictionary<SecurityId, TradeGenerator> TradeGenerators { get { return _tradeGenerators; } }

		/// <summary>
		/// Генераторы лога заявок.
		/// </summary>
		public IDictionary<SecurityId, OrderLogGenerator> OrderLogGenerators { get { return _orderLogGenerators; } }

		/// <summary>
		/// Хранилище-агрегатор.
		/// </summary>
		public BasketMarketDataStorage<Message> BasketStorage { get { return _basketStorage; } }

		/// <summary>
		/// Создать <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии, внутри которой происходит обработка сообщений.</param>
		public HistoryMessageAdapter(HistorySessionHolder sessionHolder)
			: base(MessageAdapterTypes.MarketData, sessionHolder)
		{
			_sessionHolder = sessionHolder;

			_basketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<TimeMessage>(d => GetTimeLine(d)));

			MaxMessageCount = 1000;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_basketStorage.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// Запустить таймер генерации с интервалом <see cref="IMessageSessionHolder.MarketTimeChangedInterval"/> сообщений <see cref="TimeMessage"/>.
		/// </summary>
		protected override void StartMarketTimer()
		{
		}

		/// <summary>
		/// Метод обработки исходящих сообщений.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="adapter">Адаптер.</param>
		protected override void OnOutMessageProcessor(Message message, IMessageAdapter adapter)
		{
			base.OnOutMessageProcessor(message, adapter);

			lock (_syncRoot)
			{
				if (_running && OutMessageProcessor.MessageCount < MaxMessageCount)
					_syncRoot.Pulse();
			}
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (_loadingThread != null)
						throw new InvalidOperationException(LocalizedStrings.Str1116);

					LoadedEventCount = 0;
					_running = true;
					_disconnecting = false;

					_loadingThread = ThreadingHelper
						.Thread(OnLoad)
						.Name("HistoryMessageAdapter. Loader thread")
						.Launch();

					SendOutMessage(new ConnectMessage());

					return;
				}

				case MessageTypes.Disconnect:
				{
					var running = _running;

					_running = false;

					if (_loadingThread == null)
					{
						// отправляем LastMessage только если не отправили его из OnLoad
						if (!running)
							SendOutMessage(new LastMessage());

						SendOutMessage(new DisconnectMessage());
					}
					else
					{
						// DisconnectMessage должен быть отправлен самым последним
						_disconnecting = true;
						_syncRoot.Pulse();
					}

					return;
				}

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					return;
			}

			SendOutMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var security = _sessionHolder.SecurityProvider.LookupById(message.SecurityId.SecurityCode + "@" + message.SecurityId.BoardCode);

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
						_basketStorage.InnerStorages.Add(StorageRegistry.GetLevel1MessageStorage(security, Drive, StorageFormat));

						_basketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<ClearingMessage>(date => new[]
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
						_basketStorage.InnerStorages.Add((IMarketDataStorage<QuoteChangeMessage>)StorageRegistry.GetMarketDepthStorage(security, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<QuoteChangeMessage>>(security, MessageTypes.QuoteChange, message.Arg);
					
					break;
				}

				case MarketDataTypes.Trades:
				{
					if (message.IsSubscribe)
						_basketStorage.InnerStorages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetTradeStorage(security, Drive, StorageFormat));
					else
						RemoveStorage<IMarketDataStorage<ExecutionMessage>>(security, MessageTypes.Execution, message.Arg);
					
					break;
				}

				case MarketDataTypes.OrderLog:
				{
					if (message.IsSubscribe)
					{
						//var msg = "OrderLog".ValidateLicense();

						//if (msg == null)
						_basketStorage.InnerStorages.Add((IMarketDataStorage<ExecutionMessage>)StorageRegistry.GetOrderLogStorage(security, Drive, StorageFormat));
						//else
						//	SessionHolder.AddErrorLog(msg);	
					}
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
						_basketStorage.InnerStorages.Add(StorageRegistry.GetCandleMessageStorage(candleMessageType, security, message.Arg, Drive, StorageFormat));
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
			var storage = _basketStorage
				.InnerStorages
				.OfType<T>()
				.FirstOrDefault(s => s.Security == security && s.Arg.Compare(arg) == 0);

			if (storage != null)
				_basketStorage.InnerStorages.Remove(storage);

			SendOutMessage(new ClearMessageQueueMessage
			{
				MessageTypes = messageType,
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
				var loadDate = _sessionHolder.StartDate;

				EnqueueGenerators(_tradeGenerators, MarketDataTypes.Trades);
				EnqueueGenerators(_depthGenerators, MarketDataTypes.MarketDepth);
				EnqueueGenerators(_orderLogGenerators, MarketDataTypes.OrderLog);

				var messageTypes = new[] { MessageTypes.Time, ExtendedMessageTypes.Clearing };

				while (loadDate.Date <= _sessionHolder.StopDate.Date && _running)
				{
					if (Boards.Any(b => b.IsTradeDate(loadDate, true)))
					{
						SessionHolder.AddInfoLog("Loading {0} Events: {1}", loadDate.Date, LoadedEventCount);

						var enumerator = _basketStorage.Load(loadDate.Date);

						// хранилище за указанную дату содержит только время и клиринг
						var noData = !enumerator.DataTypes.Except(messageTypes).Any();

						if (noData)
							SendOutMessages(loadDate, GetSimpleTimeLine(loadDate).GetEnumerator());
						else
							SendOutMessages(loadDate, enumerator);
					}

					loadDate = loadDate.Date.AddDays(1);
				}

				SendOutMessage(new LastMessage { LocalTime = _sessionHolder.StopDate.LocalDateTime });
			}
			catch (Exception ex)
			{
				SendOutError(ex);
				SendOutMessage(new LastMessage { IsError = true });
			}

			if (_disconnecting)
				SendOutMessage(new DisconnectMessage());

			_loadingThread = null;
		}

		private void SendOutMessages(DateTimeOffset loadDate, IEnumerator<Message> enumerator)
		{
			var checkFromTime = loadDate.Date == _sessionHolder.StartDate.Date && loadDate.Date != loadDate;
			var checkToTime = loadDate.Date == _sessionHolder.StopDate.Date;

			while (enumerator.MoveNext() && _running)
			{
				var msg = enumerator.Current;

				msg.LocalTime = msg.GetServerTime().LocalDateTime;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < _sessionHolder.StartDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > _sessionHolder.StopDate)
						break;
				}

				LoadedEventCount++;
				SendOutMessage(msg);

				lock (_syncRoot)
				{
					if (OutMessageProcessor.MessageCount > MaxMessageCount)
						_syncRoot.Wait();
				}
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
					TransactionId = SessionHolder.TransactionIdGenerator.GetNextId(),
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
				for (var time = range.Item2.Min; time <= range.Item2.Max; time += SessionHolder.MarketTimeChangedInterval)
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
				lastTime += SessionHolder.MarketTimeChangedInterval;

				if (lastTime > TimeHelper.LessOneDay)
					break;

				yield return new TimeMessage
				{
					ServerTime = GetTime(date, lastTime)
				};
			}
		}
	}
}