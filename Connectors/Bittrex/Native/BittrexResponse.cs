namespace StockSharp.Bittrex.Native;

class BittrexResponse
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; }

	[JsonProperty("result")]
	public JToken Result { get; set; }
}