namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Heartbeat
	{
		[JsonProperty("time")]
		public long Time { get; set; }
	}
}