namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Типы опционов.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum OptionTypes
	{
		/// <summary>
		/// Кол.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str223Key)]
		Call,

		/// <summary>
		/// Пут.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str224Key)]
		Put,
	}
}