namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Option types.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum OptionTypes
	{
		/// <summary>
		/// Call.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str223Key)]
		Call,

		/// <summary>
		/// Put.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str224Key)]
		Put,
	}
}