﻿namespace StockSharp.DarkHorse.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Leverage
{
	[JsonProperty("leverage")]
	public decimal? Cost { get; set; }
}
