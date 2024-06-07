namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Balance
{
	[JsonProperty("coin")]
	public string Coin { get; set; }

	[JsonProperty("total")]
	public decimal Total { get; set; }

	[JsonProperty("free")]
	public decimal Free { get; set; }

	[JsonProperty("usdValue")]
	public decimal UsdValue { get; set; }
}