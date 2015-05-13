namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Price
    {
		[JsonProperty("instrument")]
        public string Instrument { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("bid")]
        public double Bid { get; set; }

		[JsonProperty("ask")]
        public double Ask { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
    }
}