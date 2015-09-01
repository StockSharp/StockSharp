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

	/// <summary>
	/// Базовый адаптер, конвертирующий сообщения <see cref="Message"/> в команды торговой системы и обратно.
	/// </summary>
	public abstract class MessageAdapter : BaseLogReceiver, IMessageAdapter
	{
		private class CodeTimeOut
			//where T : class
		{
			private readonly CachedSynchronizedDictionary<long, TimeSpan> _registeredKeys = new CachedSynchronizedDictionary<long, TimeSpan>();

			private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

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

			public void StartTimeOut(long key)
			{
				//if (key == 0)
				//	throw new ArgumentNullException("key");

				_registeredKeys.SafeAdd(key, s => TimeOut);
			}

			public IEnumerable<long> ProcessTime(TimeSpan diff)
			{
				if (_registeredKeys.Count == 0)
					return Enumerable.Empty<long>();

				return _registeredKeys.SyncGet(d =>
				{
					var timeOutCodes = new List<long>();

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

		private DateTime _prevTime;

		private readonly CodeTimeOut _secLookupTimeOut = new CodeTimeOut();
		private readonly CodeTimeOut _pfLookupTimeOut = new CodeTimeOut();

		/// <summary>
		/// Инициализировать <see cref="MessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		protected MessageAdapter(IdGenerator transactionIdGenerator)
		{
			if (transactionIdGenerator == null)
				throw new ArgumentNullException("transactionIdGenerator");

			Platform = Platforms.AnyCPU;

			TransactionIdGenerator = transactionIdGenerator;
			SecurityClassInfo = new Dictionary<string, RefPair<SecurityTypes, string>>();
		}

		private MessageTypes[] _supportedMessages = ArrayHelper.Empty<MessageTypes>();

		/// <summary>
		/// Поддерживаемые типы сообщений, который может обработать адаптер.
		/// </summary>
		[Browsable(false)]
		public virtual MessageTypes[] SupportedMessages
		{
			get { return _supportedMessages; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				var dulicate = value.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
				if (dulicate != null)
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(dulicate.Key), "value");

				_supportedMessages = value;
			}
		}

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
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		[Browsable(false)]
		public virtual bool SecurityLookupRequired
		{
			get { return SupportedMessages.Contains(MessageTypes.SecurityLookup); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		[Browsable(false)]
		public virtual bool PortfolioLookupRequired
		{
			get { return SupportedMessages.Contains(MessageTypes.PortfolioLookup); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		[Browsable(false)]
		public virtual bool OrderStatusRequired
		{
			get { return SupportedMessages.Contains(MessageTypes.OrderStatus); }
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
		[Browsable(false)]
		public Platforms Platform { get; protected set; }

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public virtual OrderCondition CreateOrderCondition()
		{
			return null;
		}

		private readonly ReConnectionSettings _reConnectionSettings = new ReConnectionSettings();

		/// <summary>
		/// Настройки механизма отслеживания соединений <see cref="IMessageAdapter"/> с торговом системой.
		/// </summary>
		public ReConnectionSettings ReConnectionSettings
		{
			get { return _reConnectionSettings; }
		}

		private IdGenerator _transactionIdGenerator;

		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		[Browsable(false)]
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
		[Browsable(false)]
		public TimeSpan LookupTimeOut
		{
			get { return _secLookupTimeOut.TimeOut; }
			set
			{
				_secLookupTimeOut.TimeOut = value;
				_pfLookupTimeOut.TimeOut = value;
			}
		}

		private string _associatedBoardCode = "ALL";

		/// <summary>
		/// Код площадки для объединенного инструмента. По-умолчанию равно ALL.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AssociatedSecurityBoardKey)]
		[DescriptionLoc(LocalizedStrings.Str199Key)]
		public string AssociatedBoardCode
		{
			get { return _associatedBoardCode; }
			set { _associatedBoardCode = value; }
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

			InitMessageLocalTime(message);

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

			if (_prevTime != DateTime.MinValue)
			{
				var diff = message.LocalTime - _prevTime;

				_secLookupTimeOut
					.ProcessTime(diff)
					.ForEach(id => SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = id }));

				_pfLookupTimeOut
					.ProcessTime(diff)
					.ForEach(id => SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = id }));
			}

			_prevTime = message.LocalTime;
			NewOutMessage.SafeInvoke(message);
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
		/// Создать сообщение <see cref="SecurityMessage"/> и передать его в метод <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="originalTransactionId">Идентификатор первоначального сообщения, для которого данное сообщение является ответом..</param>
		protected void SendOutMarketDataNotSupported(long originalTransactionId)
		{
			SendOutMessage(new MarketDataMessage { OriginalTransactionId = originalTransactionId, IsNotSupported = true });
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
		public virtual bool IsConnectionAlive()
		{
			return true;
		}

		/// <summary>
		/// Создать построитель стакана.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Построитель стакана.</returns>
		public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			HeartbeatInterval = storage.GetValue<TimeSpan>("HeartbeatInterval");
			SupportedMessages = storage.GetValue<string[]>("SupportedMessages").Select(i => i.To<MessageTypes>()).ToArray();
			AssociatedBoardCode = storage.GetValue("AssociatedBoardCode", AssociatedBoardCode);

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("HeartbeatInterval", HeartbeatInterval);
			storage.SetValue("SupportedMessages", SupportedMessages.Select(t => t.To<string>()).ToArray());
			storage.SetValue("AssociatedBoardCode", AssociatedBoardCode);

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