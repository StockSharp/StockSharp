namespace StockSharp.InteractiveBrokers.Web
{
	/// <summary>
	/// Информация об инструменте на конкретной площадке.
	/// </summary>
	public class ProductBoard
	{
		/// <summary>
		/// Список бирж.
		/// </summary>
		public string Markets;

		/// <summary>
		/// Уникальный идентификатор.
		/// </summary>
		public string Id;

		/// <summary>
		/// Название.
		/// </summary>
		public string Name;

		/// <summary>
		/// Класс.
		/// </summary>
		public string Class;

		/// <summary>
		/// Поставочный.
		/// </summary>
		public string SettlementMethod;

		/// <summary>
		/// Сайт биржи.
		/// </summary>
		public string ExchangeWebsite;

		/// <summary>
		/// Часы торговли.
		/// </summary>
		public string[] TradingHours;

		/// <summary>
		/// Ценовой диапазон.
		/// </summary>
		public string PriceRange1;

		/// <summary>
		/// Ценовой диапазон.
		/// </summary>
		public string PriceRange2;

		/// <summary>
		/// Ценовой диапазон.
		/// </summary>
		public string PriceRange3;

		/// <summary>
		/// Сайз диапазон.
		/// </summary>
		public decimal VolumeRange1;

		/// <summary>
		/// Сайз диапазон.
		/// </summary>
		public decimal VolumeRange2;

		/// <summary>
		/// Сайз диапазон.
		/// </summary>
		public decimal VolumeRange3;
	}
}