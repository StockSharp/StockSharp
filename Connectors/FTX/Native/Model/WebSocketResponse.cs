namespace StockSharp.FTX.Native.Model
{
	using System.Reflection;
	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class WebSocketResponse<T> where T : class
	{
		[JsonProperty("channel")]
		public string Channel { get; set; }
		[JsonProperty("market")]
		public string Market { get; set; }
		[JsonProperty("type")]
		public string Type { get; set; }
		[JsonProperty("data")]
		public T Data { get; set; }
	}
}