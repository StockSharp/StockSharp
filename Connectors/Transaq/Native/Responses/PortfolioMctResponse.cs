namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	class PortfolioMctResponse : BaseResponse
	{
		/// <summary>
		/// Код клиента.
		/// </summary>
		public string Client { get; set; }

		/// <summary>
		/// Валюта портфеля клиента.
		/// </summary>
		public string Currency { get; set; }

		/// <summary>
		/// Величина капитала.
		/// </summary>
		public decimal Capital { get; set; }

		/// <summary>
		/// Использование капитала (фактическое).
		/// </summary>
		public decimal UtilizationFact { get; set; }

		/// <summary>
		/// Использование капитала (плановое).
		/// </summary>
		public decimal UtilizationPlan { get; set; }

		/// <summary>
		/// Фактическая обеспеченность.
		/// </summary>
		public decimal CoverageFact { get; set; }

		/// <summary>
		/// Плановая обеспеченность.
		/// </summary>
		public decimal CoveragePlan { get; set; }

		/// <summary>
		/// Входящее сальдо.
		/// </summary>
		public decimal OpenBalance { get; set; }

		/// <summary>
		/// Суммарная комиссия.
		/// </summary>
		public decimal Tax { get; set; }

		/// <summary>
		/// Прибыль/убыток по входящим позициям.
		/// </summary>
		public decimal PnLIncome { get; set; }

		/// <summary>
		/// Прибыль/убыток по сделкам.
		/// </summary>
		public decimal PnLIntraday { get; set; }

		public IEnumerable<MctSecurity> Securities { get; set; }
	}

	class MctSecurity
	{
		/// <summary>
		/// Id инструмента.
		/// </summary>
		public string SecId { get; set; }

		/// <summary>
		/// Id рынка.
		/// </summary>
		public int Market { get; set; }

		/// <summary>
		/// Обозначение инструмента.
		/// </summary>
		public string SecCode { get; set; }

		/// <summary>
		/// Валюта цены инструмента.
		/// </summary>
		public string Currency { get; set; }

		/// <summary>
		/// Ставка ГО (либо long, либо short, в зависимости от позиции клиента) по инструменту для клиента.
		/// </summary>
		public decimal GoRate { get; set; }

		/// <summary>
		/// Ставка ГО long по инструменту для клиента.
		/// </summary>
		public decimal GoRateLong { get; set; }

		/// <summary>
		/// Ставка ГО short по инструменту для клиента.
		/// </summary>
		public decimal GoRateShort { get; set; }

		/// <summary>
		/// Текущая цена.
		/// </summary>
		public decimal Price { get; set; }

		/// <summary>
		/// Входящая цена позиции (цена последнего клиринга).
		/// </summary>
		public decimal InitRate { get; set; }

		/// <summary>
		/// Кросс-курс валюты портфеля к валюте контракта.
		/// </summary>
		public decimal CrossRate { get; set; }

		/// <summary>
		/// Входящий кросс-курс валюты портфеля к валюте контракта.
		/// </summary>
		public decimal InitCrossRate { get; set; }

		/// <summary>
		/// Входящяя позиция, штук.
		/// </summary>
		public int OpenBalance { get; set; }

		/// <summary>
		/// Куплено, штук.
		/// </summary>
		public int Bought { get; set; }

		/// <summary>
		/// Продано, штук.
		/// </summary>
		public int Sold { get; set; }

		/// <summary>
		/// Текущая позиция, штук.
		/// </summary>
		public int Balance { get; set; }

		/// <summary>
		/// Заявлено купить, штук.
		/// </summary>
		public int Buying { get; set; }

		/// <summary>
		/// Заявлено продать, штук.
		/// </summary>
		public int Selling { get; set; }

		/// <summary>
		/// Текущая стоимость позиции.
		/// </summary>
		public decimal PosCost { get; set; }

		/// <summary>
		/// ГО позиции (фактическое).
		/// </summary>
		public decimal GoPosFact { get; set; }

		/// <summary>
		/// ГО позиции (плановое).
		/// </summary>
		public decimal GoPosPlan { get; set; }

		/// <summary>
		/// Комиссия по сделкам в инструменте.
		/// </summary>
		public decimal Tax { get; set; }

		/// <summary>
		/// Прибыль/убыток по входящим позициям в инструменте.
		/// </summary>
		public decimal PnLIncome { get; set; }

		/// <summary>
		/// Прибыль/убыток по сделкам в инструменте.
		/// </summary>
		public decimal PnLIntraday { get; set; }

		/// <summary>
		/// Максимум купить (лот).
		/// </summary>
		public long MaxBuy { get; set; }

		/// <summary>
		/// Максимум продать (лот).
		/// </summary>
		public long MaxSell { get; set; }

		/// <summary>
		/// Средняя цена покупки.
		/// </summary>
		public decimal BoughtAverage { get; set; }

		/// <summary>
		/// Средняя цена продажи.
		/// </summary>
		public decimal SoldAverage { get; set; }
	}
}