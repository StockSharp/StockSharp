namespace StockSharp.Coinbase.Native.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = false)]
	[JsonConverter(typeof(JArrayToObjectConverter))]
	class Ohlc
	{
		//[JsonConverter(typeof(JsonDateTimeMlsConverter))]
		public long Time { get; set; }

		public decimal Low { get; set; }

		public decimal High { get; set; }

		public decimal Open { get; set; }

		public decimal Close { get; set; }

		public decimal Volume { get; set; }
	}
}