namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Futures
{
	[JsonProperty("cost")]
	public decimal? Cost { get; set; }

	[JsonProperty("entryPrice")]
	public decimal? EntryPrice { get; set; }

	[JsonProperty("future")]
	public string Name { get; set; }

	[JsonProperty("initialMarginRequirement")]
	public decimal? InitialMarginRequirement { get; set; }

	[JsonProperty("netSize")]
	public decimal? Size { get; set; }

	[JsonProperty("unrealizedPnl")]
	public decimal? UnrealizedPnl { get; set; }
}
