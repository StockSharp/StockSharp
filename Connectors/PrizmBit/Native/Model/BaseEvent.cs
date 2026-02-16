namespace StockSharp.PrizmBit.Native.Model;

class BaseEvent
{
	[JsonProperty("marketId")]
	public int MarketId { get; set; }
}