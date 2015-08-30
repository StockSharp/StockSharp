namespace StockSharp.IQFeed
{
	using System;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Список всех доступных <see cref="IQFeedLevel1Column"/>.
	/// </summary>
	public class IQFeedLevel1ColumnRegistry
	{
		internal IQFeedLevel1ColumnRegistry()
		{
			foreach (var field in typeof(IQFeedLevel1ColumnRegistry).GetFields())
			{
				var column = field.GetValue(this) as IQFeedLevel1Column;

				if (column != null)
					_columns.Add(column.Name, column);
			}
		}

		private readonly SynchronizedDictionary<string, IQFeedLevel1Column> _columns = new SynchronizedDictionary<string, IQFeedLevel1Column>(StringComparer.InvariantCultureIgnoreCase);

		private const string _dateFormat = "MM/dd/yyyy";
		private const string _timeFormat = "hh\\:mm\\:ss\\.fff";

		/// <summary>
		/// Получить колонку по имени <see cref="IQFeedLevel1Column.Name"/>.
		/// </summary>
		/// <param name="name">Название колонки.</param>
		/// <returns>Найденная колонка. Если колонка не существует, то будет возвращено <see langword="null"/>.</returns>
		public IQFeedLevel1Column this[string name]
		{
			get { return _columns.TryGetValue(name); }
		}

		/// <summary>
		/// Код инструмента.
		/// </summary>
		public readonly IQFeedLevel1Column Symbol = new IQFeedLevel1Column("Symbol", typeof(string));

		/// <summary>
		/// Код площадки.
		/// </summary>
		public readonly IQFeedLevel1Column ExchangeId = new IQFeedLevel1Column("Exchange ID", typeof(string));

		/// <summary>
		/// Цена последней сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradePrice = new IQFeedLevel1Column("Last", typeof(decimal)) { Field = Level1Fields.LastTradePrice };

		/// <summary>
		/// Общий объем за сессию.
		/// </summary>
		public readonly IQFeedLevel1Column TotalVolume = new IQFeedLevel1Column("Total Volume", typeof(decimal)) { Field = Level1Fields.Volume };

		/// <summary>
		/// Наивысшая цена за сессию.
		/// </summary>
		public readonly IQFeedLevel1Column High = new IQFeedLevel1Column("High", typeof(decimal)) { Field = Level1Fields.HighPrice };

		/// <summary>
		/// Наименьшая цена за сессию.
		/// </summary>
		public readonly IQFeedLevel1Column Low = new IQFeedLevel1Column("Low", typeof(decimal)) { Field = Level1Fields.LowPrice };

		/// <summary>
		/// Цена бида.
		/// </summary>
		public readonly IQFeedLevel1Column BidPrice = new IQFeedLevel1Column("Bid", typeof(decimal)) { Field = Level1Fields.BestBidPrice };

		/// <summary>
		/// Цена офера.
		/// </summary>
		public readonly IQFeedLevel1Column AskPrice = new IQFeedLevel1Column("Ask", typeof(decimal)) { Field = Level1Fields.BestAskPrice };

		/// <summary>
		/// Объем бида.
		/// </summary>
		public readonly IQFeedLevel1Column BidVolume = new IQFeedLevel1Column("Bid Size", typeof(decimal)) { Field = Level1Fields.BestBidVolume };

		/// <summary>
		/// Объем офера.
		/// </summary>
		public readonly IQFeedLevel1Column AskVolume = new IQFeedLevel1Column("Ask Size", typeof(decimal)) { Field = Level1Fields.BestAskVolume };

		/// <summary>
		/// Открытый интерес.
		/// </summary>
		public readonly IQFeedLevel1Column OpenInterest = new IQFeedLevel1Column("Open Interest", typeof(decimal)) { Field = Level1Fields.OpenInterest };

		/// <summary>
		/// Цена открытия.
		/// </summary>
		public readonly IQFeedLevel1Column Open = new IQFeedLevel1Column("Open", typeof(decimal)) { Field = Level1Fields.OpenPrice };

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		public readonly IQFeedLevel1Column Close = new IQFeedLevel1Column("Close", typeof(decimal)) { Field = Level1Fields.ClosePrice };

		/// <summary>
		/// Расчетное значение.
		/// </summary>
		public readonly IQFeedLevel1Column Settle = new IQFeedLevel1Column("Settle", typeof(decimal)) { Field = Level1Fields.SettlementPrice };

		/// <summary>
		/// Время задержки данных в минутах (если используется не риал-тайм данные).
		/// </summary>
		public readonly IQFeedLevel1Column Delay = new IQFeedLevel1Column("Delay", typeof(int));

		/// <summary>
		/// Флаг, означающий допустимость коротких продаж.
		/// </summary>
		public readonly IQFeedLevel1Column ShortSaleRestrictedCode = new IQFeedLevel1Column("Restricted Code", typeof(string));

		/// <summary>
		/// Значение чистой доходности для пифов.
		/// </summary>
		public readonly IQFeedLevel1Column NetAssetValueMutualFonds = new IQFeedLevel1Column("Net Asset Value", typeof(decimal));

		/// <summary>
		/// Среднее время до поставки.
		/// </summary>
		public readonly IQFeedLevel1Column AverageDaysMaturity = new IQFeedLevel1Column("Average Maturity", typeof(decimal));

		/// <summary>
		/// Доходность за 7 дней.
		/// </summary>
		public readonly IQFeedLevel1Column SevenDayYield = new IQFeedLevel1Column("7 Day Yield", typeof(decimal));

		/// <summary>
		/// Значение чистой доходности для FX.
		/// </summary>
		public readonly IQFeedLevel1Column NetAssetValueFx = new IQFeedLevel1Column("Net Asset Value 2", typeof(decimal));

		/// <summary>
		/// Признак открытия рынка.
		/// </summary>
		public readonly IQFeedLevel1Column MarketOpen = new IQFeedLevel1Column("Market Open", typeof(int));

		/// <summary>
		/// Формат дробной цены.
		/// </summary>
		public readonly IQFeedLevel1Column FractionDisplayCode = new IQFeedLevel1Column("Fraction Display Code", typeof(string));

		/// <summary>
		/// Точность после запятой.
		/// </summary>
		public readonly IQFeedLevel1Column DecimalPrecision = new IQFeedLevel1Column("Decimal Precision", typeof(string));

		/// <summary>
		/// Объем за предыдущую торговую сессию.
		/// </summary>
		public readonly IQFeedLevel1Column PrevDayVolume = new IQFeedLevel1Column("Previous Day Volume", typeof(decimal));

		/// <summary>
		/// Диапазон открытия.
		/// </summary>
		public readonly IQFeedLevel1Column OpenRange1 = new IQFeedLevel1Column("Open Range 1", typeof(decimal));

		/// <summary>
		/// Диапазон закрытия.
		/// </summary>
		public readonly IQFeedLevel1Column CloseRange1 = new IQFeedLevel1Column("Close Range 1", typeof(decimal));

		/// <summary>
		/// Диапазон открытия.
		/// </summary>
		public readonly IQFeedLevel1Column OpenRange2 = new IQFeedLevel1Column("Open Range 2", typeof(decimal));

		/// <summary>
		/// Диапазон закрытия.
		/// </summary>
		public readonly IQFeedLevel1Column CloseRange2 = new IQFeedLevel1Column("Close Range 2", typeof(decimal));

		/// <summary>
		/// Количество сделок за сессию.
		/// </summary>
		public readonly IQFeedLevel1Column TradeCount = new IQFeedLevel1Column("Number of Trades Today", typeof(int)) { Field = Level1Fields.TradesCount };

		/// <summary>
		/// VWAP.
		/// </summary>
		public readonly IQFeedLevel1Column VWAP = new IQFeedLevel1Column("VWAP", typeof(decimal)) { Field = Level1Fields.VWAP };

		/// <summary>
		/// Идентификатор последней сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeId = new IQFeedLevel1Column("TickID", typeof(long)) { Field = Level1Fields.LastTradeId };

		/// <summary>
		/// Код индикатора.
		/// </summary>
		public readonly IQFeedLevel1Column FinancialStatusIndicator = new IQFeedLevel1Column("Financial Status Indicator", typeof(string));

		/// <summary>
		/// Дата поставки.
		/// </summary>
		public readonly IQFeedLevel1Column SettlementDate = new IQFeedLevel1Column("Settlement Date", typeof(DateTime), _dateFormat);

		/// <summary>
		/// Идентификатор рынка бида.
		/// </summary>
		public readonly IQFeedLevel1Column BidMarket = new IQFeedLevel1Column("Bid Market Center", typeof(int));

		/// <summary>
		/// Идентификатор рынка офера.
		/// </summary>
		public readonly IQFeedLevel1Column AskMarket = new IQFeedLevel1Column("Ask Market Center", typeof(int));

		/// <summary>
		/// Доступные регионы.
		/// </summary>
		public readonly IQFeedLevel1Column AvailableRegions = new IQFeedLevel1Column("Available Regions", typeof(string));

		/// <summary>
		/// Объем последней сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeVolume = new IQFeedLevel1Column("Last Size", typeof(decimal)) { Field = Level1Fields.LastTradeVolume };

		/// <summary>
		/// Время последней сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeTime = new IQFeedLevel1Column("Last TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.LastTradeTime };

		/// <summary>
		/// Идентификатор рынка последней сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastTradeMarket = new IQFeedLevel1Column("Last Market Center", typeof(int));

		/// <summary>
		/// Наиболее частая цена сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradePrice = new IQFeedLevel1Column("Most Recent Trade", typeof(decimal));

		/// <summary>
		/// Наиболее частый объем сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeVolume = new IQFeedLevel1Column("Most Recent Trade Size", typeof(decimal));

		/// <summary>
		/// Наиболее частое время сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeTime = new IQFeedLevel1Column("Most Recent Trade TimeMS", typeof(TimeSpan), _timeFormat);

		/// <summary>
		/// Наиболее частое условие сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeConditions = new IQFeedLevel1Column("Most Recent Trade Conditions", typeof(string));

		/// <summary>
		/// Идентификатор рынка наиболее частой сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeMarket = new IQFeedLevel1Column("Most Recent Trade Market Center", typeof(int));

		/// <summary>
		/// Цена последней расширенной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradePrice = new IQFeedLevel1Column("Extended Trade", typeof(decimal));

		/// <summary>
		/// Объем последней расширенной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeVolume = new IQFeedLevel1Column("Extended Trade Size", typeof(decimal));

		/// <summary>
		/// Время последней расширенной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeTime = new IQFeedLevel1Column("Extended Trade TimeMS", typeof(TimeSpan), _timeFormat);

		/// <summary>
		/// Идентификатор рынка последней расширенной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column ExtendedTradeMarket = new IQFeedLevel1Column("Extended Trade Market Center", typeof(int));

		/// <summary>
		/// Коды контента.
		/// </summary>
		public readonly IQFeedLevel1Column MessageContents = new IQFeedLevel1Column("Message Contents", typeof(string));

		/// <summary>
		/// Время офера.
		/// </summary>
		public readonly IQFeedLevel1Column AskTime = new IQFeedLevel1Column("Ask TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.BestAskTime };

		/// <summary>
		/// Время бида.
		/// </summary>
		public readonly IQFeedLevel1Column BidTime = new IQFeedLevel1Column("Bid TimeMS", typeof(TimeSpan), _timeFormat) { Field = Level1Fields.BestBidTime };

		/// <summary>
		/// Время последней кратной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastDate = new IQFeedLevel1Column("Last Date", typeof(DateTime), _dateFormat);
		
		/// <summary>
		/// Дата последней расширенной сделки.
		/// </summary>
		public readonly IQFeedLevel1Column LastExtendedTradeDate = new IQFeedLevel1Column("Extended Trade Date", typeof(DateTime), _dateFormat);
		
		/// <summary>
		/// Наиболее частая дата сделки.
		/// </summary>
		public readonly IQFeedLevel1Column MostRecentTradeDate = new IQFeedLevel1Column("Most Recent Trade Date", typeof(DateTime), _dateFormat);
	}
}