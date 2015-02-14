namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы STOPORDERS.
	/// </summary>
	public static class AlorStopOrderColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.StopOrder, "ID", typeof(int), false);

		/// <summary>
		/// Номер стоп-заявки.
		/// </summary>
		public static readonly AlorColumn OrderNo = new AlorColumn(AlorTableTypes.StopOrder, "OrderNo", typeof(long));

		/// <summary>
		/// Время регистрации стоп-заявки в торговой системе.
		/// </summary>
		public static readonly AlorColumn Time = new AlorColumn(AlorTableTypes.StopOrder, "Time", typeof(DateTime));

		/// <summary>
		/// Время снятия (отмены) стоп-заявки в торговой системе.
		/// </summary>
        public static readonly AlorColumn CancelTime = new AlorColumn(AlorTableTypes.StopOrder, "WithdrawTime", typeof(DateTime));

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityBoard = new AlorColumn(AlorTableTypes.StopOrder, "SecBoard", typeof(string));

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.StopOrder, "SecCode", typeof(string));

		/// <summary>
		/// Направленность заявки.
		/// </summary>
		public static readonly AlorColumn Direction = new AlorColumn(AlorTableTypes.StopOrder, "BuySell", typeof(string));

		/// <summary>
		/// Тип заявки.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.StopOrder, "MktLimit", typeof(string), false);

		/// <summary>
		/// Признак расщепления цены.
		/// </summary>
		public static readonly AlorColumn SplitPrice = new AlorColumn(AlorTableTypes.StopOrder, "SplitFlag", typeof(string), false);

		/// <summary>
		/// Условие исполнения.
		/// </summary>
		public static readonly AlorColumn ExecutionCondition = new AlorColumn(AlorTableTypes.StopOrder, "ImmCancel", typeof(string), false);

		/// <summary>
		/// Тип стоп-заявки.
		/// </summary>
		public static readonly AlorColumn StopType = new AlorColumn(AlorTableTypes.StopOrder, "StopType", typeof(string));

		/// <summary>
		/// Время окончания действия стоп-заявки в торговой системе.
		/// </summary>
		public static readonly AlorColumn ExpiryTime = new AlorColumn(AlorTableTypes.StopOrder, "EndTime", typeof(DateTime));

		/// <summary>
		/// Цена активизации стоп-заявки.
		/// </summary>
		public static readonly AlorColumn StopPrice = new AlorColumn(AlorTableTypes.StopOrder, "AlertPrice", typeof(decimal));

		/// <summary>
		/// Цена за одну ценную бумагу.
		/// </summary>
		public static readonly AlorColumn Price = new AlorColumn(AlorTableTypes.StopOrder, "Price", typeof(decimal));

		/// <summary>
		/// Количество ценных бумаг.
		/// </summary>
		public static readonly AlorColumn Volume = new AlorColumn(AlorTableTypes.StopOrder, "Quantity", typeof(int));

		/// <summary>
		/// Доходность.
		/// </summary>
		public static readonly AlorColumn Yield = new AlorColumn(AlorTableTypes.StopOrder, "Yield", typeof(decimal), false);

		/// <summary>
		/// Накопленный купонный доход.
		/// </summary>
		public static readonly AlorColumn AccruedInt = new AlorColumn(AlorTableTypes.StopOrder, "AccruedInt", typeof(decimal), false);

		/// <summary>
		/// Текущее состояние заявки.
		/// </summary>
		public static readonly AlorColumn State = new AlorColumn(AlorTableTypes.StopOrder, "Status", typeof(string));

		/// <summary>
		/// Ссылка типа &lt;счет клиента&gt;[/&lt;субсчет&gt;].
		/// </summary>
		public static readonly AlorColumn BrokerRef = new AlorColumn(AlorTableTypes.StopOrder, "BrokerRef", typeof(string));

		/// <summary>
		/// Код клиента.
		/// </summary>
		public static readonly AlorColumn BrokerId = new AlorColumn(AlorTableTypes.StopOrder, "BrokerID", typeof(string), false);

		/// <summary>
		/// Торговый счет.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.StopOrder, "Account", typeof(string));

		/// <summary>
		/// Поле-примечание.
		/// </summary>
		public static readonly AlorColumn ExtRef = new AlorColumn(AlorTableTypes.StopOrder, "ExtRef", typeof(string));
	}
}