using System.Reflection;
using Newtonsoft.Json;
using System;
namespace SciTrader.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Trade
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("price")]
		public decimal Price { get; set; }

		[JsonProperty("size")]
		public decimal Size { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("time")]
		[JsonConverter(typeof(JsonDateTimeFmtConverter))]
		public DateTime Time { get; set; }
	}
}