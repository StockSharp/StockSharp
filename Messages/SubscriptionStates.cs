namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppedKey)]
	Stopped,

	/// <summary>
	/// Active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,

	/// <summary>
	/// Error.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorKey)]
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