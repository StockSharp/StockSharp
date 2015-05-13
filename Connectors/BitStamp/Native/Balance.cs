namespace StockSharp.BitStamp.Native
{
	using Newtonsoft.Json;

	class Balance
	{
		[JsonProperty("usd_balance")]
		public double UsdBalance { get; set; }

		[JsonProperty("btc_balance")]
		public double BtcBalance { get; set; }

		[JsonProperty("usd_reserved")]
		public double UsdReserved { get; set; }

		[JsonProperty("btc_reserved")]
		public double BtcReserved { get; set; }

		[JsonProperty("usd_available")]
		public double UsdAvailable { get; set; }

		[JsonProperty("btc_available")]
		public double BtcAvailable { get; set; }

		[JsonProperty("fee")]
		public double Fee { get; set; }
	}
}