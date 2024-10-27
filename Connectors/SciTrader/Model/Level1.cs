using System.Reflection;
using Newtonsoft.Json;
using System;
namespace SciTrader.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Level1
	{
		[JsonProperty("bid")]
		public decimal Bid { get; set; }

		[JsonProperty("bid_size")]
		public decimal BidSize { get; set; }

		[JsonProperty("ask")]
		public decimal Ask { get; set; }

		[JsonProperty("ask_size")]
		public decimal AskSize { get; set; }

		[JsonProperty("time")]
		[JsonConverter(typeof(JsonDateTimeFmtConverter))]
		public DateTime Time { get; set; }
	}
}