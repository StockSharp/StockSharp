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
	[Display(Name = "T+0")]
	T0,

	/// <summary>
	/// Т+1.
	/// </summary>
	[EnumMember]
	[Display(Name = "T+1")]
	T1,

	/// <summary>
	/// Т+2.
	/// </summary>
	[EnumMember]
	[Display(Name = "T+2")]
	T2,
	
	/// <summary>
	/// Т+x.
	/// </summary>
	[EnumMember]
	[Display(Name = "T+x")]
	Tx,
}