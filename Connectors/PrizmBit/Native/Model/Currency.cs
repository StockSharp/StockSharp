namespace StockSharp.PrizmBit.Native.Model;

class HttpCurrencyFee
{
	[JsonProperty("rate")]
	public double Rate { get; set; }

	[JsonProperty("minimum")]
	public double Minimum { get; set; }

	[JsonProperty("maximum")]
	public double Maximum { get; set; }

	[JsonProperty("amount")]
	public double Amount { get; set; }
}

class HttpCurrencyCondition
{
	[JsonProperty("enabled")]
	public bool Enabled { get; set; }

	[JsonProperty("extended")]
	public bool Extended { get; set; }

	[JsonProperty("step")]
	public double? Step { get; set; }

	[JsonProperty("confirmations")]
	public int? Confirmations { get; set; }

	[JsonProperty("minimum")]
	public double Minimum { get; set; }

	[JsonProperty("maximum")]
	public double Maximum { get; set; }

	[JsonProperty("fee")]
	public HttpCurrencyFee Fee { get; set; }
}

class HttpCurrency
{
	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("decimals")]
	public int Decimals { get; set; }

	[JsonProperty("code")]
	public string Code { get; set; }

	[JsonProperty("long")]
	public string Long { get; set; }

	[JsonProperty("isCrypto")]
	public bool IsCrypto { get; set; }

	[JsonProperty("inUse")]
	public bool InUse { get; set; }

	[JsonProperty("token")]
	public object Token { get; set; }

	[JsonProperty("deposit")]
	public HttpCurrencyCondition Deposit { get; set; }

	[JsonProperty("withdraw")]
	public HttpCurrencyCondition Withdraw { get; set; }
}