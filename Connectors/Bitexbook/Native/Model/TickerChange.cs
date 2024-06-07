namespace StockSharp.Bitexbook.Native.Model;

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