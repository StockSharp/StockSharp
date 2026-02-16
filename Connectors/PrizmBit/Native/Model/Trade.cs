namespace StockSharp.PrizmBit.Native.Model;

class Trade
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("marketPrice")]
	public double Price { get; set; }

	[JsonProperty("quantity")]
	public double Amount { get; set; }

	[JsonProperty("dateCreated")]
	public DateTime Time { get; set; }

	[JsonProperty("side")]
	public string Type { get; set; }
}

class SocketTrade : BaseEvent
{
	[JsonProperty("tradeId")]
	public long TradeId { get; set; }

	[JsonProperty("orderId")]
	public long OrderId { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("quantity")]
	public double Amount { get; set; }

	[JsonProperty("date")]
	public DateTime Time { get; set; }

	[JsonProperty("side")]
	public int Side { get; set; }
}

class SocketUserTrade : SocketTrade
{
}

class HttpUserTrade
{
	[JsonProperty("tradeId")]
	public long TradeId { get; set; }

	[JsonProperty("orderId")]
	public long OrderId { get; set; }

	[JsonProperty("cliOrdId")]
	public string CliOrdId { get; set; }

	[JsonProperty("marketId")]
	public int MarketId { get; set; }

	[JsonProperty("marketName")]
	public string MarketName { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	[JsonProperty("feeCurrency")]
	public string FeeCurrency { get; set; }

	[JsonProperty("dateCreated")]
	public DateTime Time { get; set; }

	[JsonProperty("orderIdList")]
	public long[] OrderIdList { get; set; }

	[JsonProperty("onlyMyOrders")]
	public bool OnlyMyOrders { get; set; }
}