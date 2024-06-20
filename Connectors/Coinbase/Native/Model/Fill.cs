namespace StockSharp.Coinbase.Native.Model;

class Fill
{
	[JsonProperty("trade_id")]
	public long TradeId { get; set; }

	[JsonProperty("product_id")]
	public string Product { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("size")]
	public double Size { get; set; }

	[JsonProperty("order_id")]
	public string OrderId { get; set; }

	[JsonProperty("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonProperty("liquidity")]
	public string Liquidity { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	[JsonProperty("settled")]
	public bool Settled { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }
}