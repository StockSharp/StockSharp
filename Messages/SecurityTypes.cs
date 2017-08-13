#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: SecurityTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		/// Commodity.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CommodityKey)]
		Commodity,

		/// <summary>
		/// CFD.
		/// </summary>
		[EnumMember]
		[EnumDisplayName(LocalizedStrings.CfdKey)]
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
		[EnumDisplayName(LocalizedStrings.AdrKey)]
		Adr,

		/// <summary>
		/// Cryptocurrency.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CryptocurrencyKey)]
		CryptoCurrency,
	}
}