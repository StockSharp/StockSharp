namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы ALL_TRADES.
	/// </summary>
	public static class AlorTradeColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Trade, "ID", typeof(int), false);

		/// <summary>
		/// Номер сделки в торговой системе.
		/// </summary>
		public static readonly AlorColumn TradeNo = new AlorColumn(AlorTableTypes.Trade, "TradeNo", typeof(long));

		/// <summary>
		/// Время регистрации сделки в торговой системе.
		/// </summary>
		public static readonly AlorColumn Time = new AlorColumn(AlorTableTypes.Trade, "Time", typeof(DateTime));

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityBoard = new AlorColumn(AlorTableTypes.Trade, "SecBoard", typeof(string));

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.Trade, "SecCode", typeof(string));

		/// <summary>
		/// Объем сделки.
		/// </summary>
		public static readonly AlorColumn Volume = new AlorColumn(AlorTableTypes.Trade, "Quantity", typeof(int));

		/// <summary>
		/// Цена за одну ценную бумагу.
		/// </summary>
		public static readonly AlorColumn Price = new AlorColumn(AlorTableTypes.Trade, "Price", typeof(decimal));

		/// <summary>
		/// Номер сделки в торговой системе.
		/// </summary>
		public static readonly AlorColumn State = new AlorColumn(AlorTableTypes.Trade, "RowState", typeof(long));

		/// <summary>
		/// Объем сделки.
		/// </summary>
        public static readonly AlorColumn Value = new AlorColumn(AlorTableTypes.Trade, "Value", typeof(decimal), false);

		/// <summary>
		/// Доходность.
		/// </summary>
        public static readonly AlorColumn Yield = new AlorColumn(AlorTableTypes.Trade, "Yield", typeof(decimal), false);

		/// <summary>
		/// Накопленный купонный доход.
		/// </summary>
        public static readonly AlorColumn AccruedInt = new AlorColumn(AlorTableTypes.Trade, "AccruedInt", typeof(decimal), false);

		/// <summary>
		/// Ставка РЕПО.
		/// </summary>
        public static readonly AlorColumn RepoRate = new AlorColumn(AlorTableTypes.Trade, "RepoRate", typeof(decimal), false);

		/// <summary>
		/// Срок РЕПО.
		/// </summary>
        public static readonly AlorColumn RepoTerm = new AlorColumn(AlorTableTypes.Trade, "RepoTerm", typeof(int), false);

		/// <summary>
		/// Начальный дисконт.
		/// </summary>
        public static readonly AlorColumn StartDiscount = new AlorColumn(AlorTableTypes.Trade, "StartDiscount", typeof(decimal), false);

		/// <summary>
		/// Период торговой сессии.
		/// </summary>
        public static readonly AlorColumn Period = new AlorColumn(AlorTableTypes.Trade, "Period", typeof(string), false);

		/// <summary>
		/// Тип сделки.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.Trade, "TradeType", typeof(string), false);

		/// <summary>
		/// Код расчетов по сделке.
		/// </summary>
        public static readonly AlorColumn SettlementCode = new AlorColumn(AlorTableTypes.Trade, "SettleCode", typeof(string), false);
	}
}