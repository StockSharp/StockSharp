namespace StockSharp.DarkHorse.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Account
{
	[JsonProperty("username")]
	public string UserName { get; set; }

	[JsonProperty("totalAccountValue")]
	public string TotalAccountValue { get; set; }

    [JsonProperty("accountCode")]
    public string AccountCode { get; set; }

    [JsonProperty("accountName")]
    public string AccountName { get; set; }
}