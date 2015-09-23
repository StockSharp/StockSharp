namespace StockSharp.InteractiveBrokers.Web
{
	/// <summary>
	/// Information about the instrument on the specific board.
	/// </summary>
	public class ProductBoard
	{
		/// <summary>
		/// List of exchanges.
		/// </summary>
		public string Markets;

		/// <summary>
		/// Unique ID.
		/// </summary>
		public string Id;

		/// <summary>
		/// Name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Class.
		/// </summary>
		public string Class;

		/// <summary>
		/// Deliverable.
		/// </summary>
		public string SettlementMethod;

		/// <summary>
		/// Exchange web site.
		/// </summary>
		public string ExchangeWebsite;

		/// <summary>
		/// Business hours.
		/// </summary>
		public string[] TradingHours;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange1;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange2;

		/// <summary>
		/// The price range.
		/// </summary>
		public string PriceRange3;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange1;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange2;

		/// <summary>
		/// The volume range.
		/// </summary>
		public decimal VolumeRange3;
	}
}