namespace StockSharp.CryptoHFTData.Native;

using Parquet.Serialization.Attributes;

sealed class SymbolsResponse
{
	[JsonPropertyName("symbols")]
	public string[] Symbols { get; set; } = [];
}

sealed class TradeRow
{
	[JsonPropertyName("received_time")]
	public long ReceivedTime { get; set; }

	[JsonPropertyName("event_time")]
	public long EventTime { get; set; }

	[JsonPropertyName("symbol")]
	[ParquetRequired]
	public string Symbol { get; set; }

	[JsonPropertyName("trade_id")]
	public long TradeId { get; set; }

	[JsonPropertyName("price")]
	[ParquetRequired]
	public string Price { get; set; }

	[JsonPropertyName("quantity")]
	[ParquetRequired]
	public string Quantity { get; set; }

	[JsonPropertyName("trade_time")]
	public long TradeTime { get; set; }

	[JsonPropertyName("is_buyer_maker")]
	public bool IsBuyerMaker { get; set; }
}

sealed class OrderBookRow
{
	[JsonPropertyName("received_time")]
	public long ReceivedTime { get; set; }

	[JsonPropertyName("event_time")]
	public long EventTime { get; set; }

	[JsonPropertyName("symbol")]
	[ParquetRequired]
	public string Symbol { get; set; }

	[JsonPropertyName("event_type")]
	[ParquetRequired]
	public string EventType { get; set; }

	[JsonPropertyName("first_update_id")]
	public long? FirstUpdateId { get; set; }

	[JsonPropertyName("final_update_id")]
	public long? FinalUpdateId { get; set; }

	[JsonPropertyName("last_update_id")]
	public long? LastUpdateId { get; set; }

	[JsonPropertyName("side")]
	[ParquetRequired]
	public string Side { get; set; }

	[JsonPropertyName("price")]
	[ParquetRequired]
	public string Price { get; set; }

	[JsonPropertyName("quantity")]
	[ParquetRequired]
	public string Quantity { get; set; }

	[JsonIgnore]
	public long SequenceNumber => FinalUpdateId ?? LastUpdateId ?? 0;
}
