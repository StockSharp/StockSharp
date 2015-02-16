namespace StockSharp.Quik
{
	/// <summary>
	/// Колонки таблицы Портфель по бумагам.
	/// </summary>
	public static class DdeEquityPortfolioColumns
	{
		static DdeEquityPortfolioColumns()
		{
			FirmId = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Фирма", typeof(string));
			ClientCode = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Код клиента", typeof(string));
			ElevatedRisk = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ПовышУрРиска", typeof(string));
			ClientType = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Тип клиента", typeof(string));
			FortsAccount = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Сроч. счет", typeof(string));
			BeginAmount = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Вход.активы", typeof(decimal));
			BeginLeverage = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Плечо", typeof(decimal));
			BeginMarginLimit = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Вход.лимит", typeof(decimal));
			ShortsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Шорты", typeof(decimal));
			LongsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Лонги", typeof(decimal));
			MarginLongsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Лонги МО", typeof(decimal));
			NonMarginCoveredLongsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Лонги О", typeof(decimal));
			CurrentAmount = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Тек.активы", typeof(decimal));
			CurrentLeverage = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Тек.плечо", typeof(decimal));
			Margin = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Ур.маржи", typeof(decimal));
			CurrentMarginLimit = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Тек.лимит", typeof(decimal));
			AvailableMarginLimit = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ДостТекЛимит", typeof(decimal));
			BidsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "БлокПокупка", typeof(decimal));
			MarginBidsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "БлокПок МО", typeof(decimal));
			NonMarginBidsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "БлокПок О", typeof(decimal));
			CoveredBidsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "БлокПокНеМарж", typeof(decimal));
			AsksPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "БлокПродажа", typeof(decimal));
			BeginCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ВходСредства", typeof(decimal));
			CurrentCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ТекСредства", typeof(decimal));
			PnL = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Прибыль/убытки", typeof(decimal));
			ChangeShift = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ПроцИзмен", typeof(decimal));
			AvailableBuyMarginCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "На покупку", typeof(decimal));
			AvailableSellMarginCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "На продажу", typeof(decimal));
			AvailableBuyNonMarginCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "НаПокупНеМаржин", typeof(decimal));
			AvailableBuyCoveredCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "НаПокупОбесп", typeof(decimal));
			CoveredPositionsPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ГО поз.", typeof(decimal));
			CoveredOrdersPrice = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "ГО заяв.", typeof(decimal));
			VariationMargin = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Вариац. маржа", typeof(decimal));
			RemainCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Активы/ГО", typeof(decimal));
			AmountAtCovered = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Сумма ден. остатков", typeof(decimal));
			BlockedCurrency = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Суммарно заблок.", typeof(decimal));
			LimitType = new DdeTableColumn(DdeTableTypes.EquityPortfolio, "Вид лимита", typeof(string));
		}

		/// <summary>
		/// Идентификатор участника торгов в торговой системе.
		/// </summary>
		public static DdeTableColumn FirmId { get; private set; }

		/// <summary>
		/// Код клиента в системе QUIK.
		/// </summary>
		public static DdeTableColumn ClientCode { get; private set; }

		/// <summary>
		/// Признак «квалифицированного» клиента, которому разрешено кредитование заемными средствами с плечом 1:3.
		/// </summary>
		public static DdeTableColumn ElevatedRisk { get; private set; }

		/// <summary>
		/// Признак использования схемы кредитования с контролем текущей стоимости активов.
		/// </summary>
		public static DdeTableColumn ClientType { get; private set; }

		/// <summary>
		/// Счет клиента на FORTS, в случае наличия объединенной позиции, иначе поле остается пустым.
		/// </summary>
		public static DdeTableColumn FortsAccount { get; private set; }

		/// <summary>
		/// Оценка собственных средств клиента до начала торгов.
		/// </summary>
		public static DdeTableColumn BeginAmount { get; private set; }

		/// <summary>
		/// Отношение Входящего лимита к Входящим активам.
		/// </summary>
		public static DdeTableColumn BeginLeverage { get; private set; }

		/// <summary>
		/// Значение маржинального лимита до начала торгов.
		/// </summary>
		public static DdeTableColumn BeginMarginLimit { get; private set; }

		/// <summary>
		/// Оценка стоимости коротких позиций (значение всегда отрицательное).
		/// </summary>
		public static DdeTableColumn ShortsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости длинных позиций.
		/// </summary>
		public static DdeTableColumn LongsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости длинных позиций по маржинальным бумагам, принимаемым в обеспечение.
		/// </summary>
		public static DdeTableColumn MarginLongsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости длинных позиций по немаржинальным бумагам, принимаемым в обеспечение.
		/// </summary>
		public static DdeTableColumn NonMarginCoveredLongsPrice { get; private set; }

		/// <summary>
		/// Оценка собственных средств клиента по текущим позициям и ценам.
		/// </summary>
		public static DdeTableColumn CurrentAmount { get; private set; }

		/// <summary>
		/// Текущее отношение собственных и использованных заемных средств.
		/// </summary>
		public static DdeTableColumn CurrentLeverage { get; private set; }

		/// <summary>
		/// Отношение собственных средств клиента (Текущие активы) к стоимости всех активов клиента - стоимости длинных позиций плюс денежный остаток (если он положительный), в процентах.
		/// </summary>
		public static DdeTableColumn Margin { get; private set; }

		/// <summary>
		/// Текущее значение маржинального лимита.
		/// </summary>
		public static DdeTableColumn CurrentMarginLimit { get; private set; }

		/// <summary>
		/// Значение текущего маржинального лимита, доступное для дальнейшего открытия позиций.
		/// </summary>
		public static DdeTableColumn AvailableMarginLimit { get; private set; }

		/// <summary>
		/// Оценка стоимости активов в заявках на покупку.
		/// </summary>
		public static DdeTableColumn BidsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости активов в заявках на покупку маржинальных бумаг, принимаемых в обеспечение (типа «МО»).
		/// </summary>
		public static DdeTableColumn MarginBidsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости активов в заявках на покупку немаржинальных бумаг, принимаемых в обеспечение (типа «О»).
		/// </summary>
		public static DdeTableColumn CoveredBidsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости активов в заявках на покупку немаржинальных бумаг (тип которых не указан)
		/// </summary>
		public static DdeTableColumn NonMarginBidsPrice { get; private set; }

		/// <summary>
		/// Оценка в денежном выражении планируемых шортов.
		/// </summary>
		public static DdeTableColumn AsksPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости всех позиций клиента в ценах закрытия предыдущей торговой сессии, включая позиции по немаржинальным бумагам.
		/// </summary>
		public static DdeTableColumn BeginCurrency { get; private set; }

		/// <summary>
		/// Текущая оценка стоимости всех позиций клиента (с учетом вариационной маржи по счету).
		/// </summary>
		public static DdeTableColumn CurrentCurrency { get; private set; }

		/// <summary>
		/// Абсолютная величина изменения стоимости всех позиций клиента.
		/// </summary>
		public static DdeTableColumn PnL { get; private set; }

		/// <summary>
		/// Относительная величина изменения стоимости всех позиций клиента.
		/// </summary>
		public static DdeTableColumn ChangeShift { get; private set; }

		/// <summary>
		/// Оценка денежных средств, доступных для покупки маржинальных бумаг (типа «МО»).
		/// </summary>
		public static DdeTableColumn AvailableBuyMarginCurrency { get; private set; }

		/// <summary>
		/// Оценка денежных средств, доступных для продажи маржинальных бумаг (типа «МО»).
		/// </summary>
		public static DdeTableColumn AvailableSellMarginCurrency { get; private set; }

		/// <summary>
		/// Оценка денежных средств, доступных для покупки немаржинальных бумаг (тип которых не указан).
		/// </summary>
		public static DdeTableColumn AvailableBuyNonMarginCurrency { get; private set; }

		/// <summary>
		/// Оценка денежных средств, доступных для покупки бумаг, принимаемых в обеспечение (типа «О»).
		/// </summary>
		public static DdeTableColumn AvailableBuyCoveredCurrency { get; private set; }

		/// <summary>
		/// Размер денежных средств, уплаченных под все открытые позиции на срочном рынке.
		/// </summary>
		public static DdeTableColumn CoveredPositionsPrice { get; private set; }

		/// <summary>
		/// Оценка стоимости активов в заявках на покупку на срочном рынке.
		/// </summary>
		public static DdeTableColumn CoveredOrdersPrice { get; private set; }

		/// <summary>
		/// Текущая вариационная маржа по позициям клиента, по всем инструментам.
		/// </summary>
		public static DdeTableColumn VariationMargin { get; private set; }

		/// <summary>
		/// Сумма остатков по денежным средствам по всем лимитам.
		/// </summary>
		public static DdeTableColumn RemainCurrency { get; private set; }

		/// <summary>
		/// Отношение ликвидационной стоимости портфеля к ГО по срочному рынку.
		/// </summary>
		public static DdeTableColumn AmountAtCovered { get; private set; }

		/// <summary>
		/// Сумма денежных средств, заблокированных под исполнение обязательств.
		/// </summary>
		public static DdeTableColumn BlockedCurrency { get; private set; }

		/// <summary>
		/// Вид лимита для Т+ рынка.
		/// </summary>
		public static DdeTableColumn LimitType { get; private set; }
	}
}