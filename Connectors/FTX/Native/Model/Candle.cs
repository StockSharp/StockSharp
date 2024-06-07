namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Candle
{
	[JsonProperty("close")]
	public decimal ClosePrice { get; set; }

	[JsonProperty("high")]
	public decimal HightPrice { get; set; }

	[JsonProperty("low")]
	public decimal LowPrice { get; set; }

	[JsonProperty("open")]
	public decimal OpenPrice { get; set; }

	[JsonProperty("startTime")]
	public DateTime OpenTime { get; set; }

	[JsonProperty("volume")]
	public decimal WindowVolume { get; set; }
}