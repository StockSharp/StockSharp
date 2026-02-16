namespace StockSharp.PrizmBit.Native.Model;

class OrderMatching
{
	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	[JsonProperty("tradeId")]
	public long TradeId { get; set; }
}

class Order
{
	[JsonProperty("orderMatchingList")]
	public OwnTrade[] OrderMatchingList { get; set; }

	[JsonProperty("orderMatchingResult")]
	public OrderMatching OrderMatchingResult { get; set; }

	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("cliOrdId")]
	public string CliOrdId { get; set; }

	[JsonProperty("userAccountId")]
	public long UserAccountId { get; set; }

	[JsonProperty("userId")]
	public long UserId { get; set; }

	[JsonProperty("userAccountType")]
	public string UserAccountType { get; set; }

	[JsonProperty("marketId")]
	public int MarketId { get; set; }

	[JsonProperty("side")]
	public string Side { get; set; }

	[JsonProperty("orderQty")]
	public double OrderQty { get; set; }

	[JsonProperty("leavesQty")]
	public double LeavesQty { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("trailingStopDistance")]
	public double? TrailingStopDistance { get; set; }

	[JsonProperty("stopPrice")]
	public double? StopPrice { get; set; }

	[JsonProperty("limitPrice")]
	public double? LimitPrice { get; set; }

	[JsonProperty("takeProfit")]
	public double? TakeProfit { get; set; }

	[JsonProperty("orderStatus")]
	public string OrderStatus { get; set; }

	[JsonProperty("orderType")]
	public string OrderType { get; set; }

	[JsonProperty("averagePrice")]
	public double? AveragePrice { get; set; }

	[JsonProperty("totalPrice")]
	public double? TotalPrice { get; set; }

	[JsonProperty("filled")]
	public double Filled { get; set; }

	[JsonProperty("cancelDate")]
	public DateTime? CancelDate { get; set; }

	[JsonProperty("dateCreated")]
	public DateTime DateCreated { get; set; }
}