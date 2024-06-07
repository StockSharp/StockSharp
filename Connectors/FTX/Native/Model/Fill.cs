namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Fill
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("market")]
	public string Market { get; set; }

	[JsonProperty("orderId")]
	public long OrderId { get; set; }

	[JsonProperty("tradeId")]
	public long TradeId { get; set; }

	[JsonProperty("price")]
	public decimal? Price { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("size")]
	public decimal? Size { get; set; }

	[JsonProperty("time")]
	public DateTime Time { get; set; }
}