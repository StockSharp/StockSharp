namespace StockSharp.FTX.Native.Model
{
	using System.Reflection;
	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Account
	{
		[JsonProperty("username")]
		public string UserName { get; set; }
		[JsonProperty("totalAccountValue")]
		public string TotalAccountValue { get; set; }

	}
}