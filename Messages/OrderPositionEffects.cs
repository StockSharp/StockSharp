namespace StockSharp.Messages;

/// <summary>
/// Indicates whether the resulting position after a trade should be an opening position or closing position.
/// </summary>
[DataContract]
[Serializable]
public enum OrderPositionEffects
{
	/// <summary>
	/// Default behaviour.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DefaultKey, Description = LocalizedStrings.DefaultBehaviourKey)]
	Default,

	/// <summary>
	/// A trade should open a position.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OpenOnlyKey, Description = LocalizedStrings.PositionEffectOpenOnlyKey)]
	OpenOnly,

	/// <summary>
	/// A trade should bring the position towards zero, i.e. close as much as possible of any existing position and open an opposite position for any remainder.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CloseOnlyKey, Description = LocalizedStrings.PositionEffectCloseOnlyKey)]
	CloseOnly,
}