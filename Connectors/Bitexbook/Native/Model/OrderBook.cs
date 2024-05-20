namespace StockSharp.Bitexbook.Native.Model
{
	using System;
	using System.Reflection;

	using Ecng.Serialization;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	[JsonConverter(typeof(JArrayToObjectConverter))]
	class OrderBookEntry
	{
		public double Price { get; set; }
		public double Size { get; set; }
	}

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class OrderBook
	{
		[JsonProperty("bids")]
		public OrderBookEntry[] Bids { get; set; }

		[JsonProperty("asks")]
		public OrderBookEntry[] Asks { get; set; }

		[JsonProperty("timestamp")]
		[JsonConverter(typeof(JsonDateTimeMlsConverter))]
		public DateTime Timestamp { get; set; }
	}
}