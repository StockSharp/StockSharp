namespace StockSharp.Coinbase.Native.Model;

class Ticker
{
	[JsonProperty("trade_id")]
	public long? LastTradeId { get; set; }

	[JsonProperty("sequence")]
	public long Sequence { get; set; }

	[JsonProperty("product_id")]
	public string Product { get; set; }

	[JsonProperty("price")]
	public double? LastTradePrice { get; set; }

	[JsonProperty("side")]
	public string LastTradeSide { get; set; }

	[JsonProperty("last_size")]
	public double? LastTradeSize { get; set; }

	[JsonProperty("best_bid")]
	public double? Bid { get; set; }

	[JsonProperty("best_ask")]
	public double? Ask { get; set; }

	[JsonProperty("best_bid_quantity")]
	public double? BidSize { get; set; }

	[JsonProperty("best_ask_quantity")]
	public double? AskSize { get; set; }

	[JsonProperty("volume_24h")]
	public double? Volume { get; set; }

	[JsonProperty("low_24h")]
	public double? Low { get; set; }

	[JsonProperty("high_24h")]
	public double? High { get; set; }

	[JsonProperty("price_percent_chg_24_h")]
	public double? Change { get; set; }
}