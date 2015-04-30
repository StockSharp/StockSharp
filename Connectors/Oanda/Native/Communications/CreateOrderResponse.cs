namespace StockSharp.Oanda.Native.Communications
{
	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class CreateOrderResponse
	{
		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("tradeOpened")]
		public TradeData TradeOpened { get; set; }

		[JsonProperty("tradeReduced")]
		public TradeData TradeReduced { get; set; }

		[JsonProperty("orderOpened")]
		public Order OrderOpened { get; set; }
	}
}