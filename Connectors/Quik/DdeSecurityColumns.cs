namespace StockSharp.Quik
{
	using System;

	/// <summary>
	/// Колонки таблицы инструментов.
	/// </summary>
	public static class DdeSecurityColumns
	{
		static DdeSecurityColumns()
		{
			Name = new DdeTableColumn(DdeTableTypes.Security, "Полное название бумаги", typeof(string));
			ShortName = new DdeTableColumn(DdeTableTypes.Security, "Краткое название бумаги", typeof(string));
			Code = new DdeTableColumn(DdeTableTypes.Security, "Код бумаги", typeof(string));
			Class = new DdeTableColumn(DdeTableTypes.Security, "Код класса", typeof(string));
			Status = new DdeTableColumn(DdeTableTypes.Security, "Статус", typeof(string));
			BestBidPrice = new DdeTableColumn(DdeTableTypes.Security, "Лучшая цена спроса", typeof(decimal));
			BestBidVolume = new DdeTableColumn(DdeTableTypes.Security, "Спрос по лучшей цене", typeof(decimal));
			BestAskPrice = new DdeTableColumn(DdeTableTypes.Security, "Лучшая цена предложения", typeof(decimal));
			BestAskVolume = new DdeTableColumn(DdeTableTypes.Security, "Предложение по лучшей цене", typeof(decimal));
			LastTradePrice = new DdeTableColumn(DdeTableTypes.Security, "Цена последней сделки", typeof(decimal));
			LastTradeTime = new DdeTableColumn(DdeTableTypes.Security, "Время последней сделки", typeof(DateTime));
			LastTradeVolume = new DdeTableColumn(DdeTableTypes.Security, "Количество в последней сделке", typeof(decimal));
			Decimals = new DdeTableColumn(DdeTableTypes.Security, "Точность цены", typeof(int));
			PriceStep = new DdeTableColumn(DdeTableTypes.Security, "Минимальный шаг цены", typeof(decimal));
			LotVolume = new DdeTableColumn(DdeTableTypes.Security, "Размер лота", typeof(decimal));
			OpenPrice = new DdeTableColumn(DdeTableTypes.Security, "Цена открытия", typeof(decimal));
			HighPrice = new DdeTableColumn(DdeTableTypes.Security, "Максимальная цена сделки", typeof(decimal));
			LowPrice = new DdeTableColumn(DdeTableTypes.Security, "Минимальная цена сделки", typeof(decimal));
			ClosePrice = new DdeTableColumn(DdeTableTypes.Security, "Цена закрытия", typeof(decimal));
			SettlementDate = new DdeTableColumn(DdeTableTypes.Security, "Дата погашения", typeof(DateTime));

			ISIN = new DdeTableColumn(DdeTableTypes.Security, "ISIN-код бумаги", typeof(string));
			RegistryId = new DdeTableColumn(DdeTableTypes.Security, "Регистрационный номер", typeof(string));
			TradingDate = new DdeTableColumn(DdeTableTypes.Security, "Дата торгов", typeof(DateTime));
			SettlementDays = new DdeTableColumn(DdeTableTypes.Security, "Число дней до погашения", typeof(int));
			Nominal = new DdeTableColumn(DdeTableTypes.Security, "Номинал", typeof(decimal));
			NominalCurrency = new DdeTableColumn(DdeTableTypes.Security, "Валюта номинала", typeof(string));
			Type = new DdeTableColumn(DdeTableTypes.Security, "Тип инструмента", typeof(string));
			BidsVolume = new DdeTableColumn(DdeTableTypes.Security, "Суммарный спрос", typeof(decimal));
			BidsCount = new DdeTableColumn(DdeTableTypes.Security, "Количество заявок на покупку", typeof(int));
			AsksVolume = new DdeTableColumn(DdeTableTypes.Security, "Суммарное предложение", typeof(decimal));
			AsksCount = new DdeTableColumn(DdeTableTypes.Security, "Количество заявок на продажу", typeof(int));
			PrevTradeDiff = new DdeTableColumn(DdeTableTypes.Security, "Разница цены последней к закрытию предыдущей сессии", typeof(decimal));
			TotalVolume = new DdeTableColumn(DdeTableTypes.Security, "Контрактов во всех сделках", typeof(decimal));
			TotalMoney = new DdeTableColumn(DdeTableTypes.Security, "Оборот в деньгах", typeof(decimal));
			SessionState = new DdeTableColumn(DdeTableTypes.Security, "Состояние сессии", typeof(string));
			LastTradeValue = new DdeTableColumn(DdeTableTypes.Security, "Оборот в деньгах последней сделки", typeof(decimal));
			AveragePrice = new DdeTableColumn(DdeTableTypes.Security, "Средневзвешенная цена", typeof(decimal));
			MaxBidPrice = new DdeTableColumn(DdeTableTypes.Security, "Лучшая цена спроса сегодня", typeof(decimal));
			MinAskPrice = new DdeTableColumn(DdeTableTypes.Security, "Лучшая цена предложения сегодня", typeof(decimal));
			PrevMarketPrice = new DdeTableColumn(DdeTableTypes.Security, "Вчерашняя рыночная цена", typeof(decimal));
			MarketPrice = new DdeTableColumn(DdeTableTypes.Security, "Рыночная цена", typeof(decimal));
			MarketPrice2 = new DdeTableColumn(DdeTableTypes.Security, "Рыночная цена 2", typeof(decimal));
			MainSessionBeginTime = new DdeTableColumn(DdeTableTypes.Security, "Начало основной сессии", typeof(DateTime));
			MainSessionEndTime = new DdeTableColumn(DdeTableTypes.Security, "Окончание основной сессии", typeof(DateTime));
			EveningSessionBeginTime = new DdeTableColumn(DdeTableTypes.Security, "Начало вечерней сессии", typeof(DateTime));
			EveningSessionEndTime = new DdeTableColumn(DdeTableTypes.Security, "Окончание вечерней сессии", typeof(DateTime));
			MorningSessionBeginTime = new DdeTableColumn(DdeTableTypes.Security, "Начало утренней сессии", typeof(DateTime));
			MorningEveningSessionEndTime = new DdeTableColumn(DdeTableTypes.Security, "Окончание утренней сессии", typeof(DateTime));
			PriceType = new DdeTableColumn(DdeTableTypes.Security, "Тип цены", typeof(string));

			// Срочный рынок ММВБ
			CloseYield = new DdeTableColumn(DdeTableTypes.Security, "Цена периода закрытия", typeof(decimal));
			Yield = new DdeTableColumn(DdeTableTypes.Security, "Доходность", typeof(decimal));
			AccruedInt = new DdeTableColumn(DdeTableTypes.Security, "Накопленный купонный доход", typeof(decimal));
			CouponValue = new DdeTableColumn(DdeTableTypes.Security, "Размер купона", typeof(decimal));
			NextCoupon = new DdeTableColumn(DdeTableTypes.Security, "Дата выплаты купона", typeof(DateTime));
			CouponPeriod = new DdeTableColumn(DdeTableTypes.Security, "Длительность купона", typeof(int));
			BuyBackPrice = new DdeTableColumn(DdeTableTypes.Security, "Цена оферты", typeof(decimal));
			BuyBackDate = new DdeTableColumn(DdeTableTypes.Security, "Дата оферты", typeof(DateTime));
			IssueSize = new DdeTableColumn(DdeTableTypes.Security, "Объем обращения", typeof(int));
			LegalOpenPrice = new DdeTableColumn(DdeTableTypes.Security, "Официальная цена открытия", typeof(decimal));
			LegalCurrentPrice = new DdeTableColumn(DdeTableTypes.Security, "Официальная текущая цена", typeof(decimal));
			LegalClosePrice = new DdeTableColumn(DdeTableTypes.Security, "Официальная цена закрытия", typeof(decimal));
			LastTradeVolume2 = new DdeTableColumn(DdeTableTypes.Security, "Количество контрактов в последней сделке", typeof(decimal));

			// Forts
			MinPrice = new DdeTableColumn(DdeTableTypes.Security, "Минимально возможная цена", typeof(decimal));
			MaxPrice = new DdeTableColumn(DdeTableTypes.Security, "Максимально возможная цена", typeof(decimal));
			OpenPositions = new DdeTableColumn(DdeTableTypes.Security, "Количество открытых позиций", typeof(decimal));
			Trend = new DdeTableColumn(DdeTableTypes.Security, "Разница цен последней и предыдущей сделок", typeof(decimal));
			MarginBuy = new DdeTableColumn(DdeTableTypes.Security, "Гарантийное обеспечение покупателя", typeof(decimal));
			MarginSell = new DdeTableColumn(DdeTableTypes.Security, "Гарантийное обеспечение продавца", typeof(decimal));
			LastChangeTime = new DdeTableColumn(DdeTableTypes.Security, "Время последнего изменения", typeof(DateTime));
			MarginCovered = new DdeTableColumn(DdeTableTypes.Security, "БГО по покрытым позициям", typeof(decimal));
			MarginUncovered = new DdeTableColumn(DdeTableTypes.Security, "БГО по непокрытым позициям", typeof(decimal));
			Strike = new DdeTableColumn(DdeTableTypes.Security, "Цена страйк", typeof(decimal));
			StepPrice = new DdeTableColumn(DdeTableTypes.Security, "Стоимость шага цены", typeof(decimal));
			OptionType = new DdeTableColumn(DdeTableTypes.Security, "Тип опциона", typeof(string));
			UnderlyingSecurity = new DdeTableColumn(DdeTableTypes.Security, "Базовый актив", typeof(string));
			TheorPrice = new DdeTableColumn(DdeTableTypes.Security, "Теоретическая цена", typeof(decimal));
			ImpliedVolatility = new DdeTableColumn(DdeTableTypes.Security, "Волатильность опциона", typeof(decimal));
			AggregateRate = new DdeTableColumn(DdeTableTypes.Security, "Агрегированная ставка", typeof(decimal));
			FuturePriceType = new DdeTableColumn(DdeTableTypes.Security, "Тип цены фьючерса", typeof(string));
			ClearingStatus = new DdeTableColumn(DdeTableTypes.Security, "Статус клиринга", typeof(string));
			MinStepPriceCurrency = new DdeTableColumn(DdeTableTypes.Security, "Валюта шага цены", typeof(string));
			IsMargined = new DdeTableColumn(DdeTableTypes.Security, "Маржируемый", typeof(string));
			ExpiryDate = new DdeTableColumn(DdeTableTypes.Security, "Дата исполнения инструмента", typeof(DateTime));
			MinStepPriceMainClearing = new DdeTableColumn(DdeTableTypes.Security, "Стоимость шага цены для клиринга", typeof(decimal));
			MinStepPriceInterClearing = new DdeTableColumn(DdeTableTypes.Security, "Стоимость шага цены для промклиринга", typeof(decimal));

			// индексы
			IndexCurrentPrice = new DdeTableColumn(DdeTableTypes.Security, "Значение", typeof(decimal));
			IndexClosePrice = new DdeTableColumn(DdeTableTypes.Security, "Закрытие", typeof(decimal));
			IndexOpenPrice = new DdeTableColumn(DdeTableTypes.Security, "Значение индекса на момент открытия торгов", typeof(decimal));
			IndexOpenPriceDelta = new DdeTableColumn(DdeTableTypes.Security, "Изменение текущего индекса по сравнению со значением открытия", typeof(decimal));
			IndexClosePriceDelta = new DdeTableColumn(DdeTableTypes.Security, "Изменение текущего индекса по сравнению со значением закрытия", typeof(decimal));
		}

		/// <summary>
		/// Наименование инструмента.
		/// </summary>
		public static DdeTableColumn Name { get; private set; }

		/// <summary>
		/// Сокращенное наименование инструмента.
		/// </summary>
		public static DdeTableColumn ShortName { get; private set; }

		/// <summary>
		/// Идентификатор инструмента в торговой системе.
		/// </summary>
		public static DdeTableColumn Code { get; private set; }

		/// <summary>
		/// Код класса в торговой системе.
		/// </summary>
		public static DdeTableColumn Class { get; private set; }

		///// <summary>
		///// Наименование класса в торговой системе.
		///// </summary>
		//public static DdeTableColumn ClassName { get; private set; }

		/// <summary>
		/// Статус инструмента (торгуется, не торгуется).
		/// </summary>
		public static DdeTableColumn Status { get; private set; }

		/// <summary>
		/// Цена лучшей котировки на покупку.
		/// </summary>
		public static DdeTableColumn BestBidPrice { get; private set; }

		/// <summary>
		/// Объем лучшей котировки на покупку.
		/// </summary>
		public static DdeTableColumn BestBidVolume { get; private set; }

		/// <summary>
		/// Цена лучшей котировки на продажу.
		/// </summary>
		public static DdeTableColumn BestAskPrice { get; private set; }

		/// <summary>
		/// Объем лучшей котировки на продажу.
		/// </summary>
		public static DdeTableColumn BestAskVolume { get; private set; }

		/// <summary>
		/// Время последнего изменения инструмента.
		/// </summary>
		public static DdeTableColumn LastChangeTime { get; private set; }

		/// <summary>
		/// Время регистрации последней сделки в торговой системе.
		/// </summary>
		public static DdeTableColumn LastTradeTime { get; private set; }

		/// <summary>
		/// Цена последней сделки, за единицу инструмента.
		/// </summary>
		public static DdeTableColumn LastTradePrice { get; private set; }

		/// <summary>
		/// Количество ценных бумаг в последней сделке.
		/// </summary>
		public static DdeTableColumn LastTradeVolume { get; private set; }

		/// <summary>
		/// Цена открытия.
		/// </summary>
		public static DdeTableColumn OpenPrice { get; private set; }

		/// <summary>
		/// Максимальная цена за сессию.
		/// </summary>
		public static DdeTableColumn HighPrice { get; private set; }

		/// <summary>
		/// Минимальная цена за сессию.
		/// </summary>
		public static DdeTableColumn LowPrice { get; private set; }

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		public static DdeTableColumn ClosePrice { get; private set; }

		/// <summary>
		/// Размер лота.
		/// </summary>
		public static DdeTableColumn LotVolume { get; private set; }

		/// <summary>
		/// Минимальный шаг цены.
		/// </summary>
		public static DdeTableColumn PriceStep { get; private set; }

		/// <summary>
		/// Стоимость шага цены.
		/// </summary>
		public static DdeTableColumn StepPrice { get; private set; }

		/// <summary>
		/// Количество знаков после запятой в цене.
		/// </summary>
		public static DdeTableColumn Decimals { get; private set; }

		/// <summary>
		/// Минимальная допустима цена спреда.
		/// </summary>
		public static DdeTableColumn MinPrice { get; private set; }

		/// <summary>
		/// Максимальная допустима цена спреда.
		/// </summary>
		public static DdeTableColumn MaxPrice { get; private set; }

		/// <summary>
		/// ГО покупателя.
		/// </summary>
		public static DdeTableColumn MarginBuy { get; private set; }

		/// <summary>
		/// ГО продавца.
		/// </summary>
		public static DdeTableColumn MarginSell { get; private set; }

		/// <summary>
		/// Дата экспирации инструмента.
		/// </summary>
		public static DdeTableColumn ExpiryDate { get; private set; }

		/// <summary>
		/// Дата погашения инструмента.
		/// </summary>
		public static DdeTableColumn SettlementDate { get; private set; }

		/// <summary>
		/// Международный идентификационный код бумаги (ISIN - International Securities Identification Number).
		/// </summary>
		public static DdeTableColumn ISIN { get; private set; }

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public static DdeTableColumn RegistryId { get; private set; }

		/// <summary>
		/// Дата текущей торговой сессии.
		/// </summary>
		public static DdeTableColumn TradingDate { get; private set; }

		/// <summary>
		/// Дата погашения (для инструментов с фиксированным сроком обращения).
		/// </summary>
		public static DdeTableColumn SettlementDays { get; private set; }

		/// <summary>
		/// Номинальная стоимость инструмента.
		/// </summary>
		public static DdeTableColumn Nominal { get; private set; }

		/// <summary>
		/// Символьный код валюты номинала инструмента, например «SUR» - рубли.
		/// </summary>
		public static DdeTableColumn NominalCurrency { get; private set; }

		/// <summary>
		/// Наименование типа ценной бумаги.
		/// </summary>
		public static DdeTableColumn Type { get; private set; }

		/// <summary>
		/// Количество ценных бумаг во всех заявках на покупку, в лотах.
		/// </summary>
		public static DdeTableColumn BidsVolume { get; private set; }

		/// <summary>
		/// Общее количество заявок на покупку по этому инструменту, штук.
		/// </summary>
		public static DdeTableColumn BidsCount { get; private set; }

		/// <summary>
		/// Количество ценных бумаг во всех заявках на продажу, в лотах.
		/// </summary>
		public static DdeTableColumn AsksVolume { get; private set; }

		/// <summary>
		/// Общее количество заявок на продажу по этому инструменту, штук.
		/// </summary>
		public static DdeTableColumn AsksCount { get; private set; }

		/// <summary>
		/// Разница между ценой последней сделки и средневзвешенной ценой предыдущей сессии, рублей.
		/// </summary>
		public static DdeTableColumn PrevTradeDiff { get; private set; }

		/// <summary>
		/// Объем совершенных в текущей сессии сделок, штук.
		/// </summary>
		public static DdeTableColumn TotalVolume { get; private set; }

		/// <summary>
		/// Объем совершенных в текущей сессии сделок в денежном выражении, рублей.
		/// </summary>
		public static DdeTableColumn TotalMoney { get; private set; }

		/// <summary>
		/// Состояние торговой сессии.
		/// </summary>
		public static DdeTableColumn SessionState { get; private set; }

		/// <summary>
		/// Объем последней совершенной сделки в денежном выражении, рублей.
		/// </summary>
		public static DdeTableColumn LastTradeValue { get; private set; }

		/// <summary>
		/// Отношение оборота текущей сессии в деньгах к бумагам во всех сделках, рублей.
		/// </summary>
		public static DdeTableColumn AveragePrice { get; private set; }

		/// <summary>
		/// Лучшая (максимальная) цена среди заявок на покупку за текущую сессию, рублей.
		/// </summary>
		public static DdeTableColumn MaxBidPrice { get; private set; }

		/// <summary>
		/// Лучшая (минимальная) цена среди заявок на продажу за текущую сессию, рублей.
		/// </summary>
		public static DdeTableColumn MinAskPrice { get; private set; }

		/// <summary>
		/// Рыночная цена предыдущего дня, рублей.
		/// </summary>
		public static DdeTableColumn PrevMarketPrice { get; private set; }

		/// <summary>
		/// Рыночная цена инструмента, рассчитанная по официальной методике, рублей.
		/// </summary>
		public static DdeTableColumn MarketPrice { get; private set; }

		/// <summary>
		/// Рыночная цена ценной бумаги, рассчитанная по официальной методике для оценки стоимости инвестиционного портфеля,
		/// сформированного за счет средств пенсионных накоплений, рублей.
		/// </summary>
		public static DdeTableColumn MarketPrice2 { get; private set; }

		/// <summary>
		/// Время начала основной сессии.
		/// </summary>
		public static DdeTableColumn MainSessionBeginTime { get; private set; }

		/// <summary>
		/// Время окончание основной сессии.
		/// </summary>
		public static DdeTableColumn MainSessionEndTime { get; private set; }

		/// <summary>
		/// Время окончание основной сессии.
		/// </summary>
		public static DdeTableColumn EveningSessionBeginTime { get; private set; }

		/// <summary>
		/// Время окончание дополнительной вечерней сессии.
		/// </summary>
		public static DdeTableColumn EveningSessionEndTime { get; private set; }

		/// <summary>
		/// Время начала дополнительной утренней сессии.
		/// </summary>
		public static DdeTableColumn MorningSessionBeginTime { get; private set; }

		/// <summary>
		/// Время окончание дополнительной утренней сессии.
		/// </summary>
		public static DdeTableColumn MorningEveningSessionEndTime { get; private set; }

		/// <summary>
		/// Доходность инструмента по цене периода закрытия, %.
		/// </summary>
		public static DdeTableColumn CloseYield { get; private set; }

		/// <summary>
		/// Доходность инструмента по цене последней сделки, %.
		/// </summary>
		public static DdeTableColumn Yield { get; private set; }

		/// <summary>
		/// Накопленный купонный доход, рублей.
		/// </summary>
		public static DdeTableColumn AccruedInt { get; private set; }

		/// <summary>
		/// Величина купона, %.
		/// </summary>
		public static DdeTableColumn CouponValue { get; private set; }

		/// <summary>
		/// Дата выплаты купона.
		/// </summary>
		public static DdeTableColumn NextCoupon { get; private set; }

		/// <summary>
		/// Продолжительность текущего купонного периода, в календарных днях.
		/// </summary>
		public static DdeTableColumn CouponPeriod { get; private set; }

		/// <summary>
		/// Цена оферты (предварительного выкупа).
		/// </summary>
		public static DdeTableColumn BuyBackPrice { get; private set; }

		/// <summary>
		/// Дата оферты.
		/// </summary>
		public static DdeTableColumn BuyBackDate { get; private set; }

		/// <summary>
		/// Объем выпуска ценных бумаг, находящийся в обращении, в штуках.
		/// </summary>
		public static DdeTableColumn IssueSize { get; private set; }

		/// <summary>
		/// Цена открытия, официально объявленная торговой системой.
		/// </summary>
		public static DdeTableColumn LegalOpenPrice { get; private set; }

		/// <summary>
		/// Текущая цена, официально объявленная торговой системой.
		/// </summary>
		public static DdeTableColumn LegalCurrentPrice { get; private set; }

		/// <summary>
		/// Цена закрытия, официально объявленная торговой системой.
		/// </summary>
		public static DdeTableColumn LegalClosePrice { get; private set; }

		/// <summary>
		/// Количество контрактов в последней сделке, штук.
		/// </summary>
		public static DdeTableColumn LastTradeVolume2 { get; private set; }

		/// <summary>
		/// Способ указания цены инструмента. Возможные значения: «% от ном.» - процент от номинала; «Цена» - цена, за единицу инструмента.
		/// </summary>
		public static DdeTableColumn PriceType { get; private set; }

		/// <summary>
		/// Тип опциона, PUT или CALL.
		/// </summary>
		public static DdeTableColumn OptionType { get; private set; }

		/// <summary>
		/// Цена исполнения опциона (поставки базового актива), рублей.
		/// </summary>
		public static DdeTableColumn Strike { get; private set; }

		/// <summary>
		/// Идентификатор инструмента в торговой системе, соответствующий базовому активу срочного контракта.
		/// </summary>
		public static DdeTableColumn UnderlyingSecurity { get; private set; }

		/// <summary>
		/// Теоретическая цена опциона, рублей.
		/// </summary>
		public static DdeTableColumn TheorPrice { get; private set; }

		/// <summary>
		/// Волатильность опциона (подразумеваемая).
		/// </summary>
		public static DdeTableColumn ImpliedVolatility { get; private set; }

		/// <summary>
		/// Опцион маржируемый/с уплатой премии.
		/// </summary>
		public static DdeTableColumn IsMargined { get; private set; }

		/// <summary>
		/// Статус клиринга.
		/// </summary>
		public static DdeTableColumn ClearingStatus { get; private set; }

		/// <summary>
		/// Количество открытых позиций по инструменту, штук.
		/// </summary>
		public static DdeTableColumn OpenPositions { get; private set; }

		/// <summary>
		/// Разница между ценой последней совершенной сделки и ценой предыдущей сделки, рублей.
		/// </summary>
		public static DdeTableColumn Trend { get; private set; }

		/// <summary>
		/// Базовый размер гарантийного обеспечения по покрытым позициям, рублей.
		/// </summary>
		public static DdeTableColumn MarginCovered { get; private set; }

		/// <summary>
		/// Базовый размер гарантийного обеспечения по непокрытым позициям, рублей.
		/// </summary>
		public static DdeTableColumn MarginUncovered { get; private set; }

		/// <summary>
		/// Процентная ставка для расчета вариационной маржи по процентным фьючерсам.
		/// </summary>
		public static DdeTableColumn AggregateRate { get; private set; }

		/// <summary>
		/// Наименование типа цены фьючерса.
		/// </summary>
		public static DdeTableColumn FuturePriceType { get; private set; }

		/// <summary>
		/// Валюта шага цены.
		/// </summary>
		public static DdeTableColumn MinStepPriceCurrency { get; private set; }

		/// <summary>
		/// Стоимость шага цены для клиринга по инструментам, номинированным в другой валюте.
		/// </summary>
		public static DdeTableColumn MinStepPriceMainClearing { get; private set; }

		/// <summary>
		/// Стоимость шага цены для промежуточного клиринга по инструментам, номинированным в другой валюте.
		/// </summary>
		public static DdeTableColumn MinStepPriceInterClearing { get; private set; }

		/// <summary>
		/// Текущее значение индекса.
		/// </summary>
		public static DdeTableColumn IndexCurrentPrice { get; private set; }

		/// <summary>
		/// Значение индекса при закрытии торгов предыдущего дня.
		/// </summary>
		public static DdeTableColumn IndexClosePrice { get; private set; }

		/// <summary>
		/// Значение индекса на момент открытия торгов.
		/// </summary>
		public static DdeTableColumn IndexOpenPrice { get; private set; }

		/// <summary>
		/// Изменение текущего индекса по сравнению с его значением на момент открытия торгов.
		/// </summary>
		public static DdeTableColumn IndexOpenPriceDelta { get; private set; }

		/// <summary>
		/// Изменение текущего индекса по сравнению со значением на момент закрытия торгов.
		/// </summary>
		public static DdeTableColumn IndexClosePriceDelta { get; private set; }
	}
}