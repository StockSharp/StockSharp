namespace StockSharp.Oanda.Native.Communications
{
	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class StreamingPriceResponse
	{
		[JsonProperty("heartbeat")]
		public Heartbeat Heartbeat { get; set; }

		[JsonProperty("tick")]
		public Price Tick { get; set; }
	}
}