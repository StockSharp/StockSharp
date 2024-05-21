namespace StockSharp.Coinbase.Native.Model
{
	using System;
	using System.Reflection;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Heartbeat
	{
		[JsonProperty("last_trade_id")]
		public long? LastTradeId { get; set; }

		[JsonProperty("sequence")]
		public long Sequence { get; set; }

		[JsonProperty("product_id")]
		public string Product { get; set; }

		[JsonProperty("time")]
		public DateTime Time { get; set; }
	}
}