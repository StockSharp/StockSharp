namespace StockSharp.Coinbase.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class OrderLog
{
	[JsonProperty("time")]
	public DateTime Time { get; set; }

	[JsonProperty("product_id")]
	public string Product { get; set; }

	[JsonProperty("sequence")]
	public long Sequence { get; set; }

	[JsonProperty("order_id")]
	public string OrderId { get; set; }

	[JsonProperty("price")]
	public decimal? Price { get; set; }

	[JsonProperty("size")]
	public decimal Size { get; set; }

	[JsonProperty("remaining_size")]
	public decimal RemainingSize { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("order_type")]
	public string OrderType { get; set; }

	[JsonProperty("funds")]
	public decimal? Funds { get; set; }

	[JsonProperty("reason")]
	public string Reason { get; set; }
}