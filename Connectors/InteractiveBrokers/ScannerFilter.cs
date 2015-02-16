namespace StockSharp.InteractiveBrokers
{
	using System;

	/// <summary>
	/// Типы исключений акций.
	/// </summary>
	public enum ScannerFilterStockExcludes
	{
		/// <summary>
		/// Не исключать ничего.
		/// </summary>
		All,

		/// <summary>
		/// Исключить <see cref="Etf"/>.
		/// </summary>
		Stock,

		/// <summary>
		/// Только Exchange-traded fund.
		/// </summary>
		Etf
	}

	/// <summary>
	/// Настройки фильтра сканера, запускаемого через <see cref="IBTrader.SubscribeScanner"/>.
	/// </summary>
	public class ScannerFilter
	{
		/// <summary>
		/// Создать <see cref="ScannerFilter"/>.
		/// </summary>
		public ScannerFilter()
		{
		}

		/// <summary>
		/// Количество строк в запросе.
		/// </summary>
		public int? RowCount { get; set; }

		/// <summary>
		/// Тип инструмента.
		/// </summary>
		public string SecurityType { get; set; }

		/// <summary>
		/// Биржевая площадка.
		/// </summary>
		public string BoardCode { get; set; }

		/// <summary>
		///  
		/// </summary>
		public string ScanCode { get; set; }

		/// <summary>
		/// Верхний предел рыночной цены инструмента.
		/// </summary>
		public decimal? AbovePrice { get; set; }

		/// <summary>
		/// Нижний предел рыночной цены инструмента.
		/// </summary>
		public decimal? BelowPrice { get; set; }

		/// <summary>
		/// Верхний предел объема торгов по инструменту.
		/// </summary>
		public int? AboveVolume { get; set; }

		/// <summary>
		/// Верхний предел объема торгов по опциону.
		/// </summary>
		public int? AverageOptionVolumeAbove { get; set; }

		/// <summary>
		/// Верхний предел капитализации.
		/// </summary>
		public decimal? MarketCapAbove { get; set; }

		/// <summary>
		/// Нижний предел капитализации.
		/// </summary>
		public decimal? MarketCapBelow { get; set; }

		/// <summary>
		/// Верхний предел рейтинга Moody.
		/// </summary>
		public string MoodyRatingAbove { get; set; }

		/// <summary>
		/// Нижний предел рейтинга Moody.
		/// </summary>
		public string MoodyRatingBelow { get; set; }

		/// <summary>
		/// Верхний предел рейтинга SP.
		/// </summary>
		public string SpRatingAbove { get; set; }

		/// <summary>
		/// Нижний предел рейтинга SP.
		/// </summary>
		public string SpRatingBelow { get; set; }

		/// <summary>
		/// Верхний предел даты погашения инструмента.
		/// </summary>
		public DateTime? MaturityDateAbove { get; set; }

		/// <summary>
		/// Нижний предел даты погашения инструмента.
		/// </summary>
		public DateTime? MaturityDateBelow { get; set; }

		/// <summary>
		/// Верхний предел купонной ставки.
		/// </summary>
		public decimal? CouponRateAbove { get; set; }

		/// <summary>
		/// Нижний предел купонной ставки.
		/// </summary>
		public decimal? CouponRateBelow { get; set; }

		/// <summary>
		/// Исключать конвертируемые облигации.
		/// </summary>
		public bool ExcludeConvertibleBonds { get; set; }

		/// <summary>
		/// Расширенные параметры. Подробнее, http://www.interactivebrokers.com/en/software/tws/usersguidebook/technicalanalytics/market_scanner_types.htm .
		/// </summary>
		public string ScannerSettingPairs { get; set; }

		/// <summary>
		/// Тип исключений акций.
		/// </summary>
		public ScannerFilterStockExcludes StockTypeExclude { get; set; }
	}
}