namespace StockSharp.BitStamp.Native.Model
{
	using System;
	using System.Reflection;

	using Ecng.Net;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Order
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("order_type")]
		public int Type { get; set; }

		//[JsonProperty("datetime")]
		//[JsonConverter(typeof(JsonDateTimeConverter))]
		//public DateTime Time { get; set; }

		[JsonProperty("microtimestamp")]
		[JsonConverter(typeof(JsonDateTimeMcsConverter))]
		public DateTime Time { get; set; }
	}
}