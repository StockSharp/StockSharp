namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее информацию для регистрации заявки.
	/// </summary>
	public class OrderRegisterMessage : OrderMessage
	{
		/// <summary>
		/// Номер транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		public long TransactionId { get; set; }

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
		public Sides Side { get; set; }

		/// <summary>
		/// Является ли заявка системной.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str139Key)]
		[DescriptionLoc(LocalizedStrings.Str140Key)]
		[MainCategory]
		public bool IsSystem { get; set; }

		/// <summary>
		/// Комментарий к выставляемой заявке.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str135Key)]
		[DescriptionLoc(LocalizedStrings.Str136Key)]
		[MainCategory]
		public string Comment { get; set; }

		/// <summary>
		/// Время действия заявки.
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
		public DateTimeOffset TillDate { get; set; }

		/// <summary>
		/// Условие заявки (например, параметры стоп- или алго- заявков).
		/// </summary>
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

		/// <summary>
		/// Информация для РЕПО\РЕПО-М заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str233Key)]
		[DescriptionLoc(LocalizedStrings.Str234Key)]
		[MainCategory]
		public RepoOrderInfo RepoInfo { get; set; }

		/// <summary>
		/// Информация для РПС заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str235Key)]
		[DescriptionLoc(LocalizedStrings.Str236Key)]
		[MainCategory]
		public RpsOrderInfo RpsInfo { get; set; }

		///// <summary>
		///// Валюта, в которой выражается значение цены <see cref="Price"/>.
		///// </summary>
		//public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Создать <see cref="OrderRegisterMessage"/>.
		/// </summary>
		public OrderRegisterMessage()
			: base(MessageTypes.OrderRegister)
		{
		}

		/// <summary>
		/// Инициализировать <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected OrderRegisterMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="OrderRegisterMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new OrderRegisterMessage(Type)
			{
				Comment = Comment,
				Condition = Condition,
				TillDate = TillDate,
				IsSystem = IsSystem,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				Price = Price,
				RepoInfo = RepoInfo,
				RpsInfo = RpsInfo,
				SecurityId = SecurityId,
				//SecurityType = SecurityType,
				Side = Side,
				TimeInForce = TimeInForce,
				TransactionId = TransactionId,
				VisibleVolume = VisibleVolume,
				Volume = Volume,
				Currency = Currency,
				UserOrderId = UserOrderId
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",TransId={0},Price={1},Side={2},OrdType={3},Vol={4},Sec={5}".Put(TransactionId, Price, Side, OrderType, Volume, SecurityId);
		}
	}
}