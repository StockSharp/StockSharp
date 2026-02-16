namespace StockSharp.PrizmBit.Native.Model;

class Balance
{
	[JsonProperty("accountId")]
	public long AccountId { get; set; }

	[JsonProperty("currencyId")]
	public int CurrencyId { get; set; }

	[JsonProperty("currencyName")]
	public string CurrencyName { get; set; }

	[JsonProperty("currencyTitle")]
	public string CurrencyTitle { get; set; }

	[JsonProperty("availableBalance")]
	public double? Available { get; set; }

	[JsonProperty("frozenBalance")]
	public double? Frozen { get; set; }
}