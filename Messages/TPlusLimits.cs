namespace StockSharp.Messages;

/// <summary>
/// Т+ limit types.
/// </summary>
[DataContract]
[Serializable]
public enum TPlusLimits
{
	/// <summary>
	/// Т+0.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementT0Key)]
	T0,

	/// <summary>
	/// Т+1.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementT1Key)]
	T1,

	/// <summary>
	/// Т+2.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementT2Key)]
	T2,
	
	/// <summary>
	/// Т+x.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementTxKey)]
	Tx,
}