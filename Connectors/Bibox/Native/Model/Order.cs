namespace StockSharp.Bibox.Native.Model;

class Order
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("createdAt")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime CreatedAt { get; set; }

	[JsonProperty("account_type")]
	public int AccountType { get; set; }

	[JsonProperty("coin_symbol")]
	public string CoinSymbol { get; set; }

	[JsonProperty("currency_symbol")]
	public string CurrencySymbol { get; set; }

	[JsonProperty("order_side")]
	public int OrderSide { get; set; }

	[JsonProperty("order_type")]
	public int OrderType { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("money")]
	public double? Money { get; set; }

	[JsonProperty("deal_amount")]
	public double? DealAmount { get; set; }

	[JsonProperty("deal_percent")]
	public string DealPercent { get; set; }

	[JsonProperty("unexecuted")]
	public double? Unexecuted { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }
}