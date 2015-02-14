namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class PortfolioTPlusResponse : BaseResponse
	{
		/// <summary>
		/// Код клиента.
		/// </summary>
		public string Client { get; set; }

		/// <summary>
		/// Фактическая обеспеченность.
		/// </summary>
		public decimal CoverageFact { get; set; }

		/// <summary>
		/// Плановая обеспеченность.
		/// </summary>
		public decimal CoveragePlan { get; set; }

		/// <summary>
		/// Критическая обеспеченность.
		/// </summary>
		public decimal CoverageCrit { get; set; }

		/// <summary>
		/// Входящая оценка портфеля без дисконта.
		/// </summary>
		public decimal OpenEquity { get; set; }

		/// <summary>
		/// Текущая оценка портфеля без дисконта.
		/// </summary>
		public decimal Equity { get; set; }

		/// <summary>
		/// Плановое обеспечение (оценка ликвидационной стоимости портфеля).
		/// </summary>
		public decimal Cover { get; set; }

		/// <summary>
		/// Плановая начальная маржа (оценка портфельного риска).
		/// </summary>
		public decimal MarginInit { get; set; }

		/// <summary>
		/// Прибыль/убыток по входящим позициям.
		/// </summary>
		public decimal PnLIncome { get; set; }

		/// <summary>
		/// Прибыль/убыток по сделкам.
		/// </summary>
		public decimal PnLIntraday { get; set; }

		/// <summary>
		/// Фактическое плечо.
		/// </summary>
		public decimal Leverage { get; set; }

		/// <summary>
		/// Фактический уровень маржи портфеля.
		/// </summary>
		public decimal MarginActual { get; set; }

		public Money Money { get; set; }

		public IEnumerable<TPlusSecurity> Securities { get; set; } 
	}

	internal class Money
	{
		/// <summary>
		/// Входящяя денежная позиция.
		/// </summary>
		public decimal OpenBalance { get; set; }

		/// <summary>
		/// Затрачено на покупки.
		/// </summary>
		public decimal Bought { get; set; }

		/// <summary>
		/// Выручено с продаж.
		/// </summary>
		public decimal Sold { get; set; }

		/// <summary>
		/// Исполнено.
		/// </summary>
		public decimal Settled { get; set; }

		/// <summary>
		/// Текущая денежная позиция.
		/// </summary>
		public decimal Balance { get; set; }

		/// <summary>
		/// Уплачено комиссии.
		/// </summary>
		public decimal Tax { get; set; }

		public IEnumerable<MoneyValuePart> MoneyValueParts { get; set; }
	}

	internal class MoneyValuePart
	{
		/// <summary>
		/// Регистр учета.
		/// </summary>
		public string Register { get; set; }

		/// <summary>
		/// Входящяя денежная позиция.
		/// </summary>
		public decimal OpenBalance { get; set; }

		/// <summary>
		/// Затрачено на покупки.
		/// </summary>
		public decimal Bought { get; set; }

		/// <summary>
		/// Выручено с продаж.
		/// </summary>
		public decimal Sold { get; set; }

		/// <summary>
		/// Исполнено.
		/// </summary>
		public decimal Settled { get; set; }

		/// <summary>
		/// Текущая денежная позиция.
		/// </summary>
		public decimal Balance { get; set; }
	}

	internal class TPlusSecurity
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
		/// Текущая цена.
		/// </summary>
		public decimal Price { get; set; }

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
		/// Вклад бумаги в плановое обеспечение.
		/// </summary>
		public decimal Cover { get; set; }

		/// <summary>
		/// Плановая начальная маржа(риск).
		/// </summary>
		public decimal InitMargin { get; set; }

		/// <summary>
		/// Прибыль/убыток по входящим позициям.
		/// </summary>
		public decimal PnLIncome { get; set; }

		/// <summary>
		/// Прибыль/убыток по сделкам.
		/// </summary>
		public decimal PnLIntraday { get; set; }

		/// <summary>
		/// Ставка риска для лонгов.
		/// </summary>
		public decimal RiskRateLong { get; set; }

		/// <summary>
		/// Ставка риска для шортов.
		/// </summary>
		public decimal RiskRateShort { get; set; }

		/// <summary>
		/// Максимальная покупка, в лотах.
		/// </summary>
		public int MaxBuy { get; set; }

		/// <summary>
		/// Максимальная продажа, в лотах.
		/// </summary>
		public int MaxSell { get; set; }

		public IEnumerable<TPlusSecurityValuePart> TPlusSecurityValueParts { get; set; }
	}

	internal class TPlusSecurityValuePart
	{
		/// <summary>
		/// Регистр учета.
		/// </summary>
		public string Register { get; set; }

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
		/// Исполнено, штук.
		/// </summary>
		public int Settled { get; set; }

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
	}
}
