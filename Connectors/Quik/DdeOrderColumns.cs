namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы заявок.
	/// </summary>
	public static class DdeOrderColumns
	{
		static DdeOrderColumns()
		{
			Id = new DdeTableColumn(DdeTableTypes.Order, "Номер", typeof(long));
			SecurityCode = new DdeTableColumn(DdeTableTypes.Order, "Код бумаги", typeof(string));
			SecurityClass = new DdeTableColumn(DdeTableTypes.Order, "Код класса", typeof(string));
			Time = new DdeTableColumn(DdeTableTypes.Order, "Выставлена (время)", typeof(DateTime));
			TimeMcs = new DdeTableColumn(DdeTableTypes.Order, "Выставлена (мкс)", typeof(int));
			Volume = new DdeTableColumn(DdeTableTypes.Order, "Кол-во", typeof(decimal));
			Price = new DdeTableColumn(DdeTableTypes.Order, "Цена", typeof(decimal));
			Type = new DdeTableColumn(DdeTableTypes.Order, "Тип", typeof(string));
			State = new DdeTableColumn(DdeTableTypes.Order, "Состояние", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.Order, "Счет", typeof(string));
			Balance = new DdeTableColumn(DdeTableTypes.Order, "Остаток", typeof(decimal));
			Comment = new DdeTableColumn(DdeTableTypes.Order, "Комментарий", typeof(string));
			CancelTime = new DdeTableColumn(DdeTableTypes.Order, "Снята (время)", typeof(DateTime));
			CancelTimeMcs = new DdeTableColumn(DdeTableTypes.Order, "Снята (мкс)", typeof(int));
			Direction = new DdeTableColumn(DdeTableTypes.Order, "Операция", typeof(string));
			TransactionId = new DdeTableColumn(DdeTableTypes.Order, "ID транзакции", typeof(long));
			Date = new DdeTableColumn(DdeTableTypes.Order, "Дата", typeof(DateTime));

			MarketId = new DdeTableColumn(DdeTableTypes.Order, "Код биржи", typeof(string));
			SecurityShortName = new DdeTableColumn(DdeTableTypes.Order, "Бумага сокр.", typeof(string));
			SecurityName = new DdeTableColumn(DdeTableTypes.Order, "Бумага", typeof(string));
			Value = new DdeTableColumn(DdeTableTypes.Order, "Объем", typeof(decimal));
			Currency = new DdeTableColumn(DdeTableTypes.Order, "Валюта", typeof(string));
			Yield = new DdeTableColumn(DdeTableTypes.Order, "Доходность", typeof(decimal));
			CouponYield = new DdeTableColumn(DdeTableTypes.Order, "Купонный процент", typeof(decimal));
			Trader = new DdeTableColumn(DdeTableTypes.Order, "Трейдер", typeof(string));
			Dealer = new DdeTableColumn(DdeTableTypes.Order, "Дилер", typeof(string));
			User = new DdeTableColumn(DdeTableTypes.Order, "UID", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.Order, "Код клиента", typeof(string));
			AccountCode = new DdeTableColumn(DdeTableTypes.Order, "Код расчетов", typeof(string));
			ActivationTime = new DdeTableColumn(DdeTableTypes.Order, "Время активации", typeof(DateTime));
			MarketMaker = new DdeTableColumn(DdeTableTypes.Order, "Заявка Маркет-мейкера", typeof(string));
			ExpiryDate = new DdeTableColumn(DdeTableTypes.Order, "Срок", typeof(DateTime));
		}

		/// <summary>
		/// Регистрационный номер заявки в торговой системе биржи.
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
		/// Время регистрации заявки в торговой системе.
		/// </summary>
		public static DdeTableColumn Time { get; private set; }

		/// <summary>
		/// Значение микросекунд для времени регистрации заявки в торговой системе.
		/// </summary>
		public static DdeTableColumn TimeMcs { get; private set; }

		/// <summary>
		/// Тип заявки.
		/// </summary>
		public static DdeTableColumn Type { get; private set; }

		/// <summary>
		/// Состояние заявки («Активна», «Исполнена», «Снята»).
		/// </summary>
		public static DdeTableColumn State { get; private set; }

		/// <summary>
		/// Код торгового счета, по которому подана заявка.
		/// </summary>
		public static DdeTableColumn Account { get; private set; }

		/// <summary>
		/// Цена заявки, за единицу инструмента.
		/// </summary>
		public static DdeTableColumn Price { get; private set; }

		/// <summary>
		/// Объем неисполненной части заявки, выраженный в лотах.
		/// </summary>
		public static DdeTableColumn Balance { get; private set; }

		/// <summary>
		/// Количество ценных бумаг, выраженное в лотах.
		/// </summary>
		public static DdeTableColumn Volume { get; private set; }

		/// <summary>
		/// Дополнительная справочная информация.
		/// </summary>
		public static DdeTableColumn Comment { get; private set; }

		/// <summary>
		/// Время отмены заявки в торговой системе.
		/// </summary>
		public static DdeTableColumn CancelTime { get; private set; }

		/// <summary>
		/// Значение микросекунд для времени отмены заявки в торговой системе.
		/// </summary>
		public static DdeTableColumn CancelTimeMcs { get; private set; }

		/// <summary>
		/// Направление операции («Купля», «Продажа»).
		/// </summary>
		public static DdeTableColumn Direction { get; private set; }

		/// <summary>
		/// Значение номера транзакции.
		/// </summary>
		public static DdeTableColumn TransactionId { get; private set; }

		/// <summary>
		/// Дата регистрации заявки.
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
		/// Идентификатор биржи.
		/// </summary>
		public static DdeTableColumn MarketId { get; private set; }

		/// <summary>
		/// Идентификатор трейдера, подавшего заявку.
		/// </summary>
		public static DdeTableColumn Trader { get; private set; }

		/// <summary>
		/// Идентификатор фирмы, от имени которой подана заявка.
		/// </summary>
		public static DdeTableColumn Dealer { get; private set; }

		/// <summary>
		/// Валюта цены, например «SUR» - российский рубль.
		/// </summary>
		public static DdeTableColumn Currency { get; private set; }

		/// <summary>
		/// Доходность в %, рассчитанная по цене заявки.
		/// </summary>
		public static DdeTableColumn Yield { get; private set; }

		/// <summary>
		/// Накопленный купонный доход, рассчитанный для указанного в заявке количества ценных бумаг, в денежном выражении.
		/// </summary>
		public static DdeTableColumn CouponYield { get; private set; }

		/// <summary>
		/// Объем заявки (без учета комиссионного сбора биржи и накопленного дохода) в денежном выражении.
		/// </summary>
		public static DdeTableColumn Value { get; private set; }

		/// <summary>
		/// Код клиента, по которому установлен лимит средств.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Код расчетов по сделке для Режима переговорных сделок (РПС) и операций РЕПО.
		/// </summary>
		public static DdeTableColumn AccountCode { get; private set; }

		/// <summary>
		/// Код пользователя на сервере QUIK.
		/// </summary>
		public static DdeTableColumn User { get; private set; }

		/// <summary>
		/// Время активации заявки.
		/// </summary>
		public static DdeTableColumn ActivationTime { get; private set; }

		/// <summary>
		/// Признак заявок, отправленных маркет-мейкером.
		/// </summary>
		public static DdeTableColumn MarketMaker { get; private set; }

		/// <summary>
		/// Срок исполнения заявки.
		/// </summary>
		public static DdeTableColumn ExpiryDate { get; private set; }
	}
}