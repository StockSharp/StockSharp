namespace StockSharp.Bitalong.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Symbol
{
	[JsonProperty("decimal_places")]
	public int DecimalPlaces { get; set; }

	[JsonProperty("min_amount")]
	public double MinAmount { get; set; }

	[JsonProperty("fee_buy")]
	public double FeeBuy { get; set; }

	[JsonProperty("fee_sell")]
	public double FeeSell { get; set; }
}