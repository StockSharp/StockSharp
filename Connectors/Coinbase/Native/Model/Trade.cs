namespace StockSharp.Coinbase.Native.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Trade
	{
		[JsonProperty("trade_id")]
		public long Id { get; set; }

		[JsonProperty("sequence")]
		public long Sequence { get; set; }

		[JsonProperty("maker_order_id")]
		public string MakerOrderId { get; set; }

		[JsonProperty("taker_order_id")]
		public string TakerOrderId { get; set; }

		[JsonProperty("time")]
		public DateTime Time { get; set; }

		[JsonProperty("product_id")]
		public string Product { get; set; }

		[JsonProperty("price")]
		public decimal Price { get; set; }

		[JsonProperty("size")]
		public decimal Size { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }
	}
}