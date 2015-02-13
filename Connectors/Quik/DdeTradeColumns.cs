namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы всех сделок.
	/// </summary>
	public static class DdeTradeColumns
	{
		static DdeTradeColumns()
		{
			Id = new DdeTableColumn(DdeTableTypes.Trade, "Номер", typeof(long));
			SecurityCode = new DdeTableColumn(DdeTableTypes.Trade, "Код бумаги", typeof(string));
			SecurityClass = new DdeTableColumn(DdeTableTypes.Trade, "Класс", typeof(string));
			Time = new DdeTableColumn(DdeTableTypes.Trade, "Время", typeof(DateTime));
			TimeMcs = new DdeTableColumn(DdeTableTypes.Trade, "Время (мкс)", typeof(int));
			Volume = new DdeTableColumn(DdeTableTypes.Trade, "Кол-во", typeof(decimal));
			Price = new DdeTableColumn(DdeTableTypes.Trade, "Цена", typeof(decimal));
			OrderDirection = new DdeTableColumn(DdeTableTypes.Trade, "Операция", typeof(string));
			Date = new DdeTableColumn(DdeTableTypes.Trade, "Дата", typeof(DateTime));

			SecurityShortName = new DdeTableColumn(DdeTableTypes.Trade, "Бумага сокр.", typeof(string));
			SecurityName = new DdeTableColumn(DdeTableTypes.Trade, "Бумага", typeof(string));
			Value = new DdeTableColumn(DdeTableTypes.Trade, "Объем", typeof(decimal));
			AccountCode = new DdeTableColumn(DdeTableTypes.Trade, "Код расчетов", typeof(string));
			Yield = new DdeTableColumn(DdeTableTypes.Trade, "Доходность", typeof(decimal));
			CouponYield = new DdeTableColumn(DdeTableTypes.Trade, "Купонный доход", typeof(decimal));
			RepoRate = new DdeTableColumn(DdeTableTypes.Trade, "Ставка РЕПО(%)", typeof(decimal));
			RepoValue = new DdeTableColumn(DdeTableTypes.Trade, "Сумма РЕПО", typeof(decimal));
			RepoPayBack = new DdeTableColumn(DdeTableTypes.Trade, "Объем выкупа РЕПО", typeof(decimal));
			RepoDate = new DdeTableColumn(DdeTableTypes.Trade, "Срок РЕПО", typeof(int));
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
		/// Направление операции, преведшей к сделке.
		/// </summary>
		public static DdeTableColumn OrderDirection { get; private set; }

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
		/// Объем сделки в денежном выражении, рублей
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
	}
}