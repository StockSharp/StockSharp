namespace StockSharp.Bitalong.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = false)]
class Trade
{
	[JsonProperty("tradeID")]
	public long Id { get; set; }

	[JsonProperty("date")]
	public DateTime Timestamp { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }

	[JsonProperty("rate")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("total")]
	public double Total { get; set; }
}