namespace StockSharp.Algo;

/// <summary>
/// States of the process.
/// </summary>
[DataContract]
public enum ProcessStates
{
	/// <summary>
	/// Stopped.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppedKey)]
	Stopped,

	/// <summary>
	/// Stopping.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppingKey)]
	Stopping,

	/// <summary>
	/// Started.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StartedKey)]
	Started,
}