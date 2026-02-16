namespace StockSharp.ZB.Native.Model;

class Ticker
{
	[JsonProperty("vol")]
	public double? Vol { get; set; }

	[JsonProperty("last")]
	public double? Last { get; set; }

	[JsonProperty("sell")]
	public double? Sell { get; set; }

	[JsonProperty("buy")]
	public double? Buy { get; set; }

	[JsonProperty("high")]
	public double? High { get; set; }

	[JsonProperty("low")]
	public double? Low { get; set; }
}