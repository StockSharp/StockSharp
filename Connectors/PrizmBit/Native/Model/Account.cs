namespace StockSharp.PrizmBit.Native.Model;

class Account
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("accountType")]
	public int Type { get; set; }

	[JsonProperty("balanceList")]
	public Balance[] Balances { get; set; }

	[JsonProperty("suspended")]
	public bool Suspended { get; set; }

	[JsonProperty("takerFee")]
	public double? TakerFee { get; set; }

	[JsonProperty("makerFee")]
	public double? MakerFee { get; set; }

	[JsonProperty("takerReward")]
	public double? TakerReward { get; set; }

	[JsonProperty("makerReward")]
	public double? MakerReward { get; set; }
}