namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Candle
	{
		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("openMid")]
		public double Open { get; set; }

		[JsonProperty("highMid")]
		public double High { get; set; }

		[JsonProperty("lowMid")]
		public double Low { get; set; }

		[JsonProperty("closeMid")]
		public double Close { get; set; }

		[JsonProperty("volume")]
		public double Volume { get; set; }

		[JsonProperty("complete")]
		public bool Complete { get; set; }
	}
}