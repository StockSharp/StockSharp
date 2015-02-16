namespace StockSharp.Quik
{
	/// <summary>
	/// Колонки таблицы Позиции по бумагам.
	/// </summary>
	public static class DdeEquityPositionColumns
	{
		static DdeEquityPositionColumns()
		{
			FirmId = new DdeTableColumn(DdeTableTypes.EquityPosition, "Фирма", typeof(string));
			GroupCode = new DdeTableColumn(DdeTableTypes.EquityPosition, "Группа", typeof(string));
			SecurityCode = new DdeTableColumn(DdeTableTypes.EquityPosition, "Код бумаги", typeof(string));
			SecurityShortName = new DdeTableColumn(DdeTableTypes.EquityPosition, "Название бумаги", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.EquityPosition, "Счет депо", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.EquityPosition, "Код клиента", typeof(string));
			BeginPosition = new DdeTableColumn(DdeTableTypes.EquityPosition, "Входящий остаток", typeof(long));
			BeginLimit = new DdeTableColumn(DdeTableTypes.EquityPosition, "Входящий лимит", typeof(long));
			CurrentPosition = new DdeTableColumn(DdeTableTypes.EquityPosition, "Текущий остаток", typeof(long));
			CurrentLimit = new DdeTableColumn(DdeTableTypes.EquityPosition, "Текущий лимит", typeof(long));
			BlockedPosition = new DdeTableColumn(DdeTableTypes.EquityPosition, "Заблокировано", typeof(long));
			BuyPrice = new DdeTableColumn(DdeTableTypes.EquityPosition, "Цена приобретения", typeof(decimal));
			LimitType = new DdeTableColumn(DdeTableTypes.EquityPosition, "Вид лимита", typeof(string));
		}

		/// <summary>
		/// Идентификатор участника торгов в торговой системе.
		/// </summary>
		public static DdeTableColumn FirmId { get; private set; }

		/// <summary>
		/// Идентификатор торговой сессии, в которой ведется лимит.
		/// </summary>
		public static DdeTableColumn GroupCode { get; private set; }

		/// <summary>
		/// Идентификатор инструмента в торговой системе.
		/// </summary>
		public static DdeTableColumn SecurityCode { get; private set; }

		/// <summary>
		/// Сокращенное наименование инструмента.
		/// </summary>
		public static DdeTableColumn SecurityShortName { get; private set; }

		/// <summary>
		/// Счет депо, на котором учитываются средства клиента.
		/// </summary>
		public static DdeTableColumn Account { get; private set; }

		/// <summary>
		/// Код клиента в системе QUIK, на которого установлен лимит.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Сумма собственных средств клиента до совершения операций.
		/// </summary>
		public static DdeTableColumn BeginPosition { get; private set; }

		/// <summary>
		/// Разрешенная сумма заемных средств до совершения операций.
		/// </summary>
		public static DdeTableColumn BeginLimit { get; private set; }

		/// <summary>
		/// Сумма собственных средств клиента на текущий момент.
		/// </summary>
		public static DdeTableColumn CurrentPosition { get; private set; }

		/// <summary>
		/// Разрешенная сумма заемных средств на текущий момент.
		/// </summary>
		public static DdeTableColumn CurrentLimit { get; private set; }

		/// <summary>
		/// Сумма средств, заблокированных под исполнение заявок клиента.
		/// </summary>
		public static DdeTableColumn BlockedPosition { get; private set; }

		/// <summary>
		/// Средневзвешенная цена приобретения, рассчитанная по сделкам клиента.
		/// </summary>
		public static DdeTableColumn BuyPrice { get; private set; }

		/// <summary>
		/// Вид лимита для Т+ рынка.
		/// </summary>
		public static DdeTableColumn LimitType { get; private set; }
	}
}