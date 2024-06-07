namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Symbol
{
	[JsonProperty("symbol")]
	public string Code { get; set; }

	[JsonProperty("alias")]
	public string Alias { get; set; }

	[JsonProperty("min_amount")]
	public double? MinAmount { get; set; }

	[JsonProperty("min_price")]
	public double? MinPrice { get; set; }

	[JsonProperty("max_price")]
	public double? MaxPrice { get; set; }

	[JsonProperty("min_total")]
	public double? MinTotal { get; set; }

	[JsonProperty("currency_base")]
	public string CurrencyBase { get; set; }

	[JsonProperty("currency_base_name")]
	public string CurrencyBaseName { get; set; }

	[JsonProperty("currency_quoted")]
	public string CurrencyQuoted { get; set; }

	[JsonProperty("currency_quoted_name")]
	public string CurrencyQuotedName { get; set; }
}