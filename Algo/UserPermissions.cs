namespace StockSharp.Algo
{
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// Available permissions which customer receives for work with data.
	/// </summary>
	[Flags]
	public enum UserPermissions
	{
		/// <summary>
		/// Market-data downloading.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditMarketDataKey)]
		Save = 1,

		/// <summary>
		/// Market-data uploading.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadMarketDataKey)]
		Load = Save << 1,

		/// <summary>
		/// Data deletion.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteMarketDataKey)]
		Delete = Load << 1,

		/// <summary>
		/// Security lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadSecuritiesKey)]
		SecurityLookup = Delete << 1,

		/// <summary>
		/// Exchange lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadExchangesKey)]
		ExchangeLookup = SecurityLookup << 1,

		/// <summary>
		/// Exchange board lookup.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.LoadBoardsKey)]
		ExchangeBoardLookup = ExchangeLookup << 1,

		/// <summary>
		/// Edit securities.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditSecuritiesKey)]
		EditSecurities = ExchangeBoardLookup << 1,

		/// <summary>
		/// Edit exchanges.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditExchangesKey)]
		EditExchanges = EditSecurities << 1,

		/// <summary>
		/// Edit boards.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.EditBoardsKey)]
		EditBoards = EditExchanges << 1,

		/// <summary>
		/// Delete securities.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteSecuritiesKey)]
		DeleteSecurities = EditBoards << 1,

		/// <summary>
		/// Delete exchanges.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteExchangesKey)]
		DeleteExchanges = DeleteSecurities << 1,

		/// <summary>
		/// Delete boards.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DeleteBoardsKey)]
		DeleteBoards = DeleteExchanges << 1,
	}
}