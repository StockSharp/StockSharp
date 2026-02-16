namespace StockSharp.Bittrex.Native.Model;

class Candle
{
	[JsonProperty("BV")]
	public double BaseVolume { get; set; }

	[JsonProperty("C")]
	public double Close { get; set; }

	[JsonProperty("H")]
	public double High { get; set; }

	[JsonProperty("L")]
	public double Low { get; set; }

	[JsonProperty("O")]
	public double Open { get; set; }

	[JsonProperty("T")]
	public DateTime Timestamp { get; set; }

	[JsonProperty("V")]
	public double Volume24H { get; set; }
}