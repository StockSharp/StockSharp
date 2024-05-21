namespace StockSharp.Coinbase.Native.Model
{
	using System.Reflection;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Withdraw
	{
		[JsonProperty("id")]
		public string Id { get; set; }
	}
}