namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class Ticket
{
	[JsonProperty("id")]
	public long Id { get; set; }

	[JsonProperty("start_volume")]
	public double? StartVolume { get; set; }

	[JsonProperty("volume")]
	public double? Volume { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("user_id")]
	public long UserId { get; set; }

	[JsonProperty("created_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime? CreatedTimestamp { get; set; }

	[JsonProperty("modify_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime? ModifyTimestamp { get; set; }

	[JsonProperty("process_timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime? ProcessTimestamp { get; set; }

	[JsonProperty("execution_type")]
	public int ExecutionType { get; set; }

	[JsonProperty("order_volume")]
	public double? OrderVolume { get; set; }

	[JsonProperty("order_id")]
	public string OrderId { get; set; }

	[JsonProperty("symbol")]
	public string Symbol { get; set; }
}