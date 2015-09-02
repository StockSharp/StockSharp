namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Order types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderTypes
	{
		/// <summary>
		/// Limit.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1353Key)]
		Limit,

		/// <summary>
		/// Market.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str241Key)]
		Market,

		/// <summary>
		/// Conditional (stop-loss, take-profit).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		Conditional,

		/// <summary>
		/// The order for REPO.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str243Key)]
		Repo,

		/// <summary>
		/// The order for modified REPO (REPO-M).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str244Key)]
		ExtRepo,

		/// <summary>
		/// Order for OTC trade.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str245Key)]
		Rps,

		/// <summary>
		/// Execution order to settlement contracts (such as options).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str246Key)]
		Execute,
	}
}