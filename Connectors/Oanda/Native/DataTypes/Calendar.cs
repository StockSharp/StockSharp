namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Calendar
	{
		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("timeStamp")]
		public long TimeStamp { get; set; }

		[JsonProperty("unit")]
		public string Unit { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("forecast")]
		public double Forecast { get; set; }

		[JsonProperty("previous")]
		public double Previous { get; set; }

		[JsonProperty("actual")]
		public double Actual { get; set; }

		[JsonProperty("market")]
		public double Market { get; set; }
	}
}