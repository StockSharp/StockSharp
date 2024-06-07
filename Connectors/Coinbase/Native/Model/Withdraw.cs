namespace StockSharp.Coinbase.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Withdraw
{
	[JsonProperty("id")]
	public string Id { get; set; }
}