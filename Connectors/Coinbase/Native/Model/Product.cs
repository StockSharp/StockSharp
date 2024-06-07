namespace StockSharp.Coinbase.Native.Model
{
	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Product
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("base_currency")]
		public string BaseCurrency { get; set; }

		[JsonProperty("quote_currency")]
		public string QuoteCurrency { get; set; }

		[JsonProperty("base_min_size")]
		public decimal BaseMinSize { get; set; }

		[JsonProperty("base_max_size")]
		public decimal BaseMaxSize { get; set; }

		[JsonProperty("quote_increment")]
		public decimal QuoteIncrement { get; set; }

		[JsonProperty("display_name")]
		public string DisplayName { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("margin_enabled")]
		public bool MarginEnabled { get; set; }

		[JsonProperty("status_message")]
		public string StatusMessage { get; set; }
	}
}