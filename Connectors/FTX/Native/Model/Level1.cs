namespace StockSharp.FTX.Native.Model
{
	using System;
	using System.Reflection;
	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class Level1
	{
		[JsonProperty("bid")]
		public decimal Bid { get; set; }
		[JsonProperty("bidSize")]
		public decimal BidSize { get; set; }
		[JsonProperty("ask")]
		public decimal Ask { get; set; }
		[JsonProperty("askSize")]
		public decimal AskSize { get; set; }
		[JsonProperty("time")]
		public decimal Time { get; set; }
		private static readonly DateTime _epochTime = new(1970, 1, 1, 0, 0, 0);
		public DateTime ConvertTime()
		{
			return _epochTime.AddSeconds((double)Time);
		}
	}
}