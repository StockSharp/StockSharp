namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Position
	{
		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("units")]
		public int Units { get; set; }

		[JsonProperty("avgPrice")]
		public double AveragePrice { get; set; }
	}
}