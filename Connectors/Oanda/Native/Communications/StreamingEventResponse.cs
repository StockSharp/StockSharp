namespace StockSharp.Oanda.Native.Communications
{
	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class StreamingEventResponse
	{
		[JsonProperty("heartbeat")]
		public Heartbeat Heartbeat { get; set; }

		[JsonProperty("transaction")]
		public Transaction Transaction { get; set; }
	}
}