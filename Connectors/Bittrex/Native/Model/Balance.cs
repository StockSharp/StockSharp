namespace StockSharp.Bittrex.Native.Model;

class Balance
{
	public string Currency { get; set; }

	[JsonProperty("Balance")]
	public decimal? Value { get; set; }

	public decimal? Available { get; set; }
	public decimal? Pending { get; set; }
	public string CryptoAddress { get; set; }
	public bool? Requested { get; set; }
	public string Uuid { get; set; }
}

class WsBalance
{
	[JsonProperty("U")]
	public string Uuid { get; set; }

	[JsonProperty("W")]
	public long AccountId { get; set; }

	[JsonProperty("c")]
	public string Currency { get; set; }

	[JsonProperty("b")]
	public decimal? Value { get; set; }

	[JsonProperty("a")]
	public decimal? Available { get; set; }

	[JsonProperty("z")]
	public decimal? Pending { get; set; }

	[JsonProperty("p")]
	public string CryptoAddress { get; set; }

	[JsonProperty("r")]
	public bool? Requested { get; set; }

	[JsonProperty("u")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime? Updated { get; set; }

	[JsonProperty("h")]
	public bool? AutoSell { get; set; }
}