namespace StockSharp.Bittrex.Native.Model;

class Order
{
	//public string AccountId { get; set; }
	public string OrderUuid { get; set; }
	public string Exchange { get; set; }
	public string Type { get; set; }
	public string OrderType { get; set; }
	public decimal Quantity { get; set; }
	public decimal QuantityRemaining { get; set; }
	public decimal? Limit { get; set; }
	public decimal? Reserved { get; set; }
	public decimal? ReservedRemaining { get; set; }
	public decimal? CommissionReserved { get; set; }
	public decimal? CommissionReservedRemaining { get; set; }
	public decimal? CommissionPaid { get; set; }
	public decimal Price { get; set; }
	public decimal? PricePerUnit { get; set; }
	public DateTime Opened { get; set; }
	public DateTime? Closed { get; set; }
	public bool IsOpen { get; set; }
	public string Sentinel { get; set; }
	public bool CancelInitiated { get; set; }
	public bool ImmediateOrCancel { get; set; }
	public bool IsConditional { get; set; }
	public string Condition { get; set; }
	public string ConditionTarget { get; set; }
}

class WsOrder
{
	[JsonProperty("U")]
	public string Uuid { get; set; }

	[JsonProperty("I")]
	public long Id { get; set; }

	[JsonProperty("OU")]
	public string OrderUuid { get; set; }

	[JsonProperty("E")]
	public string Exchange { get; set; }

	[JsonProperty("OT")]
	public string OrderType { get; set; }

	[JsonProperty("Q")]
	public decimal Quantity { get; set; }

	[JsonProperty("q")]
	public decimal QuantityRemaining { get; set; }

	[JsonProperty("X")]
	public decimal? Limit { get; set; }

	public decimal? CommissionPaid { get; set; }

	[JsonProperty("P")]
	public decimal? Price { get; set; }

	[JsonProperty("PU")]
	public decimal? PricePerUnit { get; set; }

	[JsonProperty("Y")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Opened { get; set; }

	[JsonProperty("C")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime? Closed { get; set; }

	[JsonProperty("i")]
	public bool? IsOpen { get; set; }

	[JsonProperty("CI")]
	public bool? CancelInitiated { get; set; }

	[JsonProperty("J")]
	public string Condition { get; set; }

	[JsonProperty("j")]
	public string ConditionTarget { get; set; }

	[JsonProperty("u")]
	[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime? Updated { get; set; }
}