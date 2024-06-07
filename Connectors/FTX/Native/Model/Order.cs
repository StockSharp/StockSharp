namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Order
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("market")]
	public string Market { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("price")]
	public decimal? Price { get; set; }

	[JsonProperty("avgFillPrice")]
	public decimal? AvgFillPrice { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("size")]
	public decimal? Size { get; set; }

	[JsonProperty("filledSize")]
	public decimal? FilledSize { get; set; }

	[JsonProperty("createdAt")]
	public DateTime? CreatedAt { get; set; }

	[JsonProperty("clientId")]
	public string ClientId { get; set; }
}