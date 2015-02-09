namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы ORDERS.
	/// </summary>
	public static class AlorOrderColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Order, "ID", typeof(int), false);

		/// <summary>
		/// Идентификационный номер заявки в торговой системе.
		/// </summary>
		public static readonly AlorColumn OrderNo = new AlorColumn(AlorTableTypes.Order, "OrderNo", typeof(long));

		/// <summary>
		/// Время регистрации заявки в торговой системе.
		/// </summary>
		public static readonly AlorColumn Time = new AlorColumn(AlorTableTypes.Order, "Time", typeof(DateTime));

		/// <summary>
		/// Время снятия (отмены) заявки в торговой системе.
		/// </summary>
		public static readonly AlorColumn CancelTime = new AlorColumn(AlorTableTypes.Order, "WithdrawTime", typeof(DateTime));

		/// <summary>
		/// Текущее состояние заявки.
		/// </summary>
		public static readonly AlorColumn State = new AlorColumn(AlorTableTypes.Order, "Status", typeof(string));

		/// <summary>
		/// Тип заявки.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.Order, "MktLimit", typeof(string));

		/// <summary>
		/// Направленность заявки.
		/// </summary>
		public static readonly AlorColumn Direction = new AlorColumn(AlorTableTypes.Order, "BuySell", typeof(string));

		/// <summary>
		/// Признак расщепления цены.
		/// </summary>
		public static readonly AlorColumn SplitPrice = new AlorColumn(AlorTableTypes.Order, "SplitFlag", typeof(string), false);

		/// <summary>
		/// Условие исполнения.
		/// </summary>
		public static readonly AlorColumn ExecutionCondition = new AlorColumn(AlorTableTypes.Order, "ImmCancel", typeof(string));

		/// <summary>
		/// Ссылка типа &lt;счет клиента&gt;[/&lt;субсчет&gt;].
		/// </summary>
		public static readonly AlorColumn BrokerRef = new AlorColumn(AlorTableTypes.Order, "BrokerRef", typeof(string));

		/// <summary>
		/// Код клиента.
		/// </summary>
		public static readonly AlorColumn BrokerId = new AlorColumn(AlorTableTypes.Order, "BrokerID", typeof(string), false);

		/// <summary>
		/// Идентификатор трейдера.
		/// </summary>
		public static readonly AlorColumn UserId = new AlorColumn(AlorTableTypes.Order, "UserID", typeof(string), false);

		/// <summary>
		/// Идентификатор фирмы.
		/// </summary>
		public static readonly AlorColumn FirmId = new AlorColumn(AlorTableTypes.Order, "FirmID", typeof(string), false);

		/// <summary>
		/// Торговый счет.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.Order, "Account", typeof(string));

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityBoard = new AlorColumn(AlorTableTypes.Order, "SecBoard", typeof(string));

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.Order, "SecCode", typeof(string));

		/// <summary>
		/// Цена за одну ценную бумагу.
		/// </summary>
		public static readonly AlorColumn Price = new AlorColumn(AlorTableTypes.Order, "Price", typeof(decimal));

		/// <summary>
		/// Цена выкупа второй части РЕПО.
		/// </summary>
		public static readonly AlorColumn Price2 = new AlorColumn(AlorTableTypes.Order, "Price2", typeof(decimal), false);

		/// <summary>
		/// Количество ценных бумаг.
		/// </summary>
		public static readonly AlorColumn Volume = new AlorColumn(AlorTableTypes.Order, "Quantity", typeof(int));

		/// <summary>
		/// Объем неисполненной части заявки.
		/// </summary>
		public static readonly AlorColumn Balance = new AlorColumn(AlorTableTypes.Order, "Balance", typeof(int));

		/// <summary>
		/// Объем заявки (без учета комиссионного сбора Биржи и % дохода).
		/// </summary>
		public static readonly AlorColumn Value = new AlorColumn(AlorTableTypes.Order, "Value", typeof(decimal), false);

		/// <summary>
		/// Накопленный купонный доход.
		/// </summary>
		public static readonly AlorColumn AccruedInt = new AlorColumn(AlorTableTypes.Order, "AccruedInt", typeof(decimal), false);

		/// <summary>
		/// Тип ввода значения цены.
		/// </summary>
		public static readonly AlorColumn EnterType = new AlorColumn(AlorTableTypes.Order, "EnterType", typeof(string), false);

		/// <summary>
		/// Доходность.
		/// </summary>
		public static readonly AlorColumn Yield = new AlorColumn(AlorTableTypes.Order, "Yield", typeof(decimal), false);

		/// <summary>
		/// Период торгов.
		/// </summary>
		public static readonly AlorColumn Period = new AlorColumn(AlorTableTypes.Order, "Period", typeof(string), false);

		/// <summary>
		/// Поле-примечание.
		/// </summary>
		public static readonly AlorColumn ExtRef = new AlorColumn(AlorTableTypes.Order, "ExtRef", typeof(string));

		/// <summary>
		/// Заявка Маркет-Мейкера.
		/// </summary>
		public static readonly AlorColumn MarketMaker = new AlorColumn(AlorTableTypes.Order, "MarketMaker", typeof(string), false);
	}
}