namespace StockSharp.Coinbase.Native.Model;

class Product
{
	[JsonProperty("product_id")]
	public string ProductId { get; set; }

	[JsonProperty("price")]
	public double? Price { get; set; }

	[JsonProperty("price_percentage_change_24h")]
	public double? PricePercentageChange24h { get; set; }

	[JsonProperty("volume_24h")]
	public double? Volume24h { get; set; }

	[JsonProperty("volume_percentage_change_24h")]
	public double? VolumePercentageChange24h { get; set; }

	[JsonProperty("base_increment")]
	public double? BaseIncrement { get; set; }

	[JsonProperty("quote_increment")]
	public double? QuoteIncrement { get; set; }

	[JsonProperty("quote_min_size")]
	public double? QuoteMinSize { get; set; }

	[JsonProperty("quote_max_size")]
	public double? QuoteMaxSize { get; set; }

	[JsonProperty("base_min_size")]
	public double? BaseMinSize { get; set; }

	[JsonProperty("base_max_size")]
	public double? BaseMaxSize { get; set; }

	[JsonProperty("base_name")]
	public string BaseName { get; set; }

	[JsonProperty("quote_name")]
	public string QuoteName { get; set; }

	[JsonProperty("watched")]
	public bool Watched { get; set; }

	[JsonProperty("is_disabled")]
	public bool IsDisabled { get; set; }

	[JsonProperty("new")]
	public bool New { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("cancel_only")]
	public bool CancelOnly { get; set; }

	[JsonProperty("limit_only")]
	public bool LimitOnly { get; set; }

	[JsonProperty("post_only")]
	public bool PostOnly { get; set; }

	[JsonProperty("trading_disabled")]
	public bool TradingDisabled { get; set; }

	[JsonProperty("auction_mode")]
	public bool AuctionMode { get; set; }

	[JsonProperty("product_type")]
	public string ProductType { get; set; }

	[JsonProperty("quote_currency_id")]
	public string QuoteCurrencyId { get; set; }

	[JsonProperty("base_currency_id")]
	public string BaseCurrencyId { get; set; }

	[JsonProperty("fcm_trading_session_details")]
	public object FcmTradingSessionDetails { get; set; }

	[JsonProperty("mid_market_price")]
	public double? MidMarketPrice { get; set; }

	[JsonProperty("alias")]
	public string Alias { get; set; }

	[JsonProperty("alias_to")]
	public string[] AliasTo { get; set; }

	[JsonProperty("base_display_symbol")]
	public string BaseDisplaySymbol { get; set; }

	[JsonProperty("quote_display_symbol")]
	public string QuoteDisplaySymbol { get; set; }

	[JsonProperty("view_only")]
	public bool ViewOnly { get; set; }

	[JsonProperty("price_increment")]
	public string PriceIncrement { get; set; }

	[JsonProperty("display_name")]
	public string DisplayName { get; set; }

	[JsonProperty("product_venue")]
	public string ProductVenue { get; set; }

	[JsonProperty("approximate_quote_24h_volume")]
	public double? ApproximateQuote24hVolume { get; set; }

	[JsonProperty("future_product_details")]
	public FutureProductDetails FutureProductDetails { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
class FutureProductDetails
{
	[JsonProperty("venue")]
	public string Venue { get; set; }

	[JsonProperty("contract_code")]
	public string ContractCode { get; set; }

	[JsonProperty("contract_expiry")]
	public DateTime ContractExpiry { get; set; }

	[JsonProperty("contract_size")]
	public double? ContractSize { get; set; }

	[JsonProperty("contract_root_unit")]
	public string ContractRootUnit { get; set; }

	[JsonProperty("group_description")]
	public string GroupDescription { get; set; }

	[JsonProperty("contract_expiry_timezone")]
	public string ContractExpiryTimezone { get; set; }

	[JsonProperty("group_short_description")]
	public string GroupShortDescription { get; set; }

	[JsonProperty("risk_managed_by")]
	public string RiskManagedBy { get; set; }

	[JsonProperty("contract_expiry_type")]
	public string ContractExpiryType { get; set; }

	[JsonProperty("contract_display_name")]
	public string ContractDisplayName { get; set; }

	[JsonProperty("time_to_expiry_ms")]
	public string TimeToExpiryMs { get; set; }
}