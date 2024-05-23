namespace StockSharp.FTX.Native.Model
{
	using System;
	using System.Reflection;
	using Ecng.Serialization;
	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	[JsonConverter(typeof(JArrayToObjectConverter))]
	internal class OrderBookEntry
	{
		public decimal Price { get; set; }
		public decimal Size { get; set; }


	}

	internal class OrderBookEntryInternal
	{
		public OrderBookEntry Entry { get; set; }
		public bool IsChanged { get; set; }

		public OrderBookEntryInternal(OrderBookEntry entry)
		{
			Entry = entry;
		}
	}

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	internal class OrderBook
	{
		[JsonProperty("bids")]
		public OrderBookEntry[] Bids { get; set; }

		[JsonProperty("asks")]
		public OrderBookEntry[] Asks { get; set; }

		[JsonProperty("time")]
		public decimal Time { get; set; }
		private static readonly DateTime _epochTime = new(1970, 1, 1, 0, 0, 0);
		public DateTime ConvertTime()
		{
			return _epochTime.AddSeconds((double)Time);
		}
	}
}