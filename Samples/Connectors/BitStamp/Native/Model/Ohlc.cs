namespace StockSharp.BitStamp.Native.Model;

using System;

using Newtonsoft.Json;

using Ecng.Serialization;

class Ohlc
{
	[JsonProperty("timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Time { get; set; }

	[JsonProperty("open")]
	public double Open { get; set; }

	[JsonProperty("high")]
	public double High { get; set; }

	[JsonProperty("low")]
	public double Low { get; set; }

	[JsonProperty("close")]
	public double Close { get; set; }

	[JsonProperty("volume")]
	public double Volume { get; set; }
}
