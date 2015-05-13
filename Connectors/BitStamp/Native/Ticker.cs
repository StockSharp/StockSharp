namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Net;

	using Newtonsoft.Json;

	class Ticker
	{
		public double Bid { get; set; }

		public double Ask { get; set; }

		public double Last { get; set; }

		public double High { get; set; }

		public double Low { get; set; }

		public double Volume { get; set; }

		public double VWAP { get; set; }

		[JsonProperty(PropertyName = "timestamp")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		public override string ToString()
		{
			return string.Format("[Ticker: Bid={0}, Ask={1}, Last={2}, High={3}, Low={4}, Volume={5}]", Bid, Ask, Last, High, Low, Volume);
		}
	}
}