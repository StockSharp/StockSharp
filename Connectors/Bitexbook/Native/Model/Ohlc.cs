namespace StockSharp.Bitexbook.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = false)]
class Ohlc
{
	//[JsonConverter(typeof(JsonDateTimeMlsConverter))]
	public DateTime Time { get; set; }

	public double Open { get; set; }

	public double High { get; set; }

	public double Low { get; set; }

	public double Close { get; set; }

	public double Volume { get; set; }
}