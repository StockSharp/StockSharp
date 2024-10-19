namespace StockSharp.Messages;

/// <summary>
/// Option types.
/// </summary>
[Serializable]
[DataContract]
public enum OptionTypes
{
	/// <summary>
	/// Call.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CallKey)]
	Call,

	/// <summary>
	/// Put.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PutKey)]
	Put,
}