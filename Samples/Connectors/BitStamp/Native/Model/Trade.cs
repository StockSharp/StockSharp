namespace StockSharp.BitStamp.Native.Model;

class Trade
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("microtimestamp")]
	[JsonConverter(typeof(JsonDateTimeMcsConverter))]
	public DateTime Time { get; set; }

	[JsonProperty("buy_order_id")]
	public long BuyOrderId { get; set; }

	[JsonProperty("sell_order_id")]
	public long SellOrderId { get; set; }
}