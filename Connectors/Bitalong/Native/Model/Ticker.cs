namespace StockSharp.Bitalong.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Ticker
{
	[JsonProperty("last")]
	public double? Last { get; set; }

	[JsonProperty("lowestAsk")]
	public double? LowestAsk { get; set; }

	[JsonProperty("highestBid")]
	public double? HighestBid { get; set; }

	[JsonProperty("percentChange")]
	public double? PercentChange { get; set; }

	[JsonProperty("baseVolume")]
	public double? BaseVolume { get; set; }

	[JsonProperty("quoteVolume")]
	public double? QuoteVolume { get; set; }

	[JsonProperty("high24hr")]
	public double? High24 { get; set; }

	[JsonProperty("low24hr")]
	public double? Low24 { get; set; }
}