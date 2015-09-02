namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Limit order time in force.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum TimeInForce
	{
		/// <summary>
		/// Put in queue.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str405Key)]
		PutInQueue,

		/// <summary>
		/// Fill Or Kill.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.FOKKey)]
		MatchOrCancel,

		/// <summary>
		/// Immediate Or Cancel.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.IOCKey)]
		CancelBalance,
	}
}