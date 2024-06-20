namespace StockSharp.Coinbase.Native.Model;

class Order
{
	[JsonProperty("order_id")]
	public string Id { get; set; }

	[JsonProperty("client_order_id")]
	public string ClientOrderId { get; set; }

	[JsonProperty("product_id")]
	public string Product { get; set; }

	[JsonProperty("limit_price")]
	public double? Price { get; set; }

	[JsonProperty("stop_price")]
	public double? StopPrice { get; set; }

	[JsonProperty("size")]
	public double? Size { get; set; }

	[JsonProperty("order_side")]
	public string Side { get; set; }

	[JsonProperty("order_type")]
	public string Type { get; set; }

	[JsonProperty("time_in_force")]
	public string TimeInForce { get; set; }

	[JsonProperty("creation_time")]
	public DateTime CreationTime { get; set; }

	[JsonProperty("total_fees")]
	public double? TotalFees { get; set; }

	[JsonProperty("cumulative_quantity")]
	public double? CumulativeQuantity { get; set; }

	[JsonProperty("leaves_quantity")]
	public double? LeavesQuantity { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("avg_price")]
	public double? AvgPrice { get; set; }
}