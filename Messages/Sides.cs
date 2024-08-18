namespace StockSharp.Messages;

/// <summary>
/// Sides.
/// </summary>
[DataContract]
[Serializable]
public enum Sides
{
	/// <summary>
	/// Buy.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Buy2Key)]
	Buy,

	/// <summary>
	/// Sell.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Sell2Key)]
	Sell,
}