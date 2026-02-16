namespace StockSharp.Bibox.Native.Model;

[JsonConverter(typeof(JArrayToObjectConverter<Ohlc>))]
class Ohlc
{
	public long Time { get; set; }

	public double Open { get; set; }
	public double High { get; set; }
	public double Low { get; set; }
	public double Close { get; set; }
	public double Volume { get; set; }
	public double Value { get; set; }

	public long FirstTradeId { get; set; }
	public int TradeCount { get; set; }
}