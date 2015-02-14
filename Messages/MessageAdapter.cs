namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый адаптер сообщений.
	/// </summary>
	/// <typeparam name="TSessionHolder">Тип контейнера сессии.</typeparam>
	public abstract class MessageAdapter<TSessionHolder> : Disposable, IMessageAdapter
		where TSessionHolder : class, IMessageSessionHolder
	{
		/// <summary>
		/// Состояния подключений.
		/// </summary>
		private enum ConnectionStates
		{
			/// <summary>
			/// Не активно.
			/// </summary>
			Disconnected,

			/// <summary>
			/// В процессе отключения.
			/// </summary>
			Disconnecting,

			/// <summary>
			/// В процессе подключения.
			/// </summary>
			Connecting,

			/// <summary>
			/// Подключение активно.
			/// </summary>
			Connected,

			/// <summary>
			/// Ошибка подключения.
			/// </summary>
			Failed,
		}

		/// <summary>
		/// Ожидание выполнения некоего действия, связанного с ключом.
		/// </summary>
		private class CodeTimeOut<T>
			//where T : class
		{
			private readonly CachedSynchronizedDictionary<T, TimeSpan> _registeredKeys = new CachedSynchronizedDictionary<T, TimeSpan>();

			private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

			/// <summary>
			/// Ограничение по времени, в течении которого должен отработать действие.
			/// </summary>
			/// <remarks>
			/// Значение по умолчанию равно 10 секундам.
			/// </remarks>
			public TimeSpan TimeOut
			{
				get { return _timeOut; }
				set
				{
					if (value <= TimeSpan.Zero)
						throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.IntervalMustBePositive);

					_timeOut = value;
				}
			}

			/// <summary>
			/// Запустить ожидание для ключа.
			/// </summary>
			/// <param name="key">Ключ.</param>
			public void StartTimeOut(T key)
			{
				if (key.IsNull(true))
					throw new ArgumentNullException("key");

				_registeredKeys.SafeAdd(key, s => TimeOut);
			}

			/// <summary>
			/// Обработать событие изменения рыночного времени и получить те ключи, ожидание для которых закончего.
			/// </summary>
			/// <param name="diff">Изменение рыночного времени.</param>
			/// <returns>Ключи.</returns>
			public IEnumerable<T> ProcessTime(TimeSpan diff)
			{
				if (_registeredKeys.Count == 0)
					return Enumerable.Empty<T>();

				return _registeredKeys.SyncGet(d =>
				{
					var timeOutCodes = new List<T>();

					foreach (var pair in d.CachedPairs)
					{
						d[pair.Key] -= diff;

						if (d[pair.Key] > TimeSpan.Zero)
							continue;

						timeOutCodes.Add(pair.Key);
						d.Remove(pair.Key);
					}

					return timeOutCodes;
				});
			}
		}

		private sealed class QuoteChangeDepthBuilder
		{
			private readonly Dictionary<SecurityId, QuoteChangeMessage> _feeds = new Dictionary<SecurityId, QuoteChangeMessage>();

			private readonly string _securityCode;
			private readonly string _boardCode;

			public QuoteChangeDepthBuilder(string securityCode, string boardCode)
			{
				_securityCode = securityCode;
				_boardCode = boardCode;
			}

			public QuoteChangeMessage Process(QuoteChangeMessage message)
			{
				_feeds[message.SecurityId] = message;

				var bids = _feeds.SelectMany(f => f.Value.Bids).ToArray();
				var asks = _feeds.SelectMany(f => f.Value.Asks).ToArray();

				return new QuoteChangeMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = _securityCode,
						BoardCode = _boardCode
					},
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					Bids = bids,
					Asks = asks
				};
			}
		}

		private sealed class Level1DepthBuilder
		{
			private readonly SecurityId _securityId;

			private readonly QuoteChange _bid = new QuoteChange(Sides.Buy, 0, 0);
			private readonly QuoteChange _ask = new QuoteChange(Sides.Sell, 0, 0);

			private DateTime _localTime;
			private DateTimeOffset _serverTime;

			public bool HasDepth { get; set; }

			public QuoteChangeMessage QuoteChange
			{
				get
				{
					var bids = _bid.Price == 0 || _bid.Volume == 0 ? Enumerable.Empty<QuoteChange>() : new[] { _bid.Clone() };
					var asks = _ask.Price == 0 || _ask.Volume == 0 ? Enumerable.Empty<QuoteChange>() : new[] { _ask.Clone() };

					return new QuoteChangeMessage
					{
						SecurityId = _securityId,
						ServerTime = _serverTime,
						LocalTime = _localTime,
						Bids = bids,
						Asks = asks
					};
				}
			}

			public Level1DepthBuilder(SecurityId securityId)
			{
				_securityId = securityId;

				_localTime = DateTime.MinValue;
				_serverTime = DateTimeOffset.MinValue;
			}

			public bool Process(Level1ChangeMessage message)
			{
				if (HasDepth)
					return false;

				var bidPrice = message.Changes.TryGetValue(Level1Fields.BestBidPrice);
				var askPrice = message.Changes.TryGetValue(Level1Fields.BestAskPrice);

				if (bidPrice == null && askPrice == null)
					return false;

				var bidVolume = message.Changes.TryGetValue(Level1Fields.BestBidVolume);
				var askVolume = message.Changes.TryGetValue(Level1Fields.BestAskVolume);

				_bid.Price = bidPrice == null ? 0 : (decimal)bidPrice;
				_bid.Volume = bidVolume == null ? 0 : (decimal)bidVolume;

				_ask.Price = askPrice == null ? 0 : (decimal)askPrice;
				_ask.Volume = askVolume == null ? 0 : (decimal)askVolume;

				_localTime = message.LocalTime;
				_serverTime = message.ServerTime;

				return true;
			}
		}

		private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new SynchronizedDictionary<string, QuoteChangeDepthBuilder>();
		private readonly SynchronizedDictionary<SecurityId, Level1DepthBuilder> _level1DepthBuilders = new SynchronizedDictionary<SecurityId, Level1DepthBuilder>();

		//private readonly bool _checkLicense;

		private Timer _marketTimeChangedTimer;
		private DateTime _heartbeatPrevTime;
		private DateTime _prevTime;

		// дополнительные состояния для ConnectionStates
		private const ConnectionStates _none = (ConnectionStates)0 - 1;
		private const ConnectionStates _reConnecting = (ConnectionStates)10;
		//private const ConnectionStates _reStartingExport = (ConnectionStates)11;

		private ConnectionStates _currState = _none;
		private ConnectionStates _prevState = _none;

		private int _connectingAttemptCount;
		private TimeSpan _connectionTimeOut;

		private readonly CodeTimeOut<long> _secLookupTimeOut = new CodeTimeOut<long>();
		private readonly CodeTimeOut<long> _pfLookupTimeOut = new CodeTimeOut<long>();

		private readonly SyncObject _timeSync = new SyncObject();
		private bool _canSendTimeIn;

		/// <summary>
		/// Инициализировать <see cref="MessageAdapter{TSessionHolder}"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		///// <param name="checkLicense">Проверять наличие лицензии.</param>
		protected MessageAdapter(MessageAdapterTypes type, TSessionHolder sessionHolder/*, bool checkLicense = true*/)
		{
			Platform = Platforms.AnyCPU;
			//_checkLicense = checkLicense;

			Type = type;
			TransactionIdGenerator = sessionHolder.TransactionIdGenerator;
			SessionHolder = sessionHolder;
		}

		private TSessionHolder _sessionHolder;

		/// <summary>
		/// Контейнер для сессии.
		/// </summary>
		public TSessionHolder SessionHolder
		{
			get { return _sessionHolder; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();
				
				_sessionHolder = value;
			}
		}

		IMessageSessionHolder IMessageAdapter.SessionHolder
		{
			get { return SessionHolder; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected virtual bool IsSupportNativeSecurityLookup
		{
			get { return false; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected virtual bool IsSupportNativePortfolioLookup
		{
			get { return false; }
		}

		/// <summary>
		/// Разрядность процесса, в котором может работать адаптер. По-умолчанию равно <see cref="Platforms.AnyCPU"/>.
		/// </summary>
		public Platforms Platform { get; protected set; }

		private IMessageProcessor _inMessageProcessor;

		/// <summary>
		/// Обработчик входящих сообщений.
		/// </summary>
		public IMessageProcessor InMessageProcessor
		{
			get { return _inMessageProcessor; }
			set
			{
				if (_inMessageProcessor == value)
					return;

				if (_inMessageProcessor != null)
					_inMessageProcessor.NewMessage -= OnInMessageProcessor;

				_inMessageProcessor = value;

				if (_inMessageProcessor == null)
					return;

				_inMessageProcessor.NewMessage += OnInMessageProcessor;
			}
		}

		private IMessageProcessor _outMessageProcessor;

		/// <summary>
		/// Обработчик исходящих сообщений.
		/// </summary>
		public IMessageProcessor OutMessageProcessor
		{
			get { return _outMessageProcessor; }
			set
			{
				if (_outMessageProcessor == value)
					return;

				if (_outMessageProcessor != null)
					_outMessageProcessor.NewMessage -= OnOutMessageProcessor;

				_outMessageProcessor = value;

				if (_outMessageProcessor == null)
					return;

				_outMessageProcessor.NewMessage += OnOutMessageProcessor;
			}
		}

		/// <summary>
		/// Метод для обработки входящих сообщений для <see cref="InMessageProcessor"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="adapter">Адаптер.</param>
		protected virtual void OnInMessageProcessor(Message message, IMessageAdapter adapter)
		{
			if (this != adapter)
				return;

			// при отключенном состоянии пропускаем только TimeMessage
			// остальные типы сообщений могут использоваться (например, в эмуляторе)
			if ((_currState == ConnectionStates.Disconnecting || _currState == ConnectionStates.Disconnected) && message.Type == MessageTypes.Time)
				return;

			switch (message.Type)
			{
				case MessageTypes.PortfolioLookup:
				{
					if (!IsSupportNativePortfolioLookup)
						_pfLookupTimeOut.StartTimeOut(((PortfolioLookupMessage)message).TransactionId);

					break;
				}
				case MessageTypes.SecurityLookup:
				{
					if (!IsSupportNativeSecurityLookup)
						_secLookupTimeOut.StartTimeOut(((SecurityLookupMessage)message).TransactionId);

					break;
				}
				case MessageTypes.Connect:
				{
					lock (_timeSync)
					{
						_canSendTimeIn = true;
						_currState = ConnectionStates.Connecting;	
					}

					if (_prevState == _none)
					{
						_connectionTimeOut = SessionHolder.ReConnectionSettings.TimeOutInterval;
						_connectingAttemptCount = SessionHolder.ReConnectionSettings.AttemptCount;
					}
					else
						_connectionTimeOut = SessionHolder.ReConnectionSettings.Interval;

					break;
				}
				case MessageTypes.Disconnect:
				{
					lock (_timeSync)
						_currState = ConnectionStates.Disconnecting;

					_connectionTimeOut = SessionHolder.ReConnectionSettings.TimeOutInterval;

					break;
				}
				case MessageTypes.Time:
				{
					lock (_timeSync)
						_canSendTimeIn = true;

					break;
				}
				case MessageTypes.ClearMessageQueue:
				{
					InMessageProcessor.Clear((ClearMessageQueueMessage)message);
					return;
				}
			}

			try
			{
				OnSendInMessage(message);
			}
			catch (Exception ex)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
						SendOutMessage(new ConnectMessage { Error = ex });
						return;

					case MessageTypes.Disconnect:
						SendOutMessage(new DisconnectMessage { Error = ex });
						return;

					case MessageTypes.OrderRegister:
					{
						var execMsg = ((OrderRegisterMessage)message).ToExecutionMessage();
						execMsg.Error = ex;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);

						return;
					}

					case MessageTypes.OrderReplace:
					{
						var execMsg = ((OrderReplaceMessage)message).ToExecutionMessage();
						execMsg.Error = ex;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);

						return;
					}

					case MessageTypes.OrderPairReplace:
					{
						var execMsg = ((OrderPairReplaceMessage)message).ToExecutionMessage();
						execMsg.Error = ex;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);

						return;
					}

					case MessageTypes.OrderCancel:
					{
						var execMsg = ((OrderCancelMessage)message).ToExecutionMessage();
						execMsg.Error = ex;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);

						return;
					}

					case MessageTypes.OrderGroupCancel:
					{
						var execMsg = ((OrderGroupCancelMessage)message).ToExecutionMessage();
						execMsg.Error = ex;
						execMsg.OrderState = OrderStates.Failed;
						SendOutMessage(execMsg);

						return;
					}

					case MessageTypes.MarketData:
					{
						var reply = (MarketDataMessage)message.Clone();
						reply.OriginalTransactionId = reply.TransactionId;
						reply.Error = ex;
						SendOutMessage(reply);
						return;
					}

					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
						SendOutMessage(new SecurityLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.PortfolioLookup:
					{
						var lookupMsg = (PortfolioLookupMessage)message;
						SendOutMessage(new PortfolioLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.ChangePassword:
					{
						var pwdMsg = (ChangePasswordMessage)message;
						SendOutMessage(new ChangePasswordMessage
						{
							OriginalTransactionId = pwdMsg.TransactionId,
							Error = ex
						});
						return;
					}
				}

				SendOutError(ex);
			}
		}

		/// <summary>
		/// Метод обработки исходящих сообщений для <see cref="OutMessageProcessor"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="adapter">Адаптер.</param>
		protected virtual void OnOutMessageProcessor(Message message, IMessageAdapter adapter)
		{
			if (this != adapter)
				return;

			switch (message.Type)
			{
				case MessageTypes.ClearMessageQueue:
					OutMessageProcessor.Clear((ClearMessageQueueMessage)message);
					return;

				case MessageTypes.Security:
				{
					NewOutMessage.SafeInvoke(message);

					if (!SessionHolder.CreateAssociatedSecurity)
						break;

					var clone = (SecurityMessage)message.Clone();
					clone.SecurityId = CloneSecurityId(clone.SecurityId);
					NewOutMessage.SafeInvoke(clone);

					break;
				}

				case MessageTypes.Level1Change:
				{
					NewOutMessage.SafeInvoke(message);

					var l1Msg = (Level1ChangeMessage)message;

					if (l1Msg.SecurityId.IsDefault())
						break;

					if (SessionHolder.CreateAssociatedSecurity)
					{
						// обновление BestXXX для ALL из конкретных тикеров
						var clone = (Level1ChangeMessage)l1Msg.Clone();
						clone.SecurityId = CloneSecurityId(clone.SecurityId);
						NewOutMessage.SafeInvoke(clone);
					}

					if (SessionHolder.CreateDepthFromLevel1)
					{
						// генерация стакана из Level1
						var builder = _level1DepthBuilders.SafeAdd(l1Msg.SecurityId, c => new Level1DepthBuilder(c));

						if (builder.Process(l1Msg))
						{
							var quoteMsg = builder.QuoteChange;

							NewOutMessage.SafeInvoke(quoteMsg);
							CreateAssociatedSecurityQuotes(quoteMsg);
						}
					}
					
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					NewOutMessage.SafeInvoke(quoteMsg);

					if (SessionHolder.CreateDepthFromLevel1)
						_level1DepthBuilders.SafeAdd(quoteMsg.SecurityId, c => new Level1DepthBuilder(c)).HasDepth = true;

					CreateAssociatedSecurityQuotes(quoteMsg);
					break;
				}

				case MessageTypes.Execution:
				{
					NewOutMessage.SafeInvoke(message);

					if (!SessionHolder.CreateAssociatedSecurity)
						break;

					var execMsg = (ExecutionMessage)message;

					if (execMsg.SecurityId.IsDefault())
						break;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.OrderLog:
						{
							var clone = (ExecutionMessage)message.Clone();
							clone.SecurityId = CloneSecurityId(clone.SecurityId);
							NewOutMessage.SafeInvoke(clone);
							break;
						}
					}

					break;
				}

				default:
					NewOutMessage.SafeInvoke(message);
					break;
			}
		}

		private void CreateAssociatedSecurityQuotes(QuoteChangeMessage quoteMsg)
		{
			if (!SessionHolder.CreateAssociatedSecurity)
				return;

			if (quoteMsg.SecurityId.IsDefault())
				return;

			var builder = _quoteChangeDepthBuilders
				.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, SessionHolder.AssociatedBoardCode));

			NewOutMessage.SafeInvoke(builder.Process(quoteMsg));
		}

		private SecurityId CloneSecurityId(SecurityId securityId)
		{
			return new SecurityId
			{
				SecurityCode = securityId.SecurityCode,
				BoardCode = SessionHolder.AssociatedBoardCode,
				SecurityType = securityId.SecurityType,
				Bloomberg = securityId.Bloomberg,
				Cusip = securityId.Cusip,
				IQFeed = securityId.IQFeed,
				InteractiveBrokers = securityId.InteractiveBrokers,
				Isin = securityId.Isin,
				Native = securityId.Native,
				Plaza = securityId.Plaza,
				Ric = securityId.Ric,
				Sedol = securityId.Sedol,
			};
		}

		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		public IdGenerator TransactionIdGenerator { get; private set; }

		/// <summary>
		/// Тип адаптера.
		/// </summary>
		public MessageAdapterTypes Type { get; private set; }

		/// <summary>
		/// Ограничение по времени, в течении которого должен отработать поиск инструментов или портфелей.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 10 секундам.
		/// </remarks>
		public TimeSpan LookupTimeOut
		{
			get { return _secLookupTimeOut.TimeOut; }
			set
			{
				_secLookupTimeOut.TimeOut = value;
				_pfLookupTimeOut.TimeOut = value;
			}
		}

		/// <summary>
		/// Событие получения исходящего сообщения.
		/// </summary>
		public event Action<Message> NewOutMessage;

		/// <summary>
		/// Отправить входящее сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public virtual void SendInMessage(Message message)
		{
			//if (!CheckLicense(message))
			//	return;

			if (message.Type == MessageTypes.Connect)
			{
				if (!Platform.IsCompatible())
				{
					SendOutMessage(new ConnectMessage
					{
						Error = new InvalidOperationException(LocalizedStrings.Str169Params.Put(GetType().Name, Platform))
					});

					return;
				}
			}

			//месседжи с заявками могут складываться из потока обработки
			var force = message.Type == MessageTypes.OrderRegister ||
			            message.Type == MessageTypes.OrderReplace ||
			            message.Type == MessageTypes.OrderPairReplace ||
			            message.Type == MessageTypes.OrderCancel ||
			            message.Type == MessageTypes.OrderGroupCancel;

			InitMessageLocalTime(message);

			_inMessageProcessor.EnqueueMessage(message, this, force);
		}

		//private bool CheckLicense(Message message)
		//{
		//	if (!_checkLicense)
		//		return true;

		//	switch (message.Type)
		//	{
		//		case MessageTypes.OrderRegister:
		//		{
		//			var regMsg = (OrderRegisterMessage)message;

		//			var msg = LicenseHelper.ValidateLicense(GetType(), regMsg.PortfolioName);

		//			if (msg != null)
		//			{
		//				SendOutMessage(new ExecutionMessage
		//				{
		//					OriginalTransactionId = regMsg.TransactionId,
		//					OrderState = OrderStates.Failed,
		//					ExecutionType = ExecutionTypes.Order,
		//					Error = new InvalidOperationException(msg)
		//				});

		//				return false;
		//			}
					
		//			break;
		//		}
		//		case MessageTypes.Connect:
		//		{
		//			if (_checkLicense)
		//			{
		//				var msg = GetType().ValidateLicense();

		//				if (msg != null)
		//				{
		//					SendOutMessage(new ConnectMessage { Error = new InvalidOperationException(msg) });
		//					return false;	
		//				}
		//			}

		//			break;
		//		}
		//	}

		//	return true;
		//}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected abstract void OnSendInMessage(Message message);

		/// <summary>
		/// Добавить <see cref="Message"/> в исходящую очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public virtual void SendOutMessage(Message message)
		{
			InitMessageLocalTime(message);

			_outMessageProcessor.EnqueueMessage(message, this, false);

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (((ConnectMessage)message).Error == null)
					{
						lock (_timeSync)
							_prevState = _currState = ConnectionStates.Connected;

						StartMarketTimer();
					}
					else
					{
						lock (_timeSync)
							_prevState = _currState = ConnectionStates.Failed;
					}

					break;
				}
				case MessageTypes.Disconnect:
				{
					lock (_timeSync)
						_prevState = _currState = ConnectionStates.Disconnected;

					StopMarketTimer();
					break;
				}
				default:
				{
					if (_prevTime != DateTime.MinValue)
					{
						var diff = message.LocalTime - _prevTime;

						if (message.Type != MessageTypes.Time && diff >= SessionHolder.MarketTimeChangedInterval)
						{
							SendOutMessage(new TimeMessage
							{
								LocalTime = message.LocalTime,
								ServerTime = message.GetServerTime(),
							});
						}

						_secLookupTimeOut
							.ProcessTime(diff)
							.ForEach(id => SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = id }));

						_pfLookupTimeOut
							.ProcessTime(diff)
							.ForEach(id => SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = id }));

						ProcessReconnection(diff);
					}

					_prevTime = message.LocalTime;
					break;
				}
			}
		}

		/// <summary>
		/// Инициализировать метку локального времени для <see cref="Message"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		private void InitMessageLocalTime(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = SessionHolder.CurrentTime.LocalDateTime;
		}

		/// <summary>
		/// Создать сообщение <see cref="ErrorMessage"/> и передать его в метод <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="description">Описание ошибки.</param>
		protected void SendOutError(string description)
		{
			SendOutError(new InvalidOperationException(description));
		}

		/// <summary>
		/// Создать сообщение <see cref="ErrorMessage"/> и передать его в метод <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="error">Описание ошибки.</param>
		protected void SendOutError(Exception error)
		{
			SendOutMessage(new ErrorMessage { Error = error });
		}

		/// <summary>
		/// Создать сообщение <see cref="SecurityMessage"/> и передать его в метод <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		protected void SendOutSecurityMessage(SecurityId securityId)
		{
			SendOutMessage(new SecurityMessage { SecurityId = securityId });
		}

		/// <summary>
		/// Нужно ли отправлять в адаптер сообщение типа <see cref="TimeMessage"/>.
		/// </summary>
		protected virtual bool CanSendTimeMessage
		{
			get { return false; }
		}

		/// <summary>
		/// Запустить таймер генерации с интервалом <see cref="IMessageSessionHolder.MarketTimeChangedInterval"/> сообщений <see cref="TimeMessage"/>.
		/// </summary>
		protected virtual void StartMarketTimer()
		{
			if (null != _marketTimeChangedTimer)
				return;

			_marketTimeChangedTimer = ThreadingHelper
				.Timer(() =>
				{
					var time = SessionHolder.CurrentTime;

					if (Type == MessageAdapterTypes.MarketData)
					{
						// TimeMsg нужен для оповещения внешнего кода о живом адаптере (или для изменения текущего времени)
						// Поэтому когда в очереди есть другие сообщения нет смысла добавлять еще и TimeMsg
						if (_outMessageProcessor.MessageCount == 0)
							SendOutMessage(new TimeMessage());
					}

					TimeMessage timeMsg;

					lock (_timeSync)
					{
						if (_currState != ConnectionStates.Connected)
							return;

						if (CanSendTimeMessage)
						{
							// TimeMsg нужно отправлять в очередь, если предыдущее сообщение было обработано.
							// Иначе, из-за медленной обработки, кол-во TimeMsg может вырасти до большого значения.
						
							if (!_canSendTimeIn)
								return;

							_canSendTimeIn = false;

							timeMsg = new TimeMessage();
						}
						else
						{
							if (_heartbeatPrevTime.IsDefault())
							{
								_heartbeatPrevTime = time.LocalDateTime;
								return;
							}
							
							if ((time - _heartbeatPrevTime) < SessionHolder.HeartbeatInterval)
								return;

							timeMsg = new TimeMessage { TransactionId = TransactionIdGenerator.GetNextId().To<string>() };

							_heartbeatPrevTime = time.LocalDateTime;
						}
					}

					SendInMessage(timeMsg);
				})
				.Interval(SessionHolder.MarketTimeChangedInterval);
		}

		/// <summary>
		/// Остановить таймер, запущенный ранее через <see cref="StartMarketTimer"/>.
		/// </summary>
		protected void StopMarketTimer()
		{
			if (null == _marketTimeChangedTimer)
				return;

			_marketTimeChangedTimer.Dispose();
			_marketTimeChangedTimer = null;
		}

		private void ProcessReconnection(TimeSpan diff)
		{
			switch (_currState)
			{
				case ConnectionStates.Disconnecting:
				case ConnectionStates.Connecting:
				{
					_connectionTimeOut -= diff;

					if (_connectionTimeOut <= TimeSpan.Zero)
					{
						SessionHolder.AddWarningLog("RCM: Connecting Timeout Left {0}.", _connectionTimeOut);

						switch (_currState)
						{
							case ConnectionStates.Connecting:
								SendOutMessage(new ConnectMessage { Error = new TimeoutException(LocalizedStrings.Str170) });
								break;
							case ConnectionStates.Disconnecting:
								SendOutMessage(new DisconnectMessage { Error = new TimeoutException(LocalizedStrings.Str171) });
								break;
						}

						if (_prevState != _none)
						{
							SessionHolder.AddInfoLog("RCM: Connecting AttemptError.");

							//ReConnectionSettings.RaiseAttemptError(new TimeoutException(message));
							lock (_timeSync)
								_currState = _prevState;
						}
						else
						{
							//ReConnectionSettings.RaiseTimeOut();

							if (_currState == ConnectionStates.Connecting && _connectingAttemptCount > 0)
							{
								lock (_timeSync)
									_currState = _reConnecting;

								SessionHolder.AddInfoLog("RCM: To Reconnecting Attempts {0} Timeout {1}.", _connectingAttemptCount, _connectionTimeOut);
							}
							else
							{
								lock (_timeSync)
									_currState = _none;
							}
						}
					}

					break;
				}
				case _reConnecting:
				{
					if (_connectingAttemptCount == 0)
					{
						SessionHolder.AddWarningLog("RCM: Reconnecting attemts {0} PrevState {1}.", _connectingAttemptCount, _prevState);

						lock (_timeSync)
							_currState = _none;

						break;
					}

					_connectionTimeOut -= diff;

					if (_connectionTimeOut > TimeSpan.Zero)
						break;

					if (IsTradeTime())
					{
						SessionHolder.AddInfoLog("RCM: To Connecting. CurrState {0} PrevState {1} Attempts {2}.", _currState, _prevState, _connectingAttemptCount);

						if (_connectingAttemptCount != -1)
							_connectingAttemptCount--;

						_connectionTimeOut = SessionHolder.ReConnectionSettings.Interval;

						_prevState = _currState;
						SendInMessage(new ConnectMessage());
					}
					else
					{
						SessionHolder.AddWarningLog("RCM: Out of trade time. CurrState {0}.", _currState);
						_connectionTimeOut = TimeSpan.FromMinutes(1);
					}

					break;
				}
			}
		}

		private bool IsTradeTime()
		{
			// TODO
			return true;
			//return SessionHolder.ReConnectionSettings.WorkingTime.IsTradeTime(TimeHelper.Now);
		}
	}

	/// <summary>
	/// Специальный адаптер, который передает сразу на выход все входящие сообщения.
	/// </summary>
	public class PassThroughMessageAdapter : MessageAdapter<IMessageSessionHolder>
	{
		/// <summary>
		/// Создать <see cref="PassThroughMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public PassThroughMessageAdapter(IMessageSessionHolder sessionHolder)
			: base(MessageAdapterTypes.Transaction, sessionHolder)
		{
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			SendOutMessage(message);
		}
	}
}