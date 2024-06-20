namespace StockSharp.Coinbase.Native.Model;

class OrderBookChange
{
	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("price_level")]
	public double Price { get; set; }

	[JsonProperty("new_quantity")]
	public double Size { get; set; }
}