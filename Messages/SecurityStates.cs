namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Security states.
	/// </summary>
	[Serializable]
	[DataContract]
	public enum SecurityStates
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SecurityActiveKey)]
		Trading,

		/// <summary>
		/// Suspended.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.SecuritySuspendedKey)]
		Stoped,
	}
}