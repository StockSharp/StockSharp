namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = false)]
class Trade
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("time")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Time { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }
}