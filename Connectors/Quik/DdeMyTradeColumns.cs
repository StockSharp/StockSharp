namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы моих сделок.
	/// </summary>
	public static class DdeMyTradeColumns
	{
		static DdeMyTradeColumns()
		{
			Id = new DdeTableColumn(DdeTableTypes.MyTrade, "Номер", typeof(long));
			SecurityCode = new DdeTableColumn(DdeTableTypes.MyTrade, "Код бумаги", typeof(string));
			SecurityClass = new DdeTableColumn(DdeTableTypes.MyTrade, "Код класса", typeof(string));
			Time = new DdeTableColumn(DdeTableTypes.MyTrade, "Время", typeof(DateTime));
			TimeMcs = new DdeTableColumn(DdeTableTypes.MyTrade, "Время (мкс)", typeof(int));
			Volume = new DdeTableColumn(DdeTableTypes.MyTrade, "Кол-во", typeof(decimal));
			Price = new DdeTableColumn(DdeTableTypes.MyTrade, "Цена", typeof(decimal));
			OrderId = new DdeTableColumn(DdeTableTypes.MyTrade, "Заявка", typeof(long));
			Date = new DdeTableColumn(DdeTableTypes.MyTrade, "Дата торгов", typeof(DateTime));

			MarketId = new DdeTableColumn(DdeTableTypes.MyTrade, "Код биржи", typeof(string));
			SecurityShortName = new DdeTableColumn(DdeTableTypes.MyTrade, "Бумага сокр.", typeof(string));
			SecurityName = new DdeTableColumn(DdeTableTypes.MyTrade, "Бумага", typeof(string));
			Type = new DdeTableColumn(DdeTableTypes.MyTrade, "Тип сделки", typeof(string));
			OrderDirection = new DdeTableColumn(DdeTableTypes.MyTrade, "Операция", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.MyTrade, "Счет", typeof(string));
			Value = new DdeTableColumn(DdeTableTypes.MyTrade, "Объем", typeof(decimal));
			Currency = new DdeTableColumn(DdeTableTypes.MyTrade, "Валюта", typeof(string));
			AccountCode = new DdeTableColumn(DdeTableTypes.MyTrade, "Код расчетов", typeof(string));
			Yield = new DdeTableColumn(DdeTableTypes.MyTrade, "Доходность", typeof(decimal));
			CouponYield = new DdeTableColumn(DdeTableTypes.MyTrade, "Купонный %", typeof(decimal));
			RepoRate = new DdeTableColumn(DdeTableTypes.MyTrade, "Ставка РЕПО(%)", typeof(decimal));
			RepoValue = new DdeTableColumn(DdeTableTypes.MyTrade, "Сумма РЕПО", typeof(decimal));
			RepoPayBack = new DdeTableColumn(DdeTableTypes.MyTrade, "Объем выкупа РЕПО", typeof(decimal));
			RepoDate = new DdeTableColumn(DdeTableTypes.MyTrade, "Срок РЕПО", typeof(int));
			Trader = new DdeTableColumn(DdeTableTypes.MyTrade, "Трейдер", typeof(string));
			Workstation = new DdeTableColumn(DdeTableTypes.MyTrade, "Идентификатор рабочей станции", typeof(string));
			Dealer = new DdeTableColumn(DdeTableTypes.MyTrade, "Дилер", typeof(string));
			TraderOrganization = new DdeTableColumn(DdeTableTypes.MyTrade, "Орг-я трейдера", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.MyTrade, "Код клиента", typeof(string));
			Comment = new DdeTableColumn(DdeTableTypes.MyTrade, "Комментарий", typeof(string));
			Partner = new DdeTableColumn(DdeTableTypes.MyTrade, "Партнер", typeof(string));
			PartnerOrganization = new DdeTableColumn(DdeTableTypes.MyTrade, "Орг-я партнера", typeof(string));
			Commission = new DdeTableColumn(DdeTableTypes.MyTrade, "Комиссия ТС", typeof(decimal));
			ClearingCommission = new DdeTableColumn(DdeTableTypes.MyTrade, "Клиринговая комиссия", typeof(decimal));
			StockMarketCommission = new DdeTableColumn(DdeTableTypes.MyTrade, "ФБ комиссия", typeof(decimal));
			TechnicalCenterCommission = new DdeTableColumn(DdeTableTypes.MyTrade, "ТЦ комиссия", typeof(decimal));
			Party = new DdeTableColumn(DdeTableTypes.MyTrade, "Идентификатор участника", typeof(string));
			SettlementDate = new DdeTableColumn(DdeTableTypes.MyTrade, "Дата расчетов", typeof(DateTime));
			RtsCurrency = new DdeTableColumn(DdeTableTypes.MyTrade, "Валюта сделки", typeof(string));
		}

		/// <summary>
		/// Регистрационный номер сделки в торговой системе биржи.
		/// </summary>
		public static DdeTableColumn Id { get; private set; }

		/// <summary>
		/// Идентификатор инструмента в торговой системе.
		/// </summary>
		public static DdeTableColumn SecurityCode { get; private set; }

		/// <summary>
		/// Наименование класса инструмента.
		/// </summary>
		public static DdeTableColumn SecurityClass { get; private set; }

		/// <summary>
		/// Время регистрации сделки в торговой системе.
		/// </summary>
		public static DdeTableColumn Time { get; private set; }

		/// <summary>
		/// Значение микросекунд для времени регистрации сделки в торговой системе.
		/// </summary>
		public static DdeTableColumn TimeMcs { get; private set; }

		/// <summary>
		/// Количество ценных бумаг, выраженное в лотах.
		/// </summary>
		public static DdeTableColumn Volume { get; private set; }

		/// <summary>
		/// Цена сделки, за единицу инструмента.
		/// </summary>
		public static DdeTableColumn Price { get; private set; }

		/// <summary>
		/// Номер заявки, на основании которой заключена сделка.
		/// </summary>
		public static DdeTableColumn OrderId { get; private set; }

		/// <summary>
		/// Дата регистрации сделки.
		/// </summary>
		public static DdeTableColumn Date { get; private set; }

		/// <summary>
		/// Сокращенное наименование ценной бумаги.
		/// </summary>
		public static DdeTableColumn SecurityShortName { get; private set; }

		/// <summary>
		/// Наименование ценной бумаги.
		/// </summary>
		public static DdeTableColumn SecurityName { get; private set; }

		/// <summary>
		/// Объем сделки в денежном выражении, рублей.
		/// </summary>
		public static DdeTableColumn Value { get; private set; }

		/// <summary>
		/// Код расчетов по сделке для Режима переговорных сделок (РПС) и операций РЕПО.
		/// </summary>
		public static DdeTableColumn AccountCode { get; private set; }

		/// <summary>
		/// Доходность инструмента, рассчитанная по цене совершенной сделки, %. Параметр относится к сделкам по облигациям.
		/// </summary>
		public static DdeTableColumn Yield { get; private set; }

		/// <summary>
		/// Накопленный купонный доход, рублей. Параметр относится к сделкам по облигациям.
		/// </summary>
		public static DdeTableColumn CouponYield { get; private set; }

		/// <summary>
		/// Ставка РЕПО, в процентах. Параметр операций РЕПО.
		/// </summary>
		public static DdeTableColumn RepoRate { get; private set; }

		/// <summary>
		/// Сумма РЕПО - сумма привлеченных/предоставленных по сделке РЕПО денежных средств, по состоянию на текущую дату, рублей.
		/// Параметр сделок РЕПО ГЦБ.
		/// </summary>
		public static DdeTableColumn RepoValue { get; private set; }

		/// <summary>
		/// Объем сделки выкупа РЕПО, рублей. Параметр сделок РЕПО ГЦБ.
		/// </summary>
		public static DdeTableColumn RepoPayBack { get; private set; }

		/// <summary>
		/// Срок РЕПО в календарных днях. Параметр сделок РЕПО.
		/// </summary>
		public static DdeTableColumn RepoDate { get; private set; }

		/// <summary>
		/// Идентификатор биржи.
		/// </summary>
		public static DdeTableColumn MarketId { get; private set; }

		/// <summary>
		/// Признак маржинальной сделки. Если результат сделки изменяет значение текущего лимита клиента,
		/// то указывается тип сделки «маржинальная», иначе поле пустое.
		/// </summary>
		public static DdeTableColumn Type { get; private set; }

		/// <summary>
		/// Направление операции (Купля/Продажа).
		/// </summary>
		public static DdeTableColumn OrderDirection { get; private set; }

		/// <summary>
		/// Код торгового счета, в отношении которого заключена сделка.
		/// </summary>
		public static DdeTableColumn Account { get; private set; }

		/// <summary>
		/// Валюта цены, например «SUR» - российский рубль.
		/// </summary>
		public static DdeTableColumn Currency { get; private set; }

		/// <summary>
		/// Идентификатор трейдера, совершившего сделку.
		/// </summary>
		public static DdeTableColumn Trader { get; private set; }

		/// <summary>
		/// Идентификатор рабочей станции РТС, через которую была совершена сделка.
		/// Параметр отображается только для сделок на Фондовой бирже РТС.
		/// </summary>
		public static DdeTableColumn Workstation { get; private set; }

		/// <summary>
		/// Идентификатор фирмы, от имени которой совершена сделка.
		/// </summary>
		public static DdeTableColumn Dealer { get; private set; }

		/// <summary>
		/// Идентификатор фирмы трейдера.
		/// </summary>
		public static DdeTableColumn TraderOrganization { get; private set; }

		/// <summary>
		/// Код клиента, по которому установлен лимит средств.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Дополнительная справочная информация (заполняется трейдером).
		/// </summary>
		public static DdeTableColumn Comment { get; private set; }

		/// <summary>
		/// Идентификатор трейдера, с кем заключена сделка (только для РПС).
		/// </summary>
		public static DdeTableColumn Partner { get; private set; }

		/// <summary>
		/// Идентификатор фирмы, с которой заключена сделка (только для РПС).
		/// </summary>
		public static DdeTableColumn PartnerOrganization { get; private set; }

		/// <summary>
		/// Комиссия торговой системы, взимаемая по сделке.
		/// </summary>
		public static DdeTableColumn Commission { get; private set; }

		/// <summary>
		/// Комиссия за клиринговые услуги. Параметр сделок на ММВБ.
		/// </summary>
		public static DdeTableColumn ClearingCommission { get; private set; }

		/// <summary>
		/// Комиссия Фондовой биржи.
		/// </summary>
		public static DdeTableColumn StockMarketCommission { get; private set; }

		/// <summary>
		/// Комиссия Технического центра. Параметр сделок на ММВБ.
		/// </summary>
		public static DdeTableColumn TechnicalCenterCommission { get; private set; }

		/// <summary>
		/// Идентификатор участника торгов в РТС.
		/// </summary>
		public static DdeTableColumn Party { get; private set; }

		/// <summary>
		/// Дата расчетов по сделке.
		/// </summary>
		public static DdeTableColumn SettlementDate { get; private set; }

		/// <summary>
		/// Валюта торгов сделки в РТС.
		/// </summary>
		public static DdeTableColumn RtsCurrency { get; private set; }
	}
}