namespace StockSharp.Messages;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using StockSharp.Localization;

/// <summary>
/// States <see cref="IMessageChannel"/>.
/// </summary>
[DataContract]
[Serializable]
public enum ChannelStates
{
	/// <summary>
	/// Stopped.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1128Key)]
	[EnumMember]
	Stopped,

	/// <summary>
	/// Stopping.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1114Key)]
	[EnumMember]
	Stopping,

	/// <summary>
	/// Starting.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1129Key)]
	[EnumMember]
	Starting,

	/// <summary>
	/// Working.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1130Key)]
	[EnumMember]
	Started,

	/// <summary>
	/// In the process of suspension.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1131Key)]
	[EnumMember]
	Suspending, 

	/// <summary>
	/// Suspended.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1132Key)]
	[EnumMember]
	Suspended,
}