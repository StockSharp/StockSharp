namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Order
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("symbol")]
	public string Symbol { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("user_id")]
	public long UserId { get; set; }

	[JsonProperty("buyer_user_id")]
	public long BuyerUserId { get; set; }

	[JsonProperty("execution_type")]
	public int ExecutionType { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("volume")]
	public double Volume { get; set; }

	[JsonProperty("created_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime? CreatedTimestamp { get; set; }

	[JsonProperty("closed_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime? ClosedTimestamp { get; set; }

	[JsonProperty("relation")]
	public int Relation { get; set; }
}