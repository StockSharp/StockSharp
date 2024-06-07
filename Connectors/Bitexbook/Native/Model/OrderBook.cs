namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
[JsonConverter(typeof(JArrayToObjectConverter))]
class OrderBookEntry
{
	public double Price { get; set; }
	public double Size { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class OrderBook
{
	[JsonProperty("bids")]
	public OrderBookEntry[] Bids { get; set; }

	[JsonProperty("asks")]
	public OrderBookEntry[] Asks { get; set; }

	[JsonProperty("timestamp")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Timestamp { get; set; }
}