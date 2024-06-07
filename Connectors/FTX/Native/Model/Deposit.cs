namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Deposit
{
	[JsonProperty("coin")]
	public string Coin { get; set; }

	[JsonProperty("id")]
	public decimal? Price { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("size")]
	public decimal? Size { get; set; }

	[JsonProperty("time")]
	public DateTime Time { get; set; }

	[JsonProperty("sentTime")]
	public DateTime SentTime { get; set; }

	[JsonProperty("confirmedTime")]
	public DateTime ConfirmedTime { get; set; }
}