namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Заявка.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	[DisplayNameLoc(LocalizedStrings.Str504Key)]
	[DescriptionLoc(LocalizedStrings.Str516Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
	public class Order : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// Создать <see cref="Order"/>.
		/// </summary>
		public Order()
		{
		}

		private TimeSpan? _latencyRegistration;

		/// <summary>
		/// Время, которое потребовалось для регистрации заявки.
		/// </summary>
		[TimeSpan]
		[DisplayNameLoc(LocalizedStrings.Str517Key)]
		[DescriptionLoc(LocalizedStrings.Str518Key)]
		[StatisticsCategory]
		[Nullable]
		public TimeSpan? LatencyRegistration
		{
			get { return _latencyRegistration; }
			set
			{
				if (_latencyRegistration == value)
					return;

				_latencyRegistration = value;
				NotifyChanged("LatencyRegistration");
			}
		}

		private TimeSpan? _latencyCancellation;

		/// <summary>
		/// Время, которое потребовалось для отмены заявки.
		/// </summary>
		[TimeSpan]
		[DisplayNameLoc(LocalizedStrings.Str519Key)]
		[DescriptionLoc(LocalizedStrings.Str520Key)]
		[StatisticsCategory]
		[Nullable]
		public TimeSpan? LatencyCancellation
		{
			get { return _latencyCancellation; }
			set
			{
				if (_latencyCancellation == value)
					return;

				_latencyCancellation = value;
				NotifyChanged("LatencyCancellation");
			}
		}

		private long _id;

		/// <summary>
		/// Идентификатор заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringKey, true)]
		[MainCategory]
		public long Id
		{
			get { return _id; }
			set
			{
				if (_id == value)
					return;

				_id = value;
				NotifyChanged("Id");
			}
		}

		private string _stringId;

		/// <summary>
		/// Идентификатор заявки (ввиде строки, если электронная площадка не использует числовое представление идентификатора заявки).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str521Key)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringDescKey)]
		[MainCategory]
		public string StringId
		{
			get { return _stringId; }
			set
			{
				_stringId = value;
				NotifyChanged("StringId");
			}
		}

		private string _boardId;

		/// <summary>
		/// Идентификатор заявки электронной площадки. Используется, если <see cref="Id"/> или <see cref="StringId"/> содержит идентификатор брокерской системы.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str117Key)]
		[DescriptionLoc(LocalizedStrings.Str118Key)]
		[MainCategory]
		public string BoardId
		{
			get { return _boardId; }
			set
			{
				_boardId = value;
				NotifyChanged("BoardId");
			}
		}

		private DateTimeOffset _time;

		/// <summary>
		/// Время выставления заявки на бирже.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str522Key)]
		[DescriptionLoc(LocalizedStrings.Str523Key)]
		[MainCategory]
		public DateTimeOffset Time
		{
			get { return _time; }
			set
			{
				if (_time == value)
					return;

				_time = value;
				NotifyChanged("Time");

				if (LastChangeTime.IsDefault())
					LastChangeTime = value;
			}
		}

		/// <summary>
		/// Номер транзакции. Автоматически устанавливается при вызове метода <see cref="IConnector.RegisterOrder" />.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		[Identity]
		public long TransactionId { get; set; }

		/// <summary>
		/// Инструмент, по которому выставляется заявка.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str524Key)]
		[MainCategory]
		public Security Security { get; set; }

		private OrderStates _state;

		/// <summary>
		/// Состояние заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str134Key)]
		[MainCategory]
		public OrderStates State
		{
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged("State");
			}
		}

		/// <summary>
		/// Портфель, в рамках которого торгуется заявка.
		/// </summary>
		[DataMember]
		[RelationSingle(IdentityType = typeof(string))]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str525Key)]
		[MainCategory]
		public Portfolio Portfolio { get; set; }

		private readonly Lazy<SynchronizedList<string>> _messages = new Lazy<SynchronizedList<string>>(() => new SynchronizedList<string>());

		/// <summary>
		/// Сообщения к заявке (создаются торговой системой при регистрации, изменении или снятии).
		/// </summary>
		[XmlIgnore]
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str526Key)]
		[DescriptionLoc(LocalizedStrings.Str527Key)]
		[MainCategory]
		public ISynchronizedCollection<string> Messages
		{
			get { return _messages.Value; }
		}

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Время последнего изменения заявки (Снятие, Сведение).
		/// </summary>
		[DataMember]
		//[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str528Key)]
		[DescriptionLoc(LocalizedStrings.Str529Key)]
		[MainCategory]
		public DateTimeOffset LastChangeTime
		{
			get { return _lastChangeTime; }
			set
			{
				if (_lastChangeTime == value)
					return;

				_lastChangeTime = value;
				NotifyChanged("LastChangeTime");
			}
		}

		private DateTime _localTime;

		/// <summary>
		/// Локальное время последнего изменения заявки (Снятие, Сведение).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str530Key)]
		[DescriptionLoc(LocalizedStrings.Str531Key)]
		[MainCategory]
		public DateTime LocalTime
		{
			get { return _localTime; }
			set
			{
				if (_localTime == value)
					return;

				_localTime = value;
				NotifyChanged("LocalTime");
			}
		}

		/// <summary>
		/// Цена заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal Price { get; set; }

		private decimal _volume;

		/// <summary>
		/// Количество контрактов в заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				_volume = value;
				VisibleVolume = value;
			}
		}

		/// <summary>
		/// Видимое количество контрактов в заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VisibleVolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str127Key)]
		[MainCategory]
		public decimal VisibleVolume { get; set; }

		/// <summary>
		/// Направление заявки (покупка или продажа).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str129Key)]
		[MainCategory]
		public Sides Direction { get; set; }

		private decimal _balance;

		/// <summary>
		/// Остаток контрактов в заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str130Key)]
		[DescriptionLoc(LocalizedStrings.Str131Key)]
		[MainCategory]
		public decimal Balance
		{
			get { return _balance; }
			set
			{
				if (_balance == value)
					return;

				_balance = value;
				NotifyChanged("Balance");
			}
		}

		private OrderStatus? _status;

		/// <summary>
		/// Системный статус заявки.
		/// </summary>
		[DataMember]
		[Nullable]
		[Browsable(false)]
		public OrderStatus? Status
		{
			get { return _status; }
			set
			{
				if (_status == value)
					return;

				_status = value;
				NotifyChanged("Status");
			}
		}

		private bool _isSystem = true;

		/// <summary>
		/// Является ли заявка системной.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str139Key)]
		[DescriptionLoc(LocalizedStrings.Str140Key)]
		[MainCategory]
		public bool IsSystem
		{
			get { return _isSystem; }
			set
			{
				if (_isSystem == value)
					return;

				_isSystem = value;
				NotifyChanged("IsSystem");
			}
		}

		/// <summary>
		/// Комментарий к выставляемой заявке.
		/// </summary>
		[DataMember]
		[Primitive]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Тип заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes Type { get; set; }

		private DateTimeOffset _expiryDate = DateTimeOffset.MaxValue;

		/// <summary>
		/// Время экспирации заявки. По-умолчанию равно <see cref="DateTime.MaxValue"/>, что означает действие заявки до отмены (GTC).
		/// </summary>
		/// <remarks>
		/// Если значение равно <see cref="DateTime.Today"/>, то заявка выставляется сроком на текущую сессию.
		/// Если значение равно <see cref="DateTime.MaxValue"/>, то заявка выставляется до отмены.
		/// Иначе, указывается конкретный срок.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset ExpiryDate
		{
			get { return _expiryDate; }
			set
			{
				if (_expiryDate == value)
					return;

				_expiryDate = value;
				NotifyChanged("ExpiryDate");
			}
		}

		/// <summary>
		/// Условие заявки (например, параметры стоп- или алго- заявков).
		/// </summary>
		//[DataMember]
		//[InnerSchema(IsNullable = true)]
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public OrderCondition Condition { get; set; }

		/// <summary>
		/// Время жизни лимитной заявки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str231Key)]
		[DescriptionLoc(LocalizedStrings.Str232Key)]
		[MainCategory]
		public TimeInForce TimeInForce { get; set; }

		private Order _derivedOrder;

		/// <summary>
		/// Биржевая заявка, которая была создана стоп-заявкой при активации условия (null, если стоп-условие еще не было активировано).
		/// </summary>
		[DataMember]
		[InnerSchema]
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str532Key)]
		[DescriptionLoc(LocalizedStrings.Str533Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public Order DerivedOrder
		{
			get { return _derivedOrder; }
			set
			{
				if (_derivedOrder == value)
					return;

				_derivedOrder = value;
				NotifyChanged("DerivedOrder");
			}
		}

		/// <summary>
		/// Информация для РЕПО\РЕПО-М заявок.
		/// </summary>
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str233Key)]
		[DescriptionLoc(LocalizedStrings.Str234Key)]
		[MainCategory]
		public RepoOrderInfo RepoInfo { get; set; }

		/// <summary>
		/// Информация для РПС заявок.
		/// </summary>
		[Ignore]
		[DisplayNameLoc(LocalizedStrings.Str235Key)]
		[DescriptionLoc(LocalizedStrings.Str236Key)]
		[MainCategory]
		public RpsOrderInfo RpsInfo { get; set; }

		[field: NonSerialized]
		private SynchronizedDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация по заявке.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной с заявкой.
		/// Например, время активации, доходность для обычных заявок, или номер заявки-условия для стоп-заявки.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				_extensionInfo = value.Sync();
				NotifyChanged("ExtensionInfo");
			}
		}

		/// <summary>
		/// Комиссия (брокерская, биржевая и т.д.).
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[MainCategory]
		public decimal? Commission { get; set; }

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Подключение к торговой системе, через который была зарегистрирована данная заявка.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public IConnector Connector
		{
			get { return _connector; }
			set
			{
				if (_connector == value)
					return;

				_connector = value;

				NotifyChanged("Connector");
			}
		}

		/// <summary>
		/// Пользовательский идентификатор заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str534Params
				.Put(TransactionId, Id == 0 ? StringId : Id.To<string>(), Direction == Sides.Buy ? LocalizedStrings.Str403 : LocalizedStrings.Str404, Price, Volume, State, Balance);
		}
	}
}
