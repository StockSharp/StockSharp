namespace StockSharp.Coinbase.Native.Model;

class Trade
{
	[JsonProperty("trade_id")]
	public long TradeId { get; set; }

	[JsonProperty("product_id")]
	public string ProductId { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("size")]
	public double? Size { get; set; }

	[JsonProperty("time")]
	public DateTime Time { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("bid")]
	public double? Bid { get; set; }

	[JsonProperty("ask")]
	public double? Ask { get; set; }
}