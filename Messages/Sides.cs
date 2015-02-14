namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Стороны действий.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum Sides
	{
		/// <summary>
		/// Покупка.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str403Key)]
		Buy,

		/// <summary>
		/// Продажа.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str404Key)]
		Sell,
	}
}