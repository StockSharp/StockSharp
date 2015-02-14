namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы Позиции по деривативам.
	/// </summary>
	public static class DdeDerivativePositionColumns
	{
		static DdeDerivativePositionColumns()
		{
			FirmId = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Фирма", typeof(string));
			Type = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Тип", typeof(string));
			SecurityCode = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Код инструмента", typeof(string));
			SecurityShortName = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Краткое название", typeof(string));
			Account = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Торговый счет", typeof(string));
			SettlementDate = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Дата погашения", typeof(DateTime));
			BeginPosition = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Вход. чист. поз.", typeof(long));
			CurrentPosition = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Тек. чист. поз.", typeof(long));

			CurrentBidsVolume = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Акт. покупка", typeof(long));
			CurrentAsksVolume = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Акт. продажа", typeof(long));
			CurrentPositionPrice = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Оценка тек. чист. поз.", typeof(decimal));
			PlanningPositionPrice = new DdeTableColumn(DdeTableTypes.DerivativePosition, "План. чист. поз.", typeof(decimal));
			VariationMargin = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Вариац. Маржа", typeof(decimal));
			EffectivePrice = new DdeTableColumn(DdeTableTypes.DerivativePosition, "Эффект. цена поз.", typeof(decimal));
		}

		/// <summary>
		/// Идентификатор фирмы-дилера в торговой системе.
		/// </summary>
		public static DdeTableColumn FirmId { get; private set; }

		/// <summary>
		/// Тип группировки торговых счетов.
		/// </summary>
		public static DdeTableColumn Type { get; private set; }

		/// <summary>
		/// Идентификатор инструмента в торговой системе.
		/// </summary>
		public static DdeTableColumn SecurityCode { get; private set; }

		/// <summary>
		/// Сокращенное наименование инструмента.
		/// </summary>
		public static DdeTableColumn SecurityShortName { get; private set; }

		/// <summary>
		/// Внутренний составной параметр сервера QUIK, содержащий обозначение торговой площадки.
		/// </summary>
		public static DdeTableColumn Account { get; private set; }

		/// <summary>
		/// Дата погашения контракта.
		/// </summary>
		public static DdeTableColumn SettlementDate { get; private set; }

		/// <summary>
		/// Общее количество контрактов в открытых позициях на начало торгов.
		/// </summary>
		public static DdeTableColumn BeginPosition { get; private set; }

		/// <summary>
		/// Общее количество контрактов в открытых позициях на текущий момент, с учетом сделок.
		/// </summary>
		public static DdeTableColumn CurrentPosition { get; private set; }

		/// <summary>
		/// Количество контрактов в активных заявках на покупку.
		/// </summary>
		public static DdeTableColumn CurrentBidsVolume { get; private set; }

		/// <summary>
		/// Количество контрактов в активных заявках на продажу.
		/// </summary>
		public static DdeTableColumn CurrentAsksVolume { get; private set; }

		/// <summary>
		/// Оценка размера вариационной маржи (изменения стоимости позиции клиента в денежном выражении с учетом котировок), рублей.
		/// </summary>
		public static DdeTableColumn VariationMargin { get; private set; }

		/// <summary>
		/// Стоимостная оценка текущих чистых позиций.
		/// </summary>
		public static DdeTableColumn CurrentPositionPrice { get; private set; }

		/// <summary>
		/// Стоимостная оценка планируемых (с учетом исполнения заявок) чистых позиций.
		/// </summary>
		public static DdeTableColumn PlanningPositionPrice { get; private set; }

		/// <summary>
		/// Цена, при закрытии позиций по которой вариационная маржа будет равна нулю. Рассчитывается только при использовании клиентом единой денежной позиции.
		/// </summary>
		public static DdeTableColumn EffectivePrice { get; private set; }
	}
}