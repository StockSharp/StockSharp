namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Order
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("units")]
		public int Units { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("stopLoss")]
		public double? StopLoss { get; set; }

		[JsonProperty("takeProfit")]
		public double? TakeProfit { get; set; }

		[JsonProperty("expiry")]
		public long? Expiry { get; set; }

		[JsonProperty("upperBound")]
		public double? UpperBound { get; set; }

		[JsonProperty("lowerBound")]
		public double? LowerBound { get; set; }

		[JsonProperty("trailingStop")]
		public int? TrailingStop { get; set; }
	}
}