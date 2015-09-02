namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

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
		[EnumDisplayNameLoc(LocalizedStrings.StockKey)]
		Stock,

		/// <summary>
		/// Future contract.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.FutureContractKey)]
		Future,

		/// <summary>
		/// Options contract.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.OptionsContractKey)]
		Option,

		/// <summary>
		/// Index.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.IndexKey)]
		Index,

		/// <summary>
		/// Currency.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CurrencyKey)]
		Currency,

		/// <summary>
		/// Bond.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.BondKey)]
		Bond,

		/// <summary>
		/// Warrant.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WarrantKey)]
		Warrant,

		/// <summary>
		/// Forward.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ForwardKey)]
		Forward,

		/// <summary>
		/// Swap.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SwapKey)]
		Swap,

		/// <summary>
		/// Product.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ProductKey)]
		Commodity,

		/// <summary>
		/// CFD.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("CFD")]
		Cfd,

		/// <summary>
		/// News.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str395Key)]
		News,

		/// <summary>
		/// Weather.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WeatherKey)]
		Weather,

		/// <summary>
		/// Mutual funds.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ShareFundKey)]
		Fund,

		/// <summary>
		/// American Depositary Receipts.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ADR")]
		Adr,

		/// <summary>
		/// Cryptocurrency.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CryptocurrencyKey)]
		CryptoCurrency,
	}
}