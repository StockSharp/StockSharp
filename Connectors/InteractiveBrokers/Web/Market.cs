namespace StockSharp.InteractiveBrokers.Web
{
	/// <summary>
	/// Информация о бирже.
	/// </summary>
	public class Market
	{
		/// <summary>
		/// Уникальный идентификатор.
		/// </summary>
		public string Id;

		/// <summary>
		/// Название.
		/// </summary>
		public string Name;

		/// <summary>
		/// Страна.
		/// </summary>
		public string Country;

		//public string Products;

		/// <summary>
		/// Часы работы.
		/// </summary>
		public string Hours;
	}
}