namespace StockSharp.Bibox.Native.Model;

class Balance
{
	[JsonProperty("coin_symbol")]
	public string CoinSymbol { get; set; }

	[JsonProperty("balance")]
	public double? Value { get; set; }

	[JsonProperty("freeze")]
	public double? Freeze { get; set; }

	[JsonProperty("BTCValue")]
	public double? BtcValue { get; set; }

	[JsonProperty("CNYValue")]
	public double? CnyValue { get; set; }

	[JsonProperty("USDValue")]
	public double? UsdValue { get; set; }
}