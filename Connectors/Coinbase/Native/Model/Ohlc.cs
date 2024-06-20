namespace StockSharp.Coinbase.Native.Model;

class Ohlc
{
	[JsonConverter(typeof(JsonDateTimeConverter))]
	[JsonProperty("start")]
	public DateTime Time { get; set; }

	[JsonProperty("low")]
	public double Low { get; set; }

	[JsonProperty("high")]
	public double High { get; set; }

	[JsonProperty("open")]
	public double Open { get; set; }

	[JsonProperty("close")]
	public double Close { get; set; }

	[JsonProperty("volume")]
	public double Volume { get; set; }

	[JsonProperty("product_id")]
	public string Symbol { get; set; }
}