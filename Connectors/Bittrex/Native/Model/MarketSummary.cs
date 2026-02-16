namespace StockSharp.Bittrex.Native.Model;

class MarketSummary
{
	public string MarketName { get; set; }
	public decimal? High { get; set; }
	public decimal? Low { get; set; }
	public decimal? Volume { get; set; }
	public decimal? Last { get; set; }
	public decimal? BaseVolume { get; set; }
	public DateTime Timestamp { get; set; }
	public decimal? Bid { get; set; }
	public decimal? Ask { get; set; }
	public int? OpenBuyOrders { get; set; }
	public int? OpenSellOrders { get; set; }
	public decimal? PrevDay { get; set; }
	public DateTime Created { get; set; }
	public string DisplayMarketName { get; set; }
}

class WsTicker
{
	[JsonProperty("M")]
	public string Market { get; set; }

	[JsonProperty("H")]
	public double High { get; set; }

	[JsonProperty("L")]
	public double Low { get; set; }

	[JsonProperty("V")]
	public double Volume { get; set; }

	[JsonProperty("l")]
	public double Last { get; set; }

	[JsonProperty("m")]
	public double BaseVolume { get; set; }

	[JsonProperty("T")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime TimeStamp { get; set; }

	[JsonProperty("B")]
	public double Bid { get; set; }

	[JsonProperty("A")]
	public double Ask { get; set; }

	[JsonProperty("G")]
	public int OpenBuyOrders { get; set; }

	[JsonProperty("g")]
	public int OpenSellOrders { get; set; }

	[JsonProperty("PD")]
	public double PrevDay { get; set; }

	//[JsonProperty("x")]
	//[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	//public long Created { get; set; }
}

class WsMarketSummary
{
	[JsonProperty("N")]
	public long Nonce { get; set; }

	[JsonProperty("D")]
	public IEnumerable<WsTicker> Tickers { get; set; }
}