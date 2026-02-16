namespace StockSharp.ZB.Native.Model;

class Order
{
	[JsonProperty("currency")]
	public string Currency { get; set; }

	[JsonProperty("fees")]
	public double? Fees { get; set; }

	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("total_amount")]
	public double? TotalAmount { get; set; }

	[JsonProperty("trade_amount")]
	public double? TradeAmount { get; set; }

	[JsonProperty("trade_price")]
	public double? TradePrice { get; set; }

	[JsonProperty("trade_date")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime TradeDate { get; set; }

	[JsonProperty("trade_money")]
	public double? TradeMoney { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }
}