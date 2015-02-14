namespace StockSharp.Alor.Metadata
{
	using System;

	/// <summary>
	/// Колонки системной таблицы SECURITIES.
	/// </summary>
	public static class AlorSecurityColumns
	{
		/// <summary>
		/// Идентификатор строки.
		/// </summary>
		public static readonly AlorColumn Id = new AlorColumn(AlorTableTypes.Security, "ID", typeof(int), false);

		/// <summary>
		/// Идентификатор режима торгов для финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Board = new AlorColumn(AlorTableTypes.Security, "SecBoard", typeof(string));

		/// <summary>
		/// Идентификатор финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Code = new AlorColumn(AlorTableTypes.Security, "SecCode", typeof(string));

		/// <summary>
		/// Наименование финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Name = new AlorColumn(AlorTableTypes.Security, "SecName", typeof(string));

		/// <summary>
		/// Краткое наименование финансового инструмента.
		/// </summary>
		public static readonly AlorColumn ShortName = new AlorColumn(AlorTableTypes.Security, "ShortName", typeof(string));

		/// <summary>
		/// Краткое наименование финансового инструмента на английском языке.
		/// </summary>
		public static readonly AlorColumn EnglishName = new AlorColumn(AlorTableTypes.Security, "LatName", typeof(string), false);

		/// <summary>
		/// Примечания.
		/// </summary>
		public static readonly AlorColumn Remarks = new AlorColumn(AlorTableTypes.Security, "Remarks", typeof(string), false);

		/// <summary>
		/// Код рынка, на котором торгуется финансовый инструмент.
		/// </summary>
		public static readonly AlorColumn MarketCode = new AlorColumn(AlorTableTypes.Security, "MarketCode", typeof(string), false);

		/// <summary>
		/// Код контракта (для фьючерсов и опционов).
		/// </summary>
		public static readonly AlorColumn ContractCode = new AlorColumn(AlorTableTypes.Security, "ConCode", typeof(string),false);

		/// <summary>
		/// Код депо.
		/// </summary>
		public static readonly AlorColumn DepoCode = new AlorColumn(AlorTableTypes.Security, "DepoCode", typeof(string), false);

		/// <summary>
		/// Описание финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Description = new AlorColumn(AlorTableTypes.Security, "Description", typeof(string), false);

		/// <summary>
		/// Состояние.
		/// </summary>
		public static readonly AlorColumn Status = new AlorColumn(AlorTableTypes.Security, "Status", typeof(string),false);

		/// <summary>
		/// Состояние торговой сессии по инструменту.
		/// </summary>
		public static readonly AlorColumn TradingStatus = new AlorColumn(AlorTableTypes.Security, "TradingStatus", typeof(string));

		/// <summary>
		/// Количество ценных бумаг в одном стандартном лоте.
		/// </summary>
		public static readonly AlorColumn MinLotSize = new AlorColumn(AlorTableTypes.Security, "LotSize", typeof(int));

		/// <summary>
		/// Кратность объема заявки.
		/// </summary>
		public static readonly AlorColumn LotStep = new AlorColumn(AlorTableTypes.Security, "LotStep", typeof(int),false);

		/// <summary>
		/// Минимально возможная разница между ценами.
		/// </summary>
		public static readonly AlorColumn MinStep = new AlorColumn(AlorTableTypes.Security, "MinStep", typeof(decimal));

		/// <summary>
		/// Стоимость шага цены.
		/// </summary>
		public static readonly AlorColumn MinStepPrice = new AlorColumn(AlorTableTypes.Security, "StepPrice", typeof(decimal));

		/// <summary>
		/// Количество знаков после запятой для полей типа PRICE.
		/// </summary>
		public static readonly AlorColumn Decimals = new AlorColumn(AlorTableTypes.Security, "Decimals", typeof(int));

		/// <summary>
		/// Разрядность полей типа PRICE.
		/// </summary>
		public static readonly AlorColumn Precision = new AlorColumn(AlorTableTypes.Security, "Precision", typeof(int), false);

		/// <summary>
		/// Номинальная стоимость одной ценной бумаги.
		/// </summary>
		public static readonly AlorColumn FaceValue = new AlorColumn(AlorTableTypes.Security, "FaceValue", typeof(decimal),false);

		/// <summary>
		/// Код валюты.
		/// </summary>
		public static readonly AlorColumn FaceUnit = new AlorColumn(AlorTableTypes.Security, "FaceUnit", typeof(string), false);

		/// <summary>
		/// Дата предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn PrevDate = new AlorColumn(AlorTableTypes.Security, "PrevDate", typeof(DateTime), false);

		/// <summary>
		/// Цена последней сделки предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn PrevPrice = new AlorColumn(AlorTableTypes.Security, "PrevPrice", typeof(decimal), false);

        ///// <summary>
        ///// Цена периода закрытия.
        ///// </summary>
        //public static readonly AlorColumn ClosePrice = new AlorColumn(AlorTableTypes.Security, "ClosePrice", typeof(decimal), false);

		/// <summary>
		/// Доходность по цене периода закрытия.
		/// </summary>
		public static readonly AlorColumn CloseYield = new AlorColumn(AlorTableTypes.Security, "CloseYield", typeof(decimal), false);

		/// <summary>
		/// Величина купона.
		/// </summary>
		public static readonly AlorColumn CouponValue = new AlorColumn(AlorTableTypes.Security, "CouponValue", typeof(decimal), false);

		/// <summary>
		/// Длительность купона.
		/// </summary>
		public static readonly AlorColumn CouponPeriod = new AlorColumn(AlorTableTypes.Security, "CouponPeriod", typeof(int), false);

		/// <summary>
		/// Дата выплаты купона.
		/// </summary>
		public static readonly AlorColumn NextCoupon = new AlorColumn(AlorTableTypes.Security, "NextCoupon", typeof(DateTime), false);

		/// <summary>
		/// Объем обращения.
		/// </summary>
		public static readonly AlorColumn IssueSize = new AlorColumn(AlorTableTypes.Security, "IssueSize", typeof(decimal), false);

		/// <summary>
		/// Цена второй части РЕПО.
		/// </summary>
		public static readonly AlorColumn Repo2Price = new AlorColumn(AlorTableTypes.Security, "Repo2Price", typeof(decimal), false);

		/// <summary>
		/// Код сопряженной валюты ценной бумаги.
		/// </summary>
		public static readonly AlorColumn CurrencyId = new AlorColumn(AlorTableTypes.Security, "CurrencyID", typeof(string), false);

		/// <summary>
		/// Цена оферты.
		/// </summary>
		public static readonly AlorColumn BuyBackPrice = new AlorColumn(AlorTableTypes.Security, "BuyBackPrice", typeof(decimal), false);

		/// <summary>
		/// Дата оферты.
		/// </summary>
		public static readonly AlorColumn BuyBackDate = new AlorColumn(AlorTableTypes.Security, "BuyBackDate", typeof(DateTime), false);

		/// <summary>
		/// Агент по размещению.
		/// </summary>
		public static readonly AlorColumn AgentId = new AlorColumn(AlorTableTypes.Security, "AgentID", typeof(string), false);

		/// <summary>
		/// Тип цены.
		/// </summary>
		public static readonly AlorColumn QuoteBasis = new AlorColumn(AlorTableTypes.Security, "QuoteBasis", typeof(string), false);

		/// <summary>
		/// Международный идентификационный код ценной бумаги.
		/// </summary>
		public static readonly AlorColumn ISIN = new AlorColumn(AlorTableTypes.Security, "ISIN", typeof(string), false);

		/// <summary>
		/// Номер государственной регистрации.
		/// </summary>
		public static readonly AlorColumn RegNumber = new AlorColumn(AlorTableTypes.Security, "RegNumber", typeof(string), false);

		/// <summary>
		/// Значение оценки предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn PrevWaPrice = new AlorColumn(AlorTableTypes.Security, "PrevWaPrice", typeof(decimal), false);

		/// <summary>
		/// Доходность по оценке предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn YieldAtPrevWaPrice = new AlorColumn(AlorTableTypes.Security, "YieldAtPrevWaPrice", typeof(decimal), false);

		/// <summary>
		/// Средневзвешенная цена.
		/// </summary>
		public static readonly AlorColumn WaPrice = new AlorColumn(AlorTableTypes.Security, "WaPrice", typeof(decimal), false);

		/// <summary>
		/// Доходность по средневзвешенной цене.
		/// </summary>
		public static readonly AlorColumn YieldAtWaPrice = new AlorColumn(AlorTableTypes.Security, "YieldAtWaPrice", typeof(decimal), false);

		/// <summary>
		/// Количество заключенных контрактов.
		/// </summary>
		public static readonly AlorColumn Contracts = new AlorColumn(AlorTableTypes.Security, "Contracts", typeof(int), false);

		/// <summary>
		/// Количество открытых позиций.
		/// </summary>
		public static readonly AlorColumn OpenPositions = new AlorColumn(AlorTableTypes.Security, "OpenPositions", typeof(int), false);

		/// <summary>
		/// Время модификации.
		/// </summary>
		public static readonly AlorColumn ModificationTime = new AlorColumn(AlorTableTypes.Security, "ModTime", typeof(DateTime), false);

		/// <summary>
		/// Дата окончания обращения инструмента.
		/// </summary>
		public static readonly AlorColumn CancellationDate = new AlorColumn(AlorTableTypes.Security, "Cancellation", typeof(DateTime));

		/// <summary>
		/// Дата исполнения контракта.
		/// </summary>
		public static readonly AlorColumn ExecutionDate = new AlorColumn(AlorTableTypes.Security, "ExecTerm", typeof(DateTime));

		/// <summary>
		/// Гарантийные обязательства покупателя.
		/// </summary>
		public static readonly AlorColumn BuyDeposit = new AlorColumn(AlorTableTypes.Security, "BuyDeposit", typeof(decimal));

		/// <summary>
		/// Гарантийные обязательства продавца.
		/// </summary>
		public static readonly AlorColumn SellDeposit = new AlorColumn(AlorTableTypes.Security, "SellDeposit", typeof(decimal));

		/// <summary>
		/// Тип опциона.
		/// </summary>
		public static readonly AlorColumn OptionType = new AlorColumn(AlorTableTypes.Security, "OptionType", typeof(string));

		/// <summary>
		/// Разновидность опциона.
		/// </summary>
		public static readonly AlorColumn OptionStyle = new AlorColumn(AlorTableTypes.Security, "OptionStyle", typeof(string),false);

		/// <summary>
		/// Цена страйк.
		/// </summary>
		public static readonly AlorColumn Strike = new AlorColumn(AlorTableTypes.Security, "Strike", typeof(decimal),false);

		/// <summary>
		/// Код фьючерсного инструмента.
		/// </summary>
		public static readonly AlorColumn FutureCode = new AlorColumn(AlorTableTypes.Security, "FutCode", typeof(string));

		/// <summary>
		/// Котировка фьючерсного инструмента.
		/// </summary>
		public static readonly AlorColumn FuturePrice = new AlorColumn(AlorTableTypes.Security, "FutPrice", typeof(decimal),false);

		/// <summary>
		/// Расчетная цена опциона.
		/// </summary>
		public static readonly AlorColumn OptionPrice = new AlorColumn(AlorTableTypes.Security, "OptPrice", typeof(decimal), false);

		/// <summary>
		/// Волатильность финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Volatility = new AlorColumn(AlorTableTypes.Security, "Volatility", typeof(decimal), false);

		/// <summary>
		/// Верхний лимит цены (задается относительно PrevPrice).
		/// </summary>
		public static readonly AlorColumn HighPriceLimit = new AlorColumn(AlorTableTypes.Security, "HighPriceLimit", typeof(decimal));

		/// <summary>
		/// Нижний лимит цены (задается относительно PrevPrice).
		/// </summary>
		public static readonly AlorColumn LowPriceLimit = new AlorColumn(AlorTableTypes.Security, "LowPriceLimit", typeof(decimal));

		/// <summary>
		/// Тип финансового инструмента.
		/// </summary>
		public static readonly AlorColumn Type = new AlorColumn(AlorTableTypes.Security, "Type", typeof(string),false);

		/// <summary>
		/// Лучшая котировка на покупку.
		/// </summary>
		public static readonly AlorColumn BestBidPrice = new AlorColumn(AlorTableTypes.Security, "Bid", typeof(decimal));

		/// <summary>
		/// Объем заявок на покупку по лучшей котировке.
		/// </summary>
		public static readonly AlorColumn BestBidVolume = new AlorColumn(AlorTableTypes.Security, "BidDepth", typeof(int));

		/// <summary>
		/// Объем всех заявок на покупку в очереди торговой системы.
		/// </summary>
		public static readonly AlorColumn BidVolumes = new AlorColumn(AlorTableTypes.Security, "BidDepthT", typeof(int), false);

		/// <summary>
		/// Количество заявок на покупку в очереди торговой системы.
		/// </summary>
		public static readonly AlorColumn BidsCount = new AlorColumn(AlorTableTypes.Security, "NumBids", typeof(int), false);

		/// <summary>
		/// Наибольшая цена спроса в течение торговой сессии.
		/// </summary>
		public static readonly AlorColumn MaxBidPrice = new AlorColumn(AlorTableTypes.Security, "HighBid", typeof(decimal), false);

		/// <summary>
		/// Лучшая котировка на продажу.
		/// </summary>
		public static readonly AlorColumn BestAskPrice = new AlorColumn(AlorTableTypes.Security, "Offer", typeof(decimal));

		/// <summary>
		/// Объем заявок на продажу по лучшей котировке.
		/// </summary>
		public static readonly AlorColumn BestAskVolume = new AlorColumn(AlorTableTypes.Security, "OfferDepth", typeof(int), false);

		/// <summary>
		/// Объем всех заявок на продажу в очереди торговой системы.
		/// </summary>
		public static readonly AlorColumn AskVolumes = new AlorColumn(AlorTableTypes.Security, "OfferDepthT", typeof(int), false);

		/// <summary>
		/// Количество заявок на продажу в очереди торговой системы.
		/// </summary>
		public static readonly AlorColumn AsksCount = new AlorColumn(AlorTableTypes.Security, "NumOffers", typeof(int), false);

		/// <summary>
		/// Наименьшая цена предложения в течение торговой сессии.
		/// </summary>
		public static readonly AlorColumn MinAskPrice = new AlorColumn(AlorTableTypes.Security, "LowOffer", typeof(decimal), false);

		/// <summary>
		/// Цена открытия.
		/// </summary>
		public static readonly AlorColumn OpenPrice = new AlorColumn(AlorTableTypes.Security, "Open", typeof(decimal));

		/// <summary>
		/// Минимальное значение цены в текущей сессии.
		/// </summary>
		public static readonly AlorColumn LowPrice = new AlorColumn(AlorTableTypes.Security, "Low", typeof(decimal));

		/// <summary>
		/// Максимальное значение цены в текущей сессии.
		/// </summary>
		public static readonly AlorColumn HighPrice = new AlorColumn(AlorTableTypes.Security, "High", typeof(decimal));

		/// <summary>
		/// Цена последней сделки.
		/// </summary>
		public static readonly AlorColumn LastTradePrice = new AlorColumn(AlorTableTypes.Security, "Last", typeof(decimal));

		/// <summary>
		/// Изменение цены последней сделки по отношению к цене последней сделки предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn LastPriceChange = new AlorColumn(AlorTableTypes.Security, "LastChange", typeof(DateTime), false);

		/// <summary>
		/// Количество лотов в последней сделке.
		/// </summary>
		public static readonly AlorColumn LastTradeVolume = new AlorColumn(AlorTableTypes.Security, "Quantity", typeof(int));

		/// <summary>
		/// Доходность.
		/// </summary>
		public static readonly AlorColumn Yield = new AlorColumn(AlorTableTypes.Security, "Yield", typeof(decimal), false);

		/// <summary>
		/// Накопленный купонный доход на дату торгов в расчете на одну бумагу.
		/// </summary>
		public static readonly AlorColumn AccruedInt = new AlorColumn(AlorTableTypes.Security, "AccruedInt", typeof(decimal), false);

		/// <summary>
		/// Индикатор информирующий.
		/// </summary>
		public static readonly AlorColumn PrimaryDist = new AlorColumn(AlorTableTypes.Security, "PrimaryDist", typeof(string), false);

		/// <summary>
		/// Дата погашения.
		/// </summary>
		public static readonly AlorColumn MatureDate = new AlorColumn(AlorTableTypes.Security, "MatDate", typeof(DateTime), false);

		/// <summary>
		/// Время последней сделки.
		/// </summary>
		public static readonly AlorColumn LastTradeTime = new AlorColumn(AlorTableTypes.Security, "Time", typeof(DateTime));

		/// <summary>
		/// Количество сделок за торговый день.
		/// </summary>
		public static readonly AlorColumn TradesCount = new AlorColumn(AlorTableTypes.Security, "NumTrades", typeof(int), false);

		/// <summary>
		/// Объем совершенных сделок, выраженный в единицах ценных бумаг.
		/// </summary>
		public static readonly AlorColumn VolumeToday = new AlorColumn(AlorTableTypes.Security, "VolToday", typeof(decimal), false);

		/// <summary>
		/// Объем совершенных сделок.
		/// </summary>
		public static readonly AlorColumn ValueToday = new AlorColumn(AlorTableTypes.Security, "ValToday", typeof(decimal), false);

		/// <summary>
		/// Объем последней сделки.
		/// </summary>
		public static readonly AlorColumn LastTradeValue = new AlorColumn(AlorTableTypes.Security, "Value", typeof(decimal), false);

		/// <summary>
		/// Рыночная цена предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn PrevMarketPrice = new AlorColumn(AlorTableTypes.Security, "PrevMarketPrice", typeof(decimal), false);

		/// <summary>
		/// Рыночная цена по результатам торгов текущего торгового дня.
		/// </summary>
		public static readonly AlorColumn MarketPrice = new AlorColumn(AlorTableTypes.Security, "MarketPrice", typeof(decimal), false);

		/// <summary>
		/// Рыночная цена 2.
		/// </summary>
		public static readonly AlorColumn MarketPrice2 = new AlorColumn(AlorTableTypes.Security, "MarketPrice2", typeof(decimal), false);

		/// <summary>
		/// Официальная цена закрытия предыдущего торгового дня.
		/// </summary>
		public static readonly AlorColumn LegalPrevPrice = new AlorColumn(AlorTableTypes.Security, "LegalPrevPrice", typeof(decimal), false);

		/// <summary>
		/// Официальная цена открытия.
		/// </summary>
		public static readonly AlorColumn LegalOpenPrice = new AlorColumn(AlorTableTypes.Security, "LegalOpenPrice", typeof(decimal), false);

		/// <summary>
		/// Официальная текущая цена.
		/// </summary>
		public static readonly AlorColumn LegalCurrentPrice = new AlorColumn(AlorTableTypes.Security, "LegalCurrentPrice", typeof(decimal), false);

		/// <summary>
		/// Официальная цена закрытия.
		/// </summary>
		public static readonly AlorColumn LegalClosePrice = new AlorColumn(AlorTableTypes.Security, "LegalClosePrice", typeof(decimal), false);

		/// <summary>
		/// Наименование для главного инструмента.
		/// </summary>
		public static readonly AlorColumn Ticker = new AlorColumn(AlorTableTypes.Security, "Ticker", typeof(string), false);
	}
}