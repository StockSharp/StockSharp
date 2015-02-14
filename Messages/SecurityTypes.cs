namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Типы инструментов.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum SecurityTypes
	{
		/// <summary>
		/// Ценная бумага.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.StockKey)]
		Stock,

		/// <summary>
		/// Фьючерс.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.FutureContractKey)]
		Future,

		/// <summary>
		/// Опцион.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.OptionsContractKey)]
		Option,

		/// <summary>
		/// Индекс.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.IndexKey)]
		Index,

		/// <summary>
		/// Валюта.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str250Key)]
		Currency,

		/// <summary>
		/// Облигация.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.BondKey)]
		Bond,

		/// <summary>
		/// Варрант.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WarrantKey)]
		Warrant,

		/// <summary>
		/// Форвард.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ForwardKey)]
		Forward,

		/// <summary>
		/// Споп.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SwapKey)]
		Swap,

		/// <summary>
		/// Товар.
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
		/// Новость.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str395Key)]
		News,

		/// <summary>
		/// Погода.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WeatherKey)]
		Weather,

		/// <summary>
		/// Паевые фонды.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ShareFundKey)]
		Fund,

		/// <summary>
		/// Американские депозитарные расписки.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ADR")]
		Adr,

		/// <summary>
		/// Крипто-валюта.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str398Key)]
		CryptoCurrency,
	}
}