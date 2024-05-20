namespace StockSharp.Bitexbook.Native.Model
{
	using System.Reflection;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class TickerChange
	{
		[JsonProperty("s")]
		public string Symbol { get; set; }

		[JsonProperty("b")]
		public double? Bid { get; set; }

		[JsonProperty("a")]
		public double? Ask { get; set; }
	}
}