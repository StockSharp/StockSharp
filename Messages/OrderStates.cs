namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Order states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderStates
	{
		/// <summary>
		/// Not sent to the trading system.
		/// </summary>
		/// <remarks>
		/// The original state of the order, when the transaction is not sent to the trading system.
		/// </remarks>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str237Key)]
		None,

		/// <summary>
		/// The order is accepted by the exchange and is active.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str238Key)]
		Active,

		/// <summary>
		/// The order is no longer active on an exchange (it was fully matched or cancelled).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str239Key)]
		Done,

		/// <summary>
		/// The order is not accepted by the trading system.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str152Key)]
		Failed,

		/// <summary>
		/// Pending acception.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str238Key)]
		Pending,
	}
}