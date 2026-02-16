namespace StockSharp.ZB.Native.Model;

class Balance
{
	[JsonProperty("freez")]
	public double? Freez { get; set; }

	[JsonProperty("enName")]
	public string EnName { get; set; }

	[JsonProperty("unitDecimal")]
	public int UnitDecimal { get; set; }

	[JsonProperty("cnName")]
	public string CnName { get; set; }

	[JsonProperty("unitTag")]
	public string UnitTag { get; set; }

	[JsonProperty("available")]
	public double? Available { get; set; }

	[JsonProperty("key")]
	public string Key { get; set; }
}