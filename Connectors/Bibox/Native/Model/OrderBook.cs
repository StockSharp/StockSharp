namespace StockSharp.Bibox.Native.Model;

class OrderBookEntry
{
	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("volume")]
	public double Size { get; set; }
}

class OrderBook
{
	[JsonProperty("bids")]
	public OrderBookEntry[] Bids { get; set; }

	[JsonProperty("asks")]
	public OrderBookEntry[] Asks { get; set; }

	[JsonProperty("update_time")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Timestamp { get; set; }
}