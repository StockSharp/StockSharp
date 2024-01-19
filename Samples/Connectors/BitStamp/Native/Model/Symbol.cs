namespace StockSharp.BitStamp.Native.Model;

class Symbol
{
	[JsonProperty("base_decimals")]
	public int BaseDecimals { get; set; }

	[JsonProperty("minimum_order")]
	public string MinimumOrder { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("counter_decimals")]
	public int CounterDecimals { get; set; }

	[JsonProperty("trading")]
	public string Trading { get; set; }

	[JsonProperty("url_symbol")]
	public string UrlSymbol { get; set; }

	[JsonProperty("description")]
	public string Description { get; set; }
}