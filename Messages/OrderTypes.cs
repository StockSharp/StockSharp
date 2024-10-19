namespace StockSharp.Messages;

/// <summary>
/// Order types.
/// </summary>
[DataContract]
[Serializable]
public enum OrderTypes
{
	/// <summary>
	/// Limit.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LimitOrderKey)]
	Limit,

	/// <summary>
	/// Market.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketKey)]
	Market,

	/// <summary>
	/// Conditional (stop-loss, take-profit).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StopOrderTypeKey)]
	Conditional,
}