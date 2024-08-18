namespace StockSharp.Algo;

/// <summary>
/// The type of market prices.
/// </summary>
[DataContract]
public enum MarketPriceTypes
{
	/// <summary>
	/// The counter-price (for quick closure of position).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OppositeKey)]
	[EnumMember]
	Opposite,

	/// <summary>
	/// The concurrent price (for quoting at the edge of spread).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FollowingKey)]
	[EnumMember]
	Following,

	/// <summary>
	/// Spread middle.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SpreadKey)]
	[EnumMember]
	Middle,
}