namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
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
		/// Good til cancelled.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GTCKey, Description = LocalizedStrings.GoodTilCancelledKey)]
		PutInQueue,

		/// <summary>
		/// Fill Or Kill.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FOKKey, Description = LocalizedStrings.FillOrKillKey)]
		MatchOrCancel,

		/// <summary>
		/// Immediate Or Cancel.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IOCKey, Description = LocalizedStrings.ImmediateOrCancelKey)]
		CancelBalance,
	}
}