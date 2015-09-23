namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Messages;

	/// <summary>
	/// The filter result of scanner starting via <see cref="IBTrader.SubscribeScanner"/>.
	/// </summary>
	public class ScannerResult
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; set; }
		
		/// <summary>
		/// Rank.
		/// </summary>
		public int Rank { get; set; }
		
		/// <summary>
		/// The query value.
		/// </summary>
		public string Distance { get; set; }

		/// <summary>
		/// The query value.
		/// </summary>
		public string Benchmark { get; set; }

		/// <summary>
		/// The query value.
		/// </summary>
		public string Projection { get; set; }

		/// <summary>
		/// The combined instrument description.
		/// </summary>
		public string Legs { get; set; }
	}
}