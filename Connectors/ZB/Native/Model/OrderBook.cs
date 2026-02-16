namespace StockSharp.ZB.Native.Model;

[JsonConverter(typeof(JArrayToObjectConverter))]
class OrderBookEntry
{
	public double Price { get; set; }
	public double Size { get; set; }
}

class OrderBook
{
	[JsonProperty("bids")]
	public OrderBookEntry[] Bids { get; set; }

	[JsonProperty("asks")]
	public OrderBookEntry[] Asks { get; set; }
}