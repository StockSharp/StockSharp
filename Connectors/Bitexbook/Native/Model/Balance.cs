namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Balance
{
	[JsonProperty("asset")]
	public string Asset { get; set; }

	[JsonProperty("borrow")]
	public double? Borrow { get; set; }

	[JsonProperty("free")]
	public double? Free { get; set; }

	[JsonProperty("freezed")]
	public double? Freezed { get; set; }

	[JsonProperty("union_fund")]
	public double? UnionFund { get; set; }
}