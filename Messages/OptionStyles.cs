namespace StockSharp.Messages;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using StockSharp.Localization;

/// <summary>
/// Option styles.
/// </summary>
[Serializable]
[DataContract]
public enum OptionStyles
{
	/// <summary>
	/// European.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EuropeanKey)]
	European,

	/// <summary>
	/// American.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AmericanKey)]
	American,

	/// <summary>
	/// Exotic.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExoticKey)]
	Exotic,
}