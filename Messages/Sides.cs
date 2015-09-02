namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Sides.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum Sides
	{
		/// <summary>
		/// Buy.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str403Key)]
		Buy,

		/// <summary>
		/// Sell.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str404Key)]
		Sell,
	}
}