namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	internal class TradeData
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("units")]
		public int Units { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("takeProfit")]
		public double TakeProfit { get; set; }

		[JsonProperty("stopLoss")]
		public double StopLoss { get; set; }

		[JsonProperty("trailingStop")]
		public int TrailingStop { get; set; }

		[JsonProperty("trailingAmount")]
		public double TrailingAmount { get; set; }
	}
}