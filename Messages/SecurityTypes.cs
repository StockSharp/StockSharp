namespace StockSharp.Messages;

/// <summary>
/// Securities types.
/// </summary>
[Serializable]
[DataContract]
public enum SecurityTypes
{
	/// <summary>
	/// Shares.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StockKey)]
	Stock,

	/// <summary>
	/// Future contract.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FutureContractKey)]
	Future,

	/// <summary>
	/// Options contract.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OptionsContractKey)]
	Option,

	/// <summary>
	/// Index.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndexKey)]
	Index,

	/// <summary>
	/// Currency.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrencyKey)]
	Currency,

	/// <summary>
	/// Bond.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BondKey)]
	Bond,

	/// <summary>
	/// Warrant.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.WarrantKey)]
	Warrant,

	/// <summary>
	/// Forward.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ForwardKey)]
	Forward,

	/// <summary>
	/// Swap.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SwapKey)]
	Swap,

	/// <summary>
	/// Commodity.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommodityKey)]
	Commodity,

	/// <summary>
	/// CFD.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CfdKey)]
	Cfd,

	/// <summary>
	/// News.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NewsKey)]
	News,

	/// <summary>
	/// Weather.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.WeatherKey)]
	Weather,

	/// <summary>
	/// Mutual funds.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ShareFundKey)]
	Fund,

	/// <summary>
	/// American Depositary Receipts.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AdrKey)]
	Adr,

	/// <summary>
	/// Cryptocurrency.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CryptocurrencyKey)]
	CryptoCurrency,

	/// <summary>
	/// ETF.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EtfKey)]
	Etf,

	/// <summary>
	/// Multi leg.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LegsKey)]
	MultiLeg,
	
	/// <summary>
	/// Loan.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoanKey)]
	Loan,
	
	/// <summary>
	/// Spread.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SpreadKey)]
	Spread,
	
	/// <summary>
	/// Global Depositary Receipts.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GdrKey)]
	Gdr,
	
	/// <summary>
	/// Receipt.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ReceiptKey)]
	Receipt,
	
	/// <summary>
	/// Indicator.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndicatorKey)]
	Indicator,
	
	/// <summary>
	/// Strategy.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyKey)]
	Strategy,
	
	/// <summary>
	/// Volatility.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolatilityKey)]
	Volatility,

	/// <summary>
	/// REPO.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RepoKey)]
	Repo,
}