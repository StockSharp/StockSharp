namespace StockSharp.InteractiveBrokers.Web
{
	using StockSharp.Messages;

	/// <summary>
	/// Детальная информация об инструменте.
	/// </summary>
	public class ProductDescripton
	{
		/// <summary>
		/// Название.
		/// </summary>
		public string Name;

		/// <summary>
		/// Код.
		/// </summary>
		public string Symbol;

		/// <summary>
		/// Тип.
		/// </summary>
		public SecurityTypes Type;

		/// <summary>
		/// Страна.
		/// </summary>
		public string Country;

		/// <summary>
		/// Цена закрытия.
		/// </summary>
		public decimal ClosingPrice;

		/// <summary>
		/// Валюта.
		/// </summary>
		public string Currency;

		/// <summary>
		/// Базовый актив.
		/// </summary>
		public string AssetId;

		/// <summary>
		/// Тип акции.
		/// </summary>
		public string StockType;

		/// <summary>
		/// Маржинальное обеспечение.
		/// </summary>
		public string InitialMargin;

		/// <summary>
		/// Маржинальное обеспечение.
		/// </summary>
		public string MaintenanceMargin;

		/// <summary>
		/// Маржинальное обеспечение при короткой позиции.
		/// </summary>
		public string ShortMargin;
	}
}