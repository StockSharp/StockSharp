namespace StockSharp.Bittrex.Native.Model;

class OrderBookEntry
{
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
}

class OrderBook
{
	public string MarketName { get; set; }
	public IEnumerable<OrderBookEntry> Buy { get; set; }
	public IEnumerable<OrderBookEntry> Sell { get; set; }
}

class WsOrderBookEntry
{
	[JsonProperty("TY")]
	public int Type { get; set; }

	[JsonProperty("R")]
	public double Rate { get; set; }

	[JsonProperty("Q")]
	public double Quantity { get; set; }
}

class WsFill
{
	[JsonProperty("FI")]
	public long Id { get; set; }

	[JsonProperty("OT")]
	public string OrderType { get; set; }

	[JsonProperty("R")]
	public double Rate { get; set; }

	[JsonProperty("Q")]
	public double Quantity { get; set; }

	[JsonProperty("T")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Timestamp { get; set; }
}

class WsOrderBook
{
	[JsonProperty("M")]
	public string Market { get; set; }

	[JsonProperty("N")]
	public long Nonce { get; set; }

	[JsonProperty("Z")]
	public IEnumerable<WsOrderBookEntry> Bids { get; set; }

	[JsonProperty("S")]
	public IEnumerable<WsOrderBookEntry> Asks { get; set; }

	[JsonProperty("f")]
	public IList<WsFill> Fills { get; set; }
}