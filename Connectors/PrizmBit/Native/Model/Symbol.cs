namespace StockSharp.PrizmBit.Native.Model;

class Symbol
{
	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("code")]
	public string Code { get; set; }

	[JsonProperty("isInUse")]
	public bool IsInUse { get; set; }

	[JsonProperty("enabled")]
	public bool Enabled { get; set; }

	[JsonProperty("maxAmount")]
	public double MaxAmount { get; set; }

	[JsonProperty("rate")]
	public double Rate { get; set; }

	[JsonProperty("codeBaseCurrency")]
	public string BaseCoinCode { get; set; }

	[JsonProperty("codeQuoteCurrency")]
	public string QuoteCoinCode { get; set; }

	[JsonProperty("price")]
	public SymbolPrice Price { get; set; }

	[JsonProperty("amount")]
	public SymbolPrice Amount { get; set; }
}

class SymbolPrice
{
	[JsonProperty("decimals")]
	public int Decimals { get; set; }

	[JsonProperty("step")]
	public double Step { get; set; }

	[JsonProperty("maximum")]
	public double Maximum { get; set; }

	[JsonProperty("minimum")]
	public double Minimum { get; set; }
}