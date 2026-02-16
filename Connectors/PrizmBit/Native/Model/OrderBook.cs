namespace StockSharp.PrizmBit.Native.Model;

class OrderBook : BaseEvent
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("side")]
	public int Side { get; set; }

	[JsonProperty("Count")]
	public int Count { get; set; }

	[JsonProperty("add")]
	public bool? Add { get; set; }
}