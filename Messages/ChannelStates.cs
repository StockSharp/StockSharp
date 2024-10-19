namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppedKey)]
	[EnumMember]
	Stopped,

	/// <summary>
	/// Stopping.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppingKey)]
	[EnumMember]
	Stopping,

	/// <summary>
	/// Starting.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StartingKey)]
	[EnumMember]
	Starting,

	/// <summary>
	/// Working.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StartedKey)]
	[EnumMember]
	Started,

	/// <summary>
	/// In the process of suspension.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SuspendingKey)]
	[EnumMember]
	Suspending, 

	/// <summary>
	/// Suspended.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SuspendedKey)]
	[EnumMember]
	Suspended,
}