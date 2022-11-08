namespace StockSharp.Messages;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using StockSharp.Localization;

/// <summary>
/// Subscription states.
/// </summary>
[DataContract]
[Serializable]
public enum SubscriptionStates
{
	/// <summary>
	/// Stopped.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str3178Key)]
	Stopped,

	/// <summary>
	/// Active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str2229Key)]
	Active,

	/// <summary>
	/// Error.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str152Key)]
	Error,

	/// <summary>
	/// Finished.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FinishedKey)]
	Finished,

	/// <summary>
	/// Online.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OnlineKey)]
	Online,
}