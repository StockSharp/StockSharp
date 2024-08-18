namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EditMarketDataKey)]
	[EnumMember]
	Save = 1,

	/// <summary>
	/// Market-data downloading.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadMarketDataKey)]
	[EnumMember]
	Load = Save << 1,

	/// <summary>
	/// Data deletion.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeleteMarketDataKey)]
	[EnumMember]
	Delete = Load << 1,

	/// <summary>
	/// Security lookup.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadSecuritiesKey)]
	[EnumMember]
	SecurityLookup = Delete << 1,

	/// <summary>
	/// Exchange board lookup.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadBoardsKey)]
	[EnumMember]
	ExchangeBoardLookup = SecurityLookup << 1,

	/// <summary>
	/// Edit securities.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EditSecuritiesKey)]
	[EnumMember]
	EditSecurities = ExchangeBoardLookup << 1,

	/// <summary>
	/// Edit boards.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EditBoardsKey)]
	[EnumMember]
	EditBoards = EditSecurities << 1,

	/// <summary>
	/// Delete securities.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeleteSecuritiesKey)]
	[EnumMember]
	DeleteSecurities = EditBoards << 1,

	/// <summary>
	/// Delete boards.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeleteBoardsKey)]
	[EnumMember]
	DeleteBoards = DeleteSecurities << 1,

	/// <summary>
	/// Get users.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GetUsersKey)]
	[EnumMember]
	GetUsers = DeleteBoards << 1,

	/// <summary>
	/// Edit users.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EditUsersKey)]
	[EnumMember]
	EditUsers = GetUsers << 1,

	/// <summary>
	/// Delete users.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeleteUsersKey)]
	[EnumMember]
	DeleteUsers = EditUsers << 1,

	/// <summary>
	/// Restart.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ManageServerKey)]
	[EnumMember]
	ServerManage = DeleteUsers << 1,

	/// <summary>
	/// Trading.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TradingKey)]
	[EnumMember]
	Trading = ServerManage << 1,

	/// <summary>
	/// Withdraw.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.WithdrawKey)]
	[EnumMember]
	Withdraw = Trading << 1,
}