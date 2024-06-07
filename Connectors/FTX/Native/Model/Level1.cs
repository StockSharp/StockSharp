namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Level1
{
	[JsonProperty("bid")]
	public decimal Bid { get; set; }

	[JsonProperty("bidSize")]
	public decimal BidSize { get; set; }

	[JsonProperty("ask")]
	public decimal Ask { get; set; }

	[JsonProperty("askSize")]
	public decimal AskSize { get; set; }

	[JsonProperty("time")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Time { get; set; }
}