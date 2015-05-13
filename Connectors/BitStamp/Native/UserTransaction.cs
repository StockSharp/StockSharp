namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Net;

	using Newtonsoft.Json;

	class UserTransaction
	{
		[JsonProperty("datetime")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("type")]
		public int Type { get; set; }

		[JsonProperty("usd")]
		public double UsdAmount { get; set; }

		[JsonProperty("btc")]
		public double BtcAmount { get; set; }

		[JsonProperty("fee")]
		public double Fee { get; set; }

		[JsonProperty("order_id")]
		public long OrderId { get; set; }
	}
}