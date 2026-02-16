namespace StockSharp.ZB.Native.Model;

class Trade
{
	[JsonProperty("date")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Time { get; set; }

	[JsonProperty("tid")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }

	[JsonProperty("trade_type")]
	public string TradeType { get; set; }
}