using System.Reflection;
using Newtonsoft.Json;
using System;
namespace SciTrader.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Leverage
	{
		[JsonProperty("leverage")]
		public decimal? Cost { get; set; }
	}
}
