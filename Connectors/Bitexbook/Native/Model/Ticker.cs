namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Ticker
{
	[JsonProperty("symbol")]
	public string Symbol { get; set; }

	[JsonProperty("price_open")]
	public double? Open { get; set; }

	[JsonProperty("price_close")]
	public double? Close { get; set; }

	[JsonProperty("price_high")]
	public double? High { get; set; }

	[JsonProperty("price_low")]
	public double? Low { get; set; }

	[JsonProperty("volume")]
	public double? Volume { get; set; }

	[JsonProperty("unix_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }
}