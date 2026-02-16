namespace StockSharp.Bibox.Native.Model;

class Trade
{
	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("pair")]
	public string Pair { get; set; }

	[JsonProperty("price")]
	public double Price { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }

	[JsonProperty("time")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Time { get; set; }

	[JsonProperty("side")]
	public int Side { get; set; }
}