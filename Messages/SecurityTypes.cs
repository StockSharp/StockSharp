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
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

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
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str395Key)]
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
	}
}