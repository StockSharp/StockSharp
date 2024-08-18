namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoneKey)]
	None,

	/// <summary>
	/// The order is accepted by the exchange and is active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,

	/// <summary>
	/// The order is no longer active on an exchange (it was fully matched or cancelled).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DoneKey)]
	Done,

	/// <summary>
	/// The order is not accepted by the trading system.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorKey)]
	Failed,

	/// <summary>
	/// Pending registration.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PendingKey)]
	Pending,
}