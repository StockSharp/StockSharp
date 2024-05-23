namespace StockSharp.FTX.Native.Model
{
	using Newtonsoft.Json;
	using System.Reflection;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Leverage
	{
		[JsonProperty("leverage")]
		public decimal? Cost { get; set; }
	}
}
