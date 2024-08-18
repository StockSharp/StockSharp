namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
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
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityActiveKey)]
		Trading,

		/// <summary>
		/// Suspended.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SuspendedKey)]
		Stoped,
	}
}