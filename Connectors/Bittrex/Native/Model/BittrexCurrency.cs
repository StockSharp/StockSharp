namespace StockSharp.Bittrex.Native.Model;

class BittrexCurrency
{
	[JsonProperty("Currency")]
	public string Currency { get; set; }

	[JsonProperty("CurrencyLong")]
	public string CurrencyLong { get; set; }

	[JsonProperty("MinConfirmation")]
	public int MinConfirmation { get; set; }

	[JsonProperty("TxFee")]
	public double TxFee { get; set; }

	[JsonProperty("IsActive")]
	public bool IsActive { get; set; }

	[JsonProperty("IsRestricted")]
	public bool IsRestricted { get; set; }

	[JsonProperty("CoinType")]
	public string CoinType { get; set; }

	[JsonProperty("BaseAddress")]
	public string BaseAddress { get; set; }

	[JsonProperty("Notice")]
	public object Notice { get; set; }
}