namespace StockSharp.Algo.Strategies;

/// <summary>
/// <see cref="Order.Comment"/> auto-fill modes.
/// </summary>
[DataContract]
[Serializable]
public enum StrategyCommentModes
{
	/// <summary>
	/// Disabled.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisabledKey)]
	Disabled,

	/// <summary>
	/// By <see cref="Strategy.Id"/>.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IdKey)]
	Id,

	/// <summary>
	/// By <see cref="Strategy.Name"/>.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NameKey)]
	Name,
}

/// <summary>
/// Strategy trading modes.
/// </summary>
[DataContract]
[Serializable]
public enum StrategyTradingModes
{
	/// <summary>
	/// Allow trading.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradingKey,
		Description = LocalizedStrings.AllowTradingKey)]
	Full,

	/// <summary>
	/// Disabled trading.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DisabledKey,
		Description = LocalizedStrings.TradingDisabledKey)]
	Disabled,

	/// <summary>
	/// Cancel orders only.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CancelOrdersKey,
		Description = LocalizedStrings.CancelOrdersKey)]
	CancelOrdersOnly,

	/// <summary>
	/// Accept orders for reduce position only.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionReduceOnlyKey,
		Description = LocalizedStrings.PosConditionReduceOnlyDetailsKey)]
	ReducePositionOnly,

	/// <summary>
	/// Allow long positions only.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongOnlyKey,
		Description = LocalizedStrings.LongOnlyDetailsKey)]
	LongOnly,
}
