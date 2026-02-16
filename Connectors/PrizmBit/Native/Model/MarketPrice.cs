namespace StockSharp.PrizmBit.Native.Model;

class MarketPrice : BaseEvent
{
	[JsonProperty("price")]
	public double Price { get; set; }
}