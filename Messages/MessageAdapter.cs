namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Базовый адаптер, конвертирующий сообщения <see cref="Message"/> в команды торговой системы и обратно.
	/// </summary>
	public abstract class MessageAdapter : BaseLogReceiver, IMessageAdapter
	{
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

				_bid.Price = (decimal?)bidPrice ?? 0m;
				_bid.Volume = (decimal?)bidVolume ?? 0m;

				_ask.Price = (decimal?)askPrice ?? 0m;
				_ask.Volume = (decimal?)askVolume ?? 0m;

				_localTime = message.LocalTime;
				_serverTime = message.ServerTime;

				return true;
			}
		}

		private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new SynchronizedDictionary<string, QuoteChangeDepthBuilder>();
		private readonly SynchronizedDictionary<SecurityId, Level1DepthBuilder> _level1DepthBuilders = new SynchronizedDictionary<SecurityId, Level1DepthBuilder>();

		//private readonly bool _checkLicense;

		private DateTime _prevTime;

		private readonly CodeTimeOut<long> _secLookupTimeOut = new CodeTimeOut<long>();
		private readonly CodeTimeOut<long> _pfLookupTimeOut = new CodeTimeOut<long>();

		/// <summary>
		/// Инициализировать <see cref="MessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		protected MessageAdapter(IdGenerator transactionIdGenerator/*, bool checkLicense = true*/)
		{
			if (transactionIdGenerator == null)
				throw new ArgumentNullException("transactionIdGenerator");

			Platform = Platforms.AnyCPU;
			//_checkLicense = checkLicense;

			TransactionIdGenerator = transactionIdGenerator;
			SecurityClassInfo = new Dictionary<string, RefPair<SecurityTypes, string>>();

			CreateDepthFromLevel1 = true;

			IsMarketDataEnabled = IsTransactionEnabled = true;
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.MarketDataKey)]
		[DescriptionLoc(LocalizedStrings.UseMarketDataSessionKey)]
		[PropertyOrder(1)]
		public bool IsMarketDataEnabled { get; set; }

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TransactionsKey)]
		[DescriptionLoc(LocalizedStrings.UseTransactionalSessionKey)]
		[PropertyOrder(2)]
		public bool IsTransactionEnabled { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsValid { get { return true; } }

		/// <summary>
		/// Описание классов инструментов, в зависимости от которых будут проставляться параметры в <see cref="SecurityMessage.SecurityType"/> и <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		[Browsable(false)]
		public IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; private set; }

		private TimeSpan _heartbeatInterval = TimeSpan.Zero;

		/// <summary>
		/// Интервал оповещения сервера о том, что подключение еще живое.
		/// Значение <see cref="TimeSpan.Zero"/> означает выключенное оповещение.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.Str192Key)]
		[DescriptionLoc(LocalizedStrings.Str193Key)]
		public TimeSpan HeartbeatInterval
		{
			get { return _heartbeatInterval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_heartbeatInterval = value;
			}
		}

		/// <summary>
		/// Создавать объединенный инструмент для инструментов с разных торговых площадок.
		/// </summary>
		[CategoryLoc(LocalizedStrings.SecuritiesKey)]
		[DisplayNameLoc(LocalizedStrings.Str197Key)]
		[DescriptionLoc(LocalizedStrings.Str198Key)]
		[PropertyOrder(1)]
		public bool CreateAssociatedSecurity { get; set; }

		private string _associatedBoardCode = "ALL";

		/// <summary>
		/// Код площадки для объединенного инструмента.
		/// </summary>
		[CategoryLoc(LocalizedStrings.SecuritiesKey)]
		[DisplayNameLoc(LocalizedStrings.Str197Key)]
		[DescriptionLoc(LocalizedStrings.Str199Key)]
		[PropertyOrder(10)]
		public string AssociatedBoardCode
		{
			get { return _associatedBoardCode; }
			set { _associatedBoardCode = value; }
		}

		/// <summary>
		/// Обновлять стакан для инструмента при появлении сообщения <see cref="Level1ChangeMessage"/>.
		/// По умолчанию включено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.SecuritiesKey)]
		[DisplayNameLoc(LocalizedStrings.Str200Key)]
		[DescriptionLoc(LocalizedStrings.Str201Key)]
		[PropertyOrder(20)]
		public bool CreateDepthFromLevel1 { get; set; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public virtual bool PortfolioLookupRequired
		{
			get { return false; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public virtual bool SecurityLookupRequired
		{
			get { return false; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public virtual bool OrderStatusRequired
		{
			get { return false; }
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

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public virtual OrderCondition CreateOrderCondition()
		{
			return null;
		}

		private void CreateAssociatedSecurityQuotes(QuoteChangeMessage quoteMsg)
		{
			if (!CreateAssociatedSecurity)
				return;

			if (quoteMsg.SecurityId.IsDefault())
				return;

			var builder = _quoteChangeDepthBuilders
				.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, AssociatedBoardCode));

			NewOutMessage.SafeInvoke(builder.Process(quoteMsg));
		}

		private SecurityId CloneSecurityId(SecurityId securityId)
		{
			return new SecurityId
			{
				SecurityCode = securityId.SecurityCode,
				BoardCode = AssociatedBoardCode,
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

		private IdGenerator _transactionIdGenerator;

		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		public IdGenerator TransactionIdGenerator
		{
			get { return _transactionIdGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_transactionIdGenerator = value;
			}
		}

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

		bool IMessageChannel.IsOpened
		{
			get { return true; }
		}

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		/// <summary>
		/// Отправить входящее сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void SendInMessage(Message message)
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

#warning force
			//месседжи с заявками могут складываться из потока обработки
			var force = message.Type == MessageTypes.OrderRegister ||
			            message.Type == MessageTypes.OrderReplace ||
			            message.Type == MessageTypes.OrderPairReplace ||
			            message.Type == MessageTypes.OrderCancel ||
			            message.Type == MessageTypes.OrderGroupCancel;

			InitMessageLocalTime(message);

			// при отключенном состоянии пропускаем только TimeMessage
			// остальные типы сообщений могут использоваться (например, в эмуляторе)
			//if ((_currState == ConnectionStates.Disconnecting || _currState == ConnectionStates.Disconnected) && message.Type == MessageTypes.Time)
			//	return;

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
			}

			try
			{
				OnSendInMessage(message);
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);

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

			if (message.IsBack)
			{
				message.IsBack = false;

				// time msg should be return back
				SendOutMessage(message);
			}
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
		/// Отправить исходящее сообщение, вызвав событие <see cref="NewOutMessage"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public virtual void SendOutMessage(Message message)
		{
			InitMessageLocalTime(message);

			switch (message.Type)
			{
				case MessageTypes.Security:
				{
					NewOutMessage.SafeInvoke(message);

					if (!CreateAssociatedSecurity)
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

					if (CreateAssociatedSecurity)
					{
						// обновление BestXXX для ALL из конкретных тикеров
						var clone = (Level1ChangeMessage)l1Msg.Clone();
						clone.SecurityId = CloneSecurityId(clone.SecurityId);
						NewOutMessage.SafeInvoke(clone);
					}

					if (CreateDepthFromLevel1)
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

					if (CreateDepthFromLevel1)
						_level1DepthBuilders.SafeAdd(quoteMsg.SecurityId, c => new Level1DepthBuilder(c)).HasDepth = true;

					CreateAssociatedSecurityQuotes(quoteMsg);
					break;
				}

				case MessageTypes.Execution:
				{
					NewOutMessage.SafeInvoke(message);

					if (!CreateAssociatedSecurity)
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
				{
					if (_prevTime != DateTime.MinValue)
					{
						var diff = message.LocalTime - _prevTime;

						//if (message.Type != MessageTypes.Time && diff >= MarketTimeChangedInterval)
						//{
						//	SendOutMessage(new TimeMessage
						//	{
						//		LocalTime = message.LocalTime,
						//		ServerTime = message.GetServerTime(),
						//	});
						//}

						_secLookupTimeOut
							.ProcessTime(diff)
							.ForEach(id => SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = id }));

						_pfLookupTimeOut
							.ProcessTime(diff)
							.ForEach(id => SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = id }));

						//ProcessReconnection(diff);
					}

					_prevTime = message.LocalTime;
					NewOutMessage.SafeInvoke(message);
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
				message.LocalTime = CurrentTime.LocalDateTime;
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
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
		protected virtual bool IsConnectionAlive()
		{
			return true;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			HeartbeatInterval = storage.GetValue<TimeSpan>("HeartbeatInterval");

			IsMarketDataEnabled = storage.GetValue<bool>("IsMarketDataEnabled");
			IsTransactionEnabled = storage.GetValue<bool>("IsTransactionEnabled");

			CreateAssociatedSecurity = storage.GetValue("CreateAssociatedSecurity", CreateAssociatedSecurity);
			AssociatedBoardCode = storage.GetValue("AssociatedBoardCode", AssociatedBoardCode);
			CreateDepthFromLevel1 = storage.GetValue("CreateDepthFromLevel1", CreateDepthFromLevel1);

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("HeartbeatInterval", HeartbeatInterval);

			storage.SetValue("IsMarketDataEnabled", IsMarketDataEnabled);
			storage.SetValue("IsTransactionEnabled", IsTransactionEnabled);

			storage.SetValue("CreateAssociatedSecurity", CreateAssociatedSecurity);
			storage.SetValue("AssociatedBoardCode", AssociatedBoardCode);
			storage.SetValue("CreateDepthFromLevel1", CreateDepthFromLevel1);

			base.Save(storage);
		}
	}

	/// <summary>
	/// Специальный адаптер, который передает сразу на выход все входящие сообщения.
	/// </summary>
	public class PassThroughMessageAdapter : MessageAdapter
	{
		/// <summary>
		/// Создать <see cref="PassThroughMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public PassThroughMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			IsMarketDataEnabled = false;
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			SendOutMessage(message);
			//switch (message.Type)
			//{
			//	case MessageTypes.Connect:
			//		SendOutMessage(new ConnectMessage());
			//		break;

			//	case MessageTypes.Disconnect:
			//		SendOutMessage(new DisconnectMessage());
			//		break;

			//	case MessageTypes.Time: // обработка heartbeat
			//		break;

			//	default:
			//		throw new NotSupportedException(LocalizedStrings.Str2143Params.Put(message.Type));
			//}
		}
	}
}