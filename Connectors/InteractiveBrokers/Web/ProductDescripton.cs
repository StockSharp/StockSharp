namespace StockSharp.InteractiveBrokers.Web
{
	using StockSharp.Messages;

	/// <summary>
	/// Detailed information about the instrument.
	/// </summary>
	public class ProductDescripton
	{
		/// <summary>
		/// Name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Code.
		/// </summary>
		public string Symbol;

		/// <summary>
		/// Type.
		/// </summary>
		public SecurityTypes Type;

		/// <summary>
		/// Country.
		/// </summary>
		public string Country;

		/// <summary>
		/// Closing price.
		/// </summary>
		public decimal ClosingPrice;

		/// <summary>
		/// Currency.
		/// </summary>
		public string Currency;

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public string AssetId;

		/// <summary>
		/// Type of shares.
		/// </summary>
		public string StockType;

		/// <summary>
		/// Margin funds.
		/// </summary>
		public string InitialMargin;

		/// <summary>
		/// Margin funds.
		/// </summary>
		public string MaintenanceMargin;

		/// <summary>
		/// Margin funds under short position.
		/// </summary>
		public string ShortMargin;
	}
}