namespace StockSharp.Algo
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Available permissions which customer receives for work with data.
	/// </summary>
	[Flags]
	[DataContract]
	public enum UserPermissions
	{
		/// <summary>
		/// Market-data downloading.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditMarketDataKey)]
		[EnumMember]
		Save = 1,

		/// <summary>
		/// Market-data uploading.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadMarketDataKey)]
		[EnumMember]
		Load = Save << 1,

		/// <summary>
		/// Data deletion.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteMarketDataKey)]
		[EnumMember]
		Delete = Load << 1,

		/// <summary>
		/// Security lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadSecuritiesKey)]
		[EnumMember]
		SecurityLookup = Delete << 1,

		/// <summary>
		/// Exchange lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadExchangesKey)]
		[EnumMember]
		ExchangeLookup = SecurityLookup << 1,

		/// <summary>
		/// Exchange board lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadBoardsKey)]
		[EnumMember]
		ExchangeBoardLookup = ExchangeLookup << 1,

		/// <summary>
		/// Edit securities.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditSecuritiesKey)]
		[EnumMember]
		EditSecurities = ExchangeBoardLookup << 1,

		/// <summary>
		/// Edit exchanges.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditExchangesKey)]
		[EnumMember]
		EditExchanges = EditSecurities << 1,

		/// <summary>
		/// Edit boards.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditBoardsKey)]
		[EnumMember]
		EditBoards = EditExchanges << 1,

		/// <summary>
		/// Delete securities.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteSecuritiesKey)]
		[EnumMember]
		DeleteSecurities = EditBoards << 1,

		/// <summary>
		/// Delete exchanges.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteExchangesKey)]
		[EnumMember]
		DeleteExchanges = DeleteSecurities << 1,

		/// <summary>
		/// Delete boards.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteBoardsKey)]
		[EnumMember]
		DeleteBoards = DeleteExchanges << 1,

		/// <summary>
		/// Get users.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.GetUsersKey)]
		[EnumMember]
		GetUsers = DeleteBoards << 1,

		/// <summary>
		/// Edit users.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditUsersKey)]
		[EnumMember]
		EditUsers = GetUsers << 1,

		/// <summary>
		/// Delete users.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteUsersKey)]
		[EnumMember]
		DeleteUsers = EditUsers << 1,

		/// <summary>
		/// Restart.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ManageServerKey)]
		[EnumMember]
		ServerManage = DeleteUsers << 1,
	}
}