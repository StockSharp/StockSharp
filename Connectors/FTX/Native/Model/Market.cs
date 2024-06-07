namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Market
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }

	[JsonProperty("minProvideSize")]
	public decimal MinProvideSize { get; set; }

	[JsonProperty("priceIncrement")]
	public decimal PriceIncrement { get; set; }

	[JsonProperty("sizeIncrement")]
	public decimal SizeIncrement { get; set; }
}
