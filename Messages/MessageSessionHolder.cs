namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	public abstract class MessageSessionHolder : BaseLogReceiver, IMessageSessionHolder
	{
		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		[Browsable(false)]
		public IdGenerator TransactionIdGenerator { get; private set; }

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
		/// Объединять обработчики входящих сообщений для адаптеров.
		/// </summary>
		[Browsable(false)]
		public virtual bool JoinInProcessors { get { return true; } }

		/// <summary>
		/// Объединять обработчики исходящих сообщений для адаптеров.
		/// </summary>
		[Browsable(false)]
		public virtual bool JoinOutProcessors { get { return true; } }

		/// <summary>
		/// Описание классов инструментов, в зависимости от которых будут проставляться параметры в <see cref="SecurityMessage.SecurityType"/> и <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		[Browsable(false)]
		public IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; private set; }

		/// <summary>
		/// Настройки механизма отслеживания соединений.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		public MessageAdapterReConnectionSettings ReConnectionSettings { get; private set; }

		private TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Интервал оповещения сервера о том, что подключение еще живое. По-умолчанию равно 1 минуте.
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

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromMilliseconds(10);
		
		/// <summary>
		/// Интервал генерации сообщения <see cref="TimeMessage"/>. По-умолчанию равно 10 миллисекундам.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public TimeSpan MarketTimeChangedInterval
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
		/// Являются ли подключения адаптеров независимыми друг от друга.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsAdaptersIndependent
		{
			get { return false; }
		}

		/// <summary>
		/// Инициализировать <see cref="MessageSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		protected MessageSessionHolder(IdGenerator transactionIdGenerator)
		{
			if (transactionIdGenerator == null)
				throw new ArgumentNullException("transactionIdGenerator");

			TransactionIdGenerator = transactionIdGenerator;
			SecurityClassInfo = new Dictionary<string, RefPair<SecurityTypes, string>>();
			ReConnectionSettings = new MessageAdapterReConnectionSettings();

			CreateDepthFromLevel1 = true;
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public virtual OrderCondition CreateOrderCondition()
		{
			return null;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			MarketTimeChangedInterval = storage.GetValue<TimeSpan>("MarketTimeChangedInterval");
			HeartbeatInterval = storage.GetValue<TimeSpan>("HeartbeatInterval");
			ReConnectionSettings.Load(storage.GetValue<SettingsStorage>("ReConnectionSettings"));

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
			storage.SetValue("MarketTimeChangedInterval", MarketTimeChangedInterval);
			storage.SetValue("HeartbeatInterval", HeartbeatInterval);
			storage.SetValue("ReConnectionSettings", ReConnectionSettings.Save());

			storage.SetValue("IsMarketDataEnabled", IsMarketDataEnabled);
			storage.SetValue("IsTransactionEnabled", IsTransactionEnabled);

			storage.SetValue("CreateAssociatedSecurity", CreateAssociatedSecurity);
			storage.SetValue("AssociatedBoardCode", AssociatedBoardCode);
			storage.SetValue("CreateDepthFromLevel1", CreateDepthFromLevel1);

			base.Save(storage);
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public abstract IMessageAdapter CreateTransactionAdapter();

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public abstract IMessageAdapter CreateMarketDataAdapter();

		private IMessageAdapter _transactionAdapter;
		private IMessageAdapter _marketDataAdapter;

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public virtual void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Security:
				case MessageTypes.SecurityLookup:
				case MessageTypes.News:
				case MessageTypes.MarketData:
				{
					if (_marketDataAdapter != null)
						_marketDataAdapter.SendInMessage(message);
					
					break;
				}
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				case MessageTypes.Portfolio:
				case MessageTypes.Position:
				case MessageTypes.PortfolioLookup:
				case MessageTypes.OrderStatus:
				{
					if (_transactionAdapter != null)
						_transactionAdapter.SendInMessage(message);
					
					break;
				}
				case MessageTypes.Time:
				case MessageTypes.Connect:
				{
					if (IsTransactionEnabled)
					{
						_transactionAdapter = CreateTransactionAdapter();
						_transactionAdapter.NewOutMessage += TransactionAdapterOnNewOutMessage;
						_transactionAdapter.SendInMessage(message);
					}

					if (IsMarketDataEnabled)
					{
						_marketDataAdapter = CreateMarketDataAdapter();
						_marketDataAdapter.NewOutMessage += MarketDataAdapterOnNewOutMessage;
						_marketDataAdapter.SendInMessage(message);
					}

					break;
				}
				case MessageTypes.Disconnect:
				case MessageTypes.ChangePassword:
				case MessageTypes.ClearMessageQueue:
				{
					if (_marketDataAdapter != null)
						_marketDataAdapter.SendInMessage(message);
					
					if (_transactionAdapter != null)
						_transactionAdapter.SendInMessage(message);
					
					break;
				}
			}
		}

		private void MarketDataAdapterOnNewOutMessage(Message message)
		{
			NewOutMessage.SafeInvoke(message);
		}

		private void TransactionAdapterOnNewOutMessage(Message message)
		{
			NewOutMessage.SafeInvoke(message);
		}

		/// <summary>
		/// Событие появления нового сообщения.
		/// </summary>
		public event Action<Message> NewOutMessage;
	}

	/// <summary>
	/// Контейнер-заглушка.
	/// </summary>
	public class PassThroughSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Создать <see cref="PassThroughSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public PassThroughSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return null;
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return null;
		}
	}
}