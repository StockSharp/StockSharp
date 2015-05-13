namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Instrument
    {
		[JsonProperty("instrument")]
		public string Code { get; set; }

		[JsonProperty("displayName")]
        public string DisplayName { get; set; }

		[JsonProperty("pip")]
		public double Pip { get; set; }

		[JsonProperty("maxTradeUnits")]
		public int MaxTradeUnits { get; set; }

		[JsonProperty("halted")]
		public bool Halted { get; set; }
    }
}