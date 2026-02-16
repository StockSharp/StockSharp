namespace StockSharp.PrizmBit.Native.Model;

class OwnTrade
{
	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	[JsonProperty("totalPrice")]
	public double TotalPrice { get; set; }

	[JsonProperty("tradeId")]
	public long TradeId { get; set; }
}

class SocketOwnTrade : SocketTrade
{
	[JsonProperty("orderStatus")]
	public int OrderStatus { get; set; }
}