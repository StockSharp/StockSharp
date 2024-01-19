namespace StockSharp.BitStamp.Native.Model;

class UserTransaction
{
	[JsonProperty("datetime")]
	public string Time { get; set; }

	[JsonProperty("id")]
	public long Id { get; set; }

	// Transaction type: 0 - deposit; 1 - withdrawal; 2 - market trade; 14 - sub account transfer.
	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("usd")]
	public double? UsdAmount { get; set; }

	[JsonProperty("eur")]
	public double? EurAmount { get; set; }

	[JsonProperty("btc")]
	public double? BtcAmount { get; set; }

	[JsonProperty("xrp")]
	public double? XrpAmount { get; set; }

	[JsonProperty("eth")]
	public double? EthAmount { get; set; }

	[JsonProperty("ltc")]
	public double? LtcAmount { get; set; }

	[JsonProperty("bch")]
	public double? BchAmount { get; set; }

	[JsonProperty("btc_usd")]
	public double? BtcUsd { get; set; }

	[JsonProperty("btc_eur")]
	public double? BtcEur { get; set; }

	[JsonProperty("eur_usd")]
	public double? EurUsd { get; set; }

	[JsonProperty("xrp_usd")]
	public double? XrpUsd { get; set; }

	[JsonProperty("xrp_eur")]
	public double? XrpEur { get; set; }

	[JsonProperty("xrp_btc")]
	public double? XrpBtc { get; set; }

	[JsonProperty("ltc_usd")]
	public double? LtcUsd { get; set; }

	[JsonProperty("ltc_eur")]
	public double? LtcEur { get; set; }

	[JsonProperty("ltc_btc")]
	public double? LtcBtc { get; set; }

	[JsonProperty("eth_usd")]
	public double? EthUsd { get; set; }

	[JsonProperty("eth_eur")]
	public double? EthEur { get; set; }

	[JsonProperty("eth_btc")]
	public double? EthBtc { get; set; }

	[JsonProperty("bch_usd")]
	public double? BchUsd { get; set; }

	[JsonProperty("bch_eur")]
	public double? BchEur { get; set; }

	[JsonProperty("bch_btc")]
	public double? BchBtc { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	[JsonProperty("order_id")]
	public long OrderId { get; set; }
}