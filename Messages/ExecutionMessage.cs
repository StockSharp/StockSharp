namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Типы данных, информация о которых содержится <see cref="ExecutionMessage"/>.
	/// </summary>
	public enum ExecutionTypes
	{
		/// <summary>
		/// Тиковая сделка.
		/// </summary>
		Tick,

		/// <summary>
		/// Лог заявок.
		/// </summary>
		Order,

		/// <summary>
		/// Собственная сделка.
		/// </summary>
		Trade,

		/// <summary>
		/// Лог заявок
		/// </summary>
		OrderLog,
	}

	/// <summary>
	/// Сообщение, содержащее информацию об исполнении.
	/// </summary>
	[Serializable]
	[DataContract]
	public sealed class ExecutionMessage : Message
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Название портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.PortfolioNameKey)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Серверное время.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.ServerTimeKey, true)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Номер транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Номер первоначальной транзакции, для которой данное сообщение является ответом.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OriginalTrasactionKey)]
		[DescriptionLoc(LocalizedStrings.OriginalTrasactionIdKey)]
		[MainCategory]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Тип данных, информация о которых содержится <see cref="ExecutionMessage"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DataTypeKey)]
		[DescriptionLoc(LocalizedStrings.Str110Key)]
		[MainCategory]
		public ExecutionTypes? ExecutionType { get; set; }

		/// <summary>
		/// Является ли действие отменой заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CancelKey)]
		[DescriptionLoc(LocalizedStrings.IsActionOrderCancellationKey)]
		[MainCategory]
		public bool IsCancelled { get; set; }

		///// <summary>
		///// Действие, произошедшее с заявкой.
		///// </summary>
		//[DataMember]
		//[DisplayName("Действие")]
		//[Description("Действие, произошедшее с заявкой.")]
		//[MainCategory]
		//public ExecutionActions Action { get; set; }

		/// <summary>
		/// Идентификатор заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdKey)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringKey, true)]
		[MainCategory]
		public long OrderId { get; set; }

		/// <summary>
		/// Идентификатор заявки (ввиде строки, если электронная площадка не использует числовое представление идентификатора заявки).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.OrderIdStringDescKey)]
		[MainCategory]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Идентификатор заявки электронной площадки. Используется, если <see cref="OrderId"/> или <see cref="OrderStringId"/> содержит идентификатор брокерской системы.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str117Key)]
		[DescriptionLoc(LocalizedStrings.Str118Key)]
		[MainCategory]
		public string OrderBoardId { get; set; }

		/// <summary>
		/// Идентификатор производной заявки (например, условная заявка сгенерировала реальную биржевую).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DerivedKey)]
		[DescriptionLoc(LocalizedStrings.DerivedOrderIdKey)]
		[MainCategory]
		public long? DerivedOrderId { get; set; }

		/// <summary>
		/// Идентификатор производной заявки (например, условная заявка сгенерировала реальную биржевую).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DerivedStringKey)]
		[DescriptionLoc(LocalizedStrings.DerivedStringDescKey)]
		[MainCategory]
		public string DerivedOrderStringId { get; set; }

		/// <summary>
		/// Цена заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.OrderPriceKey)]
		[MainCategory]
		public decimal Price { get; set; }

		/// <summary>
		/// Количество контрактов в заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.OrderVolumeKey)]
		[MainCategory]
		public decimal Volume { get; set; }

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
		public Sides Side { get; set; }

		/// <summary>
		/// Остаток контрактов в заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str130Key)]
		[DescriptionLoc(LocalizedStrings.Str131Key)]
		[MainCategory]
		public decimal Balance { get; set; }

		/// <summary>
		/// Тип заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes OrderType { get; set; }

		/// <summary>
		/// Системный статус заявки.
		/// </summary>
		[DataMember]
		[Browsable(false)]
		public OrderStatus? OrderStatus { get; set; }

		/// <summary>
		/// Состояние заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str134Key)]
		[MainCategory]
		public OrderStates? OrderState { get; set; }

		/// <summary>
		/// Комментарий к выставляемой заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Сообщение к заявке (создается торговой системой при регистрации, изменении или снятии).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str137Key)]
		[DescriptionLoc(LocalizedStrings.Str138Key)]
		[MainCategory]
		public string SystemComment { get; set; }

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
			set { _isSystem = value; }
		}

		/// <summary>
		/// Время экспирации заявки.
		/// </summary>
		/// <remarks>
		/// Если значение равно <see cref="DateTime.Today"/>, то заявка выставляется сроком на текущую сессию.
		/// Если значение равно <see cref="DateTime.MaxValue"/>, то заявка выставляется до отмены (GTC).
		/// Иначе, указывается конкретный срок.
		/// </remarks>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str141Key)]
		[DescriptionLoc(LocalizedStrings.Str142Key)]
		[MainCategory]
		public DateTimeOffset ExpiryDate { get; set; }

		/// <summary>
		/// Условие исполнения лимитированной заявки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str143Key)]
		[DescriptionLoc(LocalizedStrings.Str144Key)]
		[MainCategory]
		public TimeInForce TimeInForce { get; set; }

		/// <summary>
		/// Идентификатор сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdKey)]
		[DescriptionLoc(LocalizedStrings.Str145Key)]
		[MainCategory]
		public long TradeId { get; set; }

		/// <summary>
		/// Идентификатор сделки (ввиде строки, если электронная площадка не использует числовое представление идентификатора сделки).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OrderIdStringKey)]
		[DescriptionLoc(LocalizedStrings.Str146Key)]
		[MainCategory]
		public string TradeStringId { get; set; }

		/// <summary>
		/// Цена сделки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str147Key)]
		[MainCategory]
		public decimal TradePrice { get; set; }

		/// <summary>
		/// Системный статус сделки.
		/// </summary>
		[DataMember]
		[Browsable(false)]
		public int TradeStatus { get; set; }

		/// <summary>
		/// Инициатор сделки (продавец или покупатель).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str148Key)]
		[DescriptionLoc(LocalizedStrings.Str149Key)]
		[MainCategory]
		public Sides? OriginSide { get; set; }

		/// <summary>
		/// Количество открытых позиций (открытый интерес).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str150Key)]
		[DescriptionLoc(LocalizedStrings.Str151Key)]
		[MainCategory]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Ошибка регистрации/отмены заявки.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str152Key)]
		[DescriptionLoc(LocalizedStrings.Str153Key)]
		[MainCategory]
		public Exception Error { get; set; }

		/// <summary>
		/// Условие заявки (например, параметры стоп- или алго- заявков).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str154Key)]
		[DescriptionLoc(LocalizedStrings.Str155Key)]
		[CategoryLoc(LocalizedStrings.Str156Key)]
		public OrderCondition Condition { get; set; }

		///// <summary>
		///// Является ли сообщение последним в запрашиваемом пакете (только для исторических сделок).
		///// </summary>
		//[DataMember]
		//[DisplayName("Последний")]
		//[Description("Является ли сообщение последним в запрашиваемом пакете (только для исторических сделок).")]
		//[MainCategory]
		//public bool IsFinished { get; set; }

		/// <summary>
		/// Является ли тик восходящим или нисходящим в цене. Заполняется только для <see cref="ExecutionType"/> равным <see cref="ExecutionTypes.Tick"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str157Key)]
		[DescriptionLoc(LocalizedStrings.Str158Key)]
		[MainCategory]
		public bool? IsUpTick { get; set; }

		/// <summary>
		/// Комиссия (брокерская, биржевая и т.д.).  Заполняется при <see cref="ExecutionType"/> равном <see cref="ExecutionTypes.Order"/> или <see cref="ExecutionTypes.Trade"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[MainCategory]
		public decimal? Commission { get; set; }

		/// <summary>
		/// Сетевая задержка. Заполняется при <see cref="ExecutionType"/> равном <see cref="ExecutionTypes.Order"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str162Key)]
		[MainCategory]
		public TimeSpan? Latency { get; set; }

		/// <summary>
		/// Проскальзывание в цене сделки. Заполняется при <see cref="ExecutionType"/> равном <see cref="ExecutionTypes.Trade"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		[MainCategory]
		public decimal? Slippage { get; set; }

		/// <summary>
		/// Пользовательский идентификатор заявки. Заполняется при <see cref="ExecutionType"/> равном <see cref="ExecutionTypes.Order"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Создать <see cref="ExecutionMessage"/>.
		/// </summary>
		public ExecutionMessage()
			: base(MessageTypes.Execution)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",T(S)={0:yyyy/MM/dd HH:mm:ss.fff},({1}),Sec={2},Ord={3}/{4}/{5},Fail={6},TId={7},Pf={8},TPrice={9},UId={10}"
				.Put(ServerTime, ExecutionType, SecurityId, OrderId, TransactionId, OriginalTransactionId,
					Error, TradeId, PortfolioName, TradePrice, UserOrderId);
		}

		/// <summary>
		/// Создать копию объекта.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new ExecutionMessage
			{
				Balance = Balance,
				Comment = Comment,
				Condition = Condition.CloneNullable(),
				ServerTime = ServerTime,
				DepoName = DepoName,
				Error = Error,
				ExpiryDate = ExpiryDate,
				IsSystem = IsSystem,
				LocalTime = LocalTime,
				OpenInterest = OpenInterest,
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				OrderBoardId = OrderBoardId,
				ExecutionType = ExecutionType,
				IsCancelled = IsCancelled,
				//Action = Action,
				OrderState = OrderState,
				OrderStatus = OrderStatus,
				OrderType = OrderType,
				OriginSide = OriginSide,
				PortfolioName = PortfolioName,
				Price = Price,
				SecurityId = SecurityId,
				Side = Side,
				SystemComment = SystemComment,
				TimeInForce = TimeInForce,
				TradeId = TradeId,
				TradeStringId = TradeStringId,
				TradePrice = TradePrice,
				TradeStatus = TradeStatus,
				TransactionId = TransactionId,
				OriginalTransactionId = OriginalTransactionId,
				Volume = Volume,
				//IsFinished = IsFinished,
				VisibleVolume = VisibleVolume,
				IsUpTick = IsUpTick,
				Commission = Commission,
				Latency = Latency,
				Slippage = Slippage,
				UserOrderId = UserOrderId,

				DerivedOrderId = DerivedOrderId,
				DerivedOrderStringId = DerivedOrderStringId,
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}
	}
}