namespace StockSharp.BitStamp.Native.Model;

class UserOrder
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("datetime")]
	public DateTime Time { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("currency_pair")]
	public string CurrencyPair { get; set; }
}