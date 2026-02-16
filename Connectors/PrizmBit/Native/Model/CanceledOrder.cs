namespace StockSharp.PrizmBit.Native.Model;

class CanceledOrder : BaseEvent
{
	[JsonProperty("orderId")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("orderType")]
	public int OrderType { get; set; }

	[JsonProperty("side")]
	public int Side { get; set; }
}

class UserCanceledOrder : CanceledOrder
{
	[JsonProperty("cliOrdId")]
	public string CliOrdId { get; set; }
}