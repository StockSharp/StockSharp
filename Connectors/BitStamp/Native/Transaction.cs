namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Net;

	using Newtonsoft.Json;

	class Transaction
	{
		[JsonProperty("date")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		[JsonProperty("tid")]
		public long Id { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }
	}
}