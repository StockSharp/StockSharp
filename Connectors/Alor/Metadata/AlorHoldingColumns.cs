namespace StockSharp.Alor.Metadata
{
	/// <summary>
	/// Колонки системной таблицы HOLDING.
	/// </summary>
	public static class AlorHoldingColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Position, "ID", typeof(int), false);

		/// <summary>
		/// Идентификатор клиента.
		/// </summary>
		public static readonly AlorColumn ClientId = new AlorColumn(AlorTableTypes.Position, "ClientID", typeof(int), false);

		/// <summary>
		/// Код торгового счета.
		/// </summary>
		public static readonly AlorColumn Account = new AlorColumn(AlorTableTypes.Position, "Account", typeof(string));

		/// <summary>
		/// Ссылка типа &lt;счет клиента&gt;[/&lt;субсчет&gt;].
		/// </summary>
		public static readonly AlorColumn BrokerRef = new AlorColumn(AlorTableTypes.Position, "BrokerRef", typeof(string), false);

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn SecurityCode = new AlorColumn(AlorTableTypes.Position, "SecCode", typeof(string));

		/// <summary>
		/// Суммарный входящий остаток по данной ценной бумаге на всех торговых счетах клиента.
		/// </summary>
		public static readonly AlorColumn BeginValue = new AlorColumn(AlorTableTypes.Position, "OpenBal", typeof(int));

		/// <summary>
		/// Текущий остаток – входящий остаток плюс суммарный объем заключенных клиентом сделок на покупку минус суммарный объем сделок на продажу.
		/// </summary>
		public static readonly AlorColumn CurrentValue = new AlorColumn(AlorTableTypes.Position, "CurrentPos", typeof(int));

		/// <summary>
		/// Минимальное значение остатка.
		/// </summary>
		public static readonly AlorColumn MinValue = new AlorColumn(AlorTableTypes.Position, "Min", typeof(int), false);

		/// <summary>
		/// Объём активных заявок на покупку.
		/// </summary>
		public static readonly AlorColumn CurrentBidsVolume = new AlorColumn(AlorTableTypes.Position, "PlannedPosBuy", typeof(int));

		/// <summary>
		/// Объём активных заявок на продажу.
		/// </summary>
        public static readonly AlorColumn CurrentAsksVolume = new AlorColumn(AlorTableTypes.Position, "PlannedPosSell", typeof(int));
	    
    }
}