namespace StockSharp.Bibox.Native.Model;

class Ticker
{
	[JsonProperty("pair")]
	public string Pair { get; set; }

	[JsonProperty("last")]
	public double? Last { get; set; }

	[JsonProperty("last_usd")]
	public double? LastUsd { get; set; }

	[JsonProperty("last_cny")]
	public double? LastCny { get; set; }

	[JsonProperty("high")]
	public double? High { get; set; }

	[JsonProperty("low")]
	public double? Low { get; set; }

	[JsonProperty("buy")]
	public double? Buy { get; set; }

	[JsonProperty("buy_amount")]
	public double? BuyAmount { get; set; }

	[JsonProperty("sell")]
	public double? Sell { get; set; }

	[JsonProperty("sell_amount")]
	public double? SellAmount { get; set; }

	[JsonProperty("vol")]
	public double? Volume { get; set; }

	[JsonProperty("percent")]
	public string Percent { get; set; }

	[JsonProperty("timestamp")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Timestamp { get; set; }

	[JsonProperty("base_last_cny")]
	public double? BaseLastCny { get; set; }
}