namespace StockSharp.Messages;

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using StockSharp.Localization;

/// <summary>
/// Settlement types.
/// </summary>
[Serializable]
[DataContract]
public enum SettlementTypes
{
	/// <summary>
	/// Delivery.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeliveryKey)]
	Delivery,

	/// <summary>
	/// Cash.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CashKey)]
	Cash,
}