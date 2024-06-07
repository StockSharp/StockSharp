namespace StockSharp.Coinbase.Native.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	[JsonConverter(typeof(JArrayToObjectConverter))]
	class OrderBookEntry
	{
		public decimal Price { get; set; }
		public decimal Size { get; set; }
	}

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class OrderBook
	{
		[JsonProperty("bids")]
		public OrderBookEntry[] Bids { get; set; }

		[JsonProperty("asks")]
		public OrderBookEntry[] Asks { get; set; }

		[JsonProperty("product_id")]
		public string Product { get; set; }
	}

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	[JsonConverter(typeof(JArrayToObjectConverter))]
	class OrderBookChangeEntry
	{
		public string Side { get; set; }
		public decimal Price { get; set; }
		public decimal Size { get; set; }
	}

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class OrderBookChanges
	{
		[JsonProperty("changes")]
		public OrderBookChangeEntry[] Entries { get; set; }

		[JsonProperty("product_id")]
		public string Product { get; set; }
	}
}