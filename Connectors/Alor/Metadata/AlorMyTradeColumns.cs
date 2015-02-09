namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы TRADES.
	/// </summary>
	public static class AlorMyTradeColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.MyTrade, "ID", typeof(int), false);

		/// <summary>
		/// Номер сделки в торговой системе.
		/// </summary>
		public static readonly AlorColumn TradeNo = new AlorColumn(AlorTableTypes.MyTrade, "TradeNo", typeof(long));

		/// <summary>
		/// Номер заявки.
		/// </summary>
		public static readonly AlorColumn OrderNo = new AlorColumn(AlorTableTypes.MyTrade, "OrderNo", typeof(long));

		/// <summary>
		/// Время регистрации сделки в торговой системе.
		/// </summary>
		public static readonly AlorColumn Time = new AlorColumn(AlorTableTypes.MyTrade, "Time", typeof(DateTime));

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityBoard = new AlorColumn(AlorTableTypes.MyTrade, "SecBoard", typeof(string));

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.MyTrade, "SecCode", typeof(string));

		/// <summary>
		/// Код выпуска ценной бумаги.
		/// </summary>
        public static readonly AlorColumn IssueCode = new AlorColumn(AlorTableTypes.MyTrade, "IssueCode", typeof(string), false);

		/// <summary>
		/// Направленность заявки.
		/// </summary>
        public static readonly AlorColumn OrderDirection = new AlorColumn(AlorTableTypes.MyTrade, "BuySell", typeof(string));

		/// <summary>
		/// Объем сделки.
		/// </summary>
		public static readonly AlorColumn Volume = new AlorColumn(AlorTableTypes.MyTrade, "Quantity", typeof(int));

		/// <summary>
		/// Цена за одну ценную бумагу.
		/// </summary>
		public static readonly AlorColumn Price = new AlorColumn(AlorTableTypes.MyTrade, "Price", typeof(decimal));

		/// <summary>
		/// Объем сделки, в руб.
		/// </summary>
        public static readonly AlorColumn Value = new AlorColumn(AlorTableTypes.MyTrade, "Value", typeof(decimal), false);

		/// <summary>
		/// Доходность.
		/// </summary>
        public static readonly AlorColumn Yield = new AlorColumn(AlorTableTypes.MyTrade, "Yield", typeof(decimal), false);

		/// <summary>
		/// Накопленный купонный доход.
		/// </summary>
        public static readonly AlorColumn AccruedInt = new AlorColumn(AlorTableTypes.MyTrade, "AccruedInt", typeof(decimal), false);

		/// <summary>
		/// Ставка РЕПО.
		/// </summary>
        public static readonly AlorColumn RepoRate = new AlorColumn(AlorTableTypes.MyTrade, "RepoRate", typeof(decimal), false);

		/// <summary>
		/// Срок РЕПО.
		/// </summary>
        public static readonly AlorColumn RepoTerm = new AlorColumn(AlorTableTypes.MyTrade, "RepoTerm", typeof(int), false);

		/// <summary>
		/// Начальный дисконт.
		/// </summary>
        public static readonly AlorColumn StartDiscount = new AlorColumn(AlorTableTypes.MyTrade, "StartDiscount", typeof(decimal), false);

		/// <summary>
		/// Объем комиссии по сделке.
		/// </summary>
        public static readonly AlorColumn Commission = new AlorColumn(AlorTableTypes.MyTrade, "Commission", typeof(decimal), false);

		/// <summary>
		/// Период торговой сессии.
		/// </summary>
        public static readonly AlorColumn Period = new AlorColumn(AlorTableTypes.MyTrade, "Period", typeof(string), false);

		/// <summary>
		/// Тип сделки.
		/// </summary>
        public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.MyTrade, "TradeType", typeof(string), false);

		/// <summary>
		/// Код расчетов по сделке.
		/// </summary>
        public static readonly AlorColumn SettlementCode = new AlorColumn(AlorTableTypes.MyTrade, "SettleCode", typeof(string), false);

		/// <summary>
		/// Ссылка типа &lt;счет клиента&gt;[/&lt;субсчет&gt;].
		/// </summary>
        public static readonly AlorColumn BrokerRef = new AlorColumn(AlorTableTypes.MyTrade, "BrokerRef", typeof(string), false);

	    /// <summary>
	    /// Код клиента.
	    /// </summary>
	    public static readonly AlorColumn BrokerId = new AlorColumn(AlorTableTypes.MyTrade, "BrokerID", typeof (string), false);

		/// <summary>
		/// Идентификатор трейдера.
		/// </summary>
        public static readonly AlorColumn UserId = new AlorColumn(AlorTableTypes.MyTrade, "UserID", typeof(string), false);

		/// <summary>
		/// Идентификатор фирмы.
		/// </summary>
        public static readonly AlorColumn FirmId = new AlorColumn(AlorTableTypes.MyTrade, "FirmID", typeof(string), false);

		/// <summary>
		/// Торговый счет.
		/// </summary>
        public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.MyTrade, "Account", typeof(string), false);

		/// <summary>
		/// Поле-примечание.
		/// </summary>
		public static readonly AlorColumn ExtRef = new AlorColumn(AlorTableTypes.MyTrade, "ExtRef", typeof(string));
	}
}