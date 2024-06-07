namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
[JsonConverter(typeof(JArrayToObjectConverter))]
internal class OrderBookEntry
{
	public decimal Price { get; set; }
	public decimal Size { get; set; }
}

internal class OrderBookEntryInternal
{
	public OrderBookEntry Entry { get; set; }
	public bool IsChanged { get; set; }

	public OrderBookEntryInternal(OrderBookEntry entry)
	{
		Entry = entry;
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class OrderBook
{
	[JsonProperty("bids")]
	public OrderBookEntry[] Bids { get; set; }

	[JsonProperty("asks")]
	public OrderBookEntry[] Asks { get; set; }

	[JsonProperty("time")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Time { get; set; }
}