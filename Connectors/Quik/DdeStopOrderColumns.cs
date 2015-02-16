namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы стоп-заявок.
	/// </summary>
	public static class DdeStopOrderColumns
	{
		static DdeStopOrderColumns()
		{
			Id = new DdeTableColumn(DdeTableTypes.StopOrder, "Номер", typeof(long));
			SecurityCode = new DdeTableColumn(DdeTableTypes.StopOrder, "Код бумаги", typeof(string));
			SecurityClass = new DdeTableColumn(DdeTableTypes.StopOrder, "Код класса", typeof(string));
			Time = new DdeTableColumn(DdeTableTypes.StopOrder, "Время", typeof(DateTime));
			Volume = new DdeTableColumn(DdeTableTypes.StopOrder, "Кол-во", typeof(decimal));
			Price = new DdeTableColumn(DdeTableTypes.StopOrder, "Цена", typeof(decimal));
			Type = new DdeTableColumn(DdeTableTypes.StopOrder, "Тип стоп-заявки", typeof(string));
			State = new DdeTableColumn(DdeTableTypes.StopOrder, "Состояние", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.StopOrder, "Счет", typeof(string));
			Balance = new DdeTableColumn(DdeTableTypes.StopOrder, "Акт.кол-во", typeof(decimal));
			Comment = new DdeTableColumn(DdeTableTypes.StopOrder, "Комментарий", typeof(string));
			CancelTime = new DdeTableColumn(DdeTableTypes.StopOrder, "Время снятия", typeof(DateTime));
			Direction = new DdeTableColumn(DdeTableTypes.StopOrder, "Операция", typeof(string));
			TransactionId = new DdeTableColumn(DdeTableTypes.StopOrder, "ID транзакции", typeof(long));
			TypeCode = new DdeTableColumn(DdeTableTypes.StopOrder, "Тип", typeof(string));
			DerivedOrderId = new DdeTableColumn(DdeTableTypes.StopOrder, "Номер заявки", typeof(long));
			OtherSecurityClass = new DdeTableColumn(DdeTableTypes.StopOrder, "Код класса стоп-цены", typeof(string));
			OtherSecurityCode = new DdeTableColumn(DdeTableTypes.StopOrder, "Код бумаги стоп-цены", typeof(string));
			StopPrice = new DdeTableColumn(DdeTableTypes.StopOrder, "Стоп-цена", typeof(decimal));
			StopPriceCondition = new DdeTableColumn(DdeTableTypes.StopOrder, "Направление стоп-цены", typeof(string));
			StopLimitCondition = new DdeTableColumn(DdeTableTypes.StopOrder, "Направление стоп-лимит цены", typeof(string));
			StopLimitMarket = new DdeTableColumn(DdeTableTypes.StopOrder, "Стоп-лимит по рыночной", typeof(string));
			StopLimitPrice = new DdeTableColumn(DdeTableTypes.StopOrder, "Стоп-лимит цена", typeof(decimal));
			ExpiryDate = new DdeTableColumn(DdeTableTypes.StopOrder, "Срок", typeof(DateTime));
			LinkedOrderId = new DdeTableColumn(DdeTableTypes.StopOrder, "Связ. заявка", typeof(long));
			LinkedOrderPrice = new DdeTableColumn(DdeTableTypes.StopOrder, "Цена связ.заявки", typeof(decimal));
			OffsetType = new DdeTableColumn(DdeTableTypes.StopOrder, "Единицы отступа", typeof(string));
			OffsetValue = new DdeTableColumn(DdeTableTypes.StopOrder, "Отступ от min/max", typeof(decimal));
			SpreadType = new DdeTableColumn(DdeTableTypes.StopOrder, "Единицы спрэда", typeof(string));
			SpreadValue = new DdeTableColumn(DdeTableTypes.StopOrder, "Защитный спрэд", typeof(decimal));
			ActiveTime = new DdeTableColumn(DdeTableTypes.StopOrder, "Время действия", typeof(string));
			ActiveFrom = new DdeTableColumn(DdeTableTypes.StopOrder, "Активна с", typeof(DateTime));
			ActiveTo = new DdeTableColumn(DdeTableTypes.StopOrder, "Активна по", typeof(DateTime));
			TakeProfitMarket = new DdeTableColumn(DdeTableTypes.StopOrder, "Тэйк-профит по рыночной", typeof(string));
			ConditionOrderId = new DdeTableColumn(DdeTableTypes.StopOrder, "Заявка условия", typeof(long));
			Date = new DdeTableColumn(DdeTableTypes.StopOrder, "Дата", typeof(DateTime));

			SecurityShortName = new DdeTableColumn(DdeTableTypes.StopOrder, "Бумага сокр.", typeof(string));
			SecurityName = new DdeTableColumn(DdeTableTypes.StopOrder, "Бумага", typeof(string));
			User = new DdeTableColumn(DdeTableTypes.StopOrder, "UID", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.StopOrder, "Код клиента", typeof(string));
			TypeDescription = new DdeTableColumn(DdeTableTypes.StopOrder, "Описание типа стоп-заявки", typeof(string));
			TradeId = new DdeTableColumn(DdeTableTypes.StopOrder, "Сделка условия", typeof(long));
			Result = new DdeTableColumn(DdeTableTypes.StopOrder, "Результат", typeof(string));
			Server = new DdeTableColumn(DdeTableTypes.StopOrder, "Сервер", typeof(string));
		}

		/// <summary>
		/// Регистрационный номер стоп-заявки на сервере QUIK.
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
		/// Время регистрации стоп-заявки на сервере QUIK.
		/// </summary>
		public static DdeTableColumn Time { get; private set; }

		/// <summary>
		/// Тип стоп-заявки, состоящий из специальных кодов.
		/// </summary>
		public static DdeTableColumn TypeCode { get; private set; }

		/// <summary>
		/// Тип стоп-заявки.
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
		/// Количество ценных бумаг в активной условной заявке, ожидающей наступления условия. 
		/// </summary>
		public static DdeTableColumn Balance { get; private set; }

		/// <summary>
		/// Количество ценных бумаг, указанное в заявке, выраженное в лотах.
		/// </summary>
		public static DdeTableColumn Volume { get; private set; }

		/// <summary>
		/// Дополнительная справочная информация.
		/// </summary>
		public static DdeTableColumn Comment { get; private set; }

		/// <summary>
		/// Время снятия стоп-заявки на сервере QUIK.
		/// </summary>
		public static DdeTableColumn CancelTime { get; private set; }

		/// <summary>
		/// Направление операции («Купля», «Продажа»).
		/// </summary>
		public static DdeTableColumn Direction { get; private set; }

		/// <summary>
		/// Номер заявки в торговой системе, зарегистрированной по наступлению условия стоп-цены.
		/// </summary>
		public static DdeTableColumn DerivedOrderId { get; private set; }

		/// <summary>
		/// Цена условия, при котором происходит начало расчета максимума (минимума) цены для заявок типа «тэйк-профит», за единицу инструмента.
		/// </summary>
		public static DdeTableColumn StopPrice { get; private set; }

		/// <summary>
		/// Отношение стоп-цены к цене последней сделки.
		/// </summary>
		public static DdeTableColumn StopPriceCondition { get; private set; }

		/// <summary>
		/// Идентификатор инструмента, указанного в «Бумага стоп-цены».
		/// </summary>
		public static DdeTableColumn OtherSecurityCode { get; private set; }

		/// <summary>
		/// Наименования класса инструмента, указанного в «Бумага стоп-цены».
		/// </summary>
		public static DdeTableColumn OtherSecurityClass { get; private set; }

		/// <summary>
		/// Срок исполнения заявки.
		/// </summary>
		public static DdeTableColumn ExpiryDate { get; private set; }

		/// <summary>
		/// Регистрационный номер заявки в торговой системе, присвоенный связанной заявке.
		/// </summary>
		public static DdeTableColumn LinkedOrderId { get; private set; }

		/// <summary>
		/// Цена, указанная в связанной заявке.
		/// </summary>
		public static DdeTableColumn LinkedOrderPrice { get; private set; }

		/// <summary>
		/// Единица измерения <see cref="OffsetValue"/>.
		/// </summary>
		public static DdeTableColumn OffsetType { get; private set; }

		/// <summary>
		/// Величина отступа.
		/// </summary>
		public static DdeTableColumn OffsetValue { get; private set; }

		/// <summary>
		/// Единица измерения <see cref="SpreadValue"/>.
		/// </summary>
		public static DdeTableColumn SpreadType { get; private set; }

		/// <summary>
		/// Дополнительное отклонение цены заявки от цены последней сделки, инициировавшей исполнение условной заявки.
		/// </summary>
		public static DdeTableColumn SpreadValue { get; private set; }

		/// <summary>
		/// Цена условия, при котором происходит выставление заявок типа «стоп-лимит» и «со связанной заявкой», за единицу инструмента.
		/// </summary>
		public static DdeTableColumn StopLimitPrice { get; private set; }

		/// <summary>
		/// Отношение стоп-лимит цены к цене последней сделки.
		/// </summary>
		public static DdeTableColumn StopLimitCondition { get; private set; }

		/// <summary>
		/// Признак исполнения заявки «Стоп-лимит» по рыночной цене.
		/// </summary>
		public static DdeTableColumn StopLimitMarket { get; private set; }

		/// <summary>
		/// Признак проверки условий заявки только в течение заданного периода времени.
		/// </summary>
		public static DdeTableColumn ActiveTime { get; private set; }

		/// <summary>
		/// Время начала действия стоп-заявки.
		/// </summary>
		public static DdeTableColumn ActiveFrom { get; private set; }

		/// <summary>
		/// Время окончания действия стоп-заявки.
		/// </summary>
		public static DdeTableColumn ActiveTo { get; private set; }

		/// <summary>
		/// Признак исполнения заявки «Тэйк-профит» по рыночной цене.
		/// </summary>
		public static DdeTableColumn TakeProfitMarket { get; private set; }

		/// <summary>
		/// Регистрационный номер заявки-условия в торговой системе.
		/// </summary>
		public static DdeTableColumn ConditionOrderId { get; private set; }

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
		/// Код пользователя на сервере QUIK.
		/// </summary>
		public static DdeTableColumn User { get; private set; }

		/// <summary>
		/// Код клиента, по которому установлен лимит средств.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Расширенное описание типа стоп-заявки.
		/// </summary>
		public static DdeTableColumn TypeDescription { get; private set; }

		/// <summary>
		/// Номер сделки в Таблице всех сделок, значение цены которой стало достаточным условием для исполнения стоп-заявки.
		/// </summary>
		public static DdeTableColumn TradeId { get; private set; }

		/// <summary>
		/// Результат исполнения стоп-заявки.
		/// </summary>
		public static DdeTableColumn Result { get; private set; }

		/// <summary>
		/// Сервер, на котором была поставлена стоп-заявка. Возможные значения: «Текущий», «Другой».
		/// </summary>
		public static DdeTableColumn Server { get; private set; }
	}
}