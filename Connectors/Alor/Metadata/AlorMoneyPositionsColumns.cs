namespace StockSharp.Alor.Metadata
{
	/// <summary>
	/// Колонки системной таблицы POSITIONS.
	/// </summary>
	public static class AlorMoneyPositionsColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Money, "ID", typeof(int), false);

		/// <summary>
		/// Идентификатор клиента.
		/// </summary>
		public static readonly AlorColumn ClientId = new AlorColumn(AlorTableTypes.Money, "ClientID", typeof(int), false);

		/// <summary>
		/// Номер торгового счета.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.Money, "Account", typeof(string));

		/// <summary>
		/// Ссылка типа &lt;счет клиента&gt;[/&lt;субсчет&gt;].
		/// </summary>
		public static readonly AlorColumn BrokerRef = new AlorColumn(AlorTableTypes.Money, "BrokerRef", typeof(string), false);

		/// <summary>
		/// Входящий остаток – остаток денежных средств на счете клиента.
		/// </summary>
		public static readonly AlorColumn BeginValue = new AlorColumn(AlorTableTypes.Money, "OpenBal", typeof(decimal));

		/// <summary>
		/// Текущий остаток – входящий остаток плюс суммарный объем заключенных клиентом сделок на продажу минус суммарный объем сделок на покупку.
		/// </summary>
		public static readonly AlorColumn CurrentValue = new AlorColumn(AlorTableTypes.Money, "CurrentPos", typeof(decimal));

		/// <summary>
		/// Объём активных заявок на покупку – суммарный неудовлетворенный объём всех активных заявок на покупку, в руб.
		/// </summary>
		public static readonly AlorColumn CurrentBidsVolume = new AlorColumn(AlorTableTypes.Money, "OrderBuy", typeof(decimal), false);

		/// <summary>
		/// Объём активных заявок на продажу – суммарный неудовлетворенный объём всех активных заявок на продажу, в руб.
		/// </summary>
		public static readonly AlorColumn CurrentAsksVolume = new AlorColumn(AlorTableTypes.Money, "OrderSell", typeof(decimal), false);

		/// <summary>
		/// Расчетное значение портфеля по текущему курсу, в руб.
		/// </summary>
		public static readonly AlorColumn TotalPortfolio = new AlorColumn(AlorTableTypes.Money, "Portfolio", typeof(decimal), false);

		/// <summary>
		/// Суммарный объем в сделках за текущую торговую сессию, в руб.
		/// </summary>
		public static readonly AlorColumn TotalTrades = new AlorColumn(AlorTableTypes.Money, "Value", typeof(decimal), false);

		/// <summary>
		/// Биржевой сбор, в руб.
		/// </summary>
        public static readonly AlorColumn MarketCommission = new AlorColumn(AlorTableTypes.Money, "Commission", typeof(decimal));

		/// <summary>
		/// Комиссия брокера, в руб.
		/// </summary>
		public static readonly AlorColumn BrokerCommission = new AlorColumn(AlorTableTypes.Money, "Commission2", typeof(decimal));

		/// <summary>
		/// Планируемый биржевой сбор (учитываются выставленные заявки), в руб.
		/// </summary>
		public static readonly AlorColumn MarketPlannedCommission = new AlorColumn(AlorTableTypes.Money, "PlannedCommission", typeof(decimal), false);

		/// <summary>
		/// Планируемая комиссия брокера (учитываются выставленные заявки), в руб.
		/// </summary>
		public static readonly AlorColumn BrokerPlannedCommission = new AlorColumn(AlorTableTypes.Money, "PlannedCommission2", typeof(decimal), false);

		/// <summary>
		/// Доход, в руб.
		/// </summary>
		public static readonly AlorColumn PnL = new AlorColumn(AlorTableTypes.Money, "Profit", typeof(decimal), false);

		/// <summary>
		/// Свободные средства.
		/// </summary>
		public static readonly AlorColumn Free = new AlorColumn(AlorTableTypes.Money, "Free", typeof(decimal), false);

		/// <summary>
		/// Расчетное значение свободных средств для коротких продаж, в руб.
		/// </summary>
		public static readonly AlorColumn FreeForShorting = new AlorColumn(AlorTableTypes.Money, "FreeForShorting", typeof(decimal), false);

		/// <summary>
		/// Расчетное значение свободных средств для покупок маржинальных бумаг, в руб.
		/// </summary>
		public static readonly AlorColumn FreeForMargin = new AlorColumn(AlorTableTypes.Money, "FreeForMargin", typeof(decimal), false);

		/// <summary>
		/// Расчетное значение свободных средств для покупок немаржинальных бумаг, в руб.
		/// </summary>
		public static readonly AlorColumn FreeForNonMargin = new AlorColumn(AlorTableTypes.Money, "FreeForNonMargin", typeof(decimal), false);

		/// <summary>
		/// Текущий уровень маржи.
		/// </summary>
		public static readonly AlorColumn MarginLevel = new AlorColumn(AlorTableTypes.Money, "MarginLevel", typeof(decimal), false);

		/// <summary>
		/// Планируемый уровень маржи (с учетом выставленных заявок).
		/// </summary>
		public static readonly AlorColumn MarginPlannedLevel = new AlorColumn(AlorTableTypes.Money, "MarginLevelPlanned", typeof(decimal), false);

		/// <summary>
		/// Идентификатор фирмы.
		/// </summary>
		public static readonly AlorColumn FirmId = new AlorColumn(AlorTableTypes.Money, "FirmID", typeof(string), false);

		/// <summary>
		/// Код валюты.
		/// </summary>
		public static readonly AlorColumn Currency = new AlorColumn(AlorTableTypes.Money, "CurrCode", typeof(string), false);

		/// <summary>
		/// Код позиции.
		/// </summary>
		public static readonly AlorColumn Code = new AlorColumn(AlorTableTypes.Money, "Tag", typeof(string), false);

		/// <summary>
		/// Текущий лимит, в руб.
		/// </summary>
		public static readonly AlorColumn Limit = new AlorColumn(AlorTableTypes.Money, "CBPLimit", typeof(decimal), false);

		/// <summary>
		/// Накопленная по сделкам вариационная маржа, рассчитанная по текущей котировке, в руб.
		/// </summary>
		public static readonly AlorColumn VarMargin = new AlorColumn(AlorTableTypes.Money, "VarMargin", typeof(decimal), false);

		/// <summary>
        /// Код счета ("0" – деньги, "1" – залоги)
        /// </summary>
        public static readonly AlorColumn AccCode = new AlorColumn(AlorTableTypes.Money, "AccCode", typeof(string));
	}
}