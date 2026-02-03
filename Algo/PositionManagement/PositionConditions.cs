namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Additional order condition based on current position.
/// </summary>
public enum PositionConditions
{
	/// <summary>
	/// No additional condition.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NoneKey,
		Description = LocalizedStrings.PosConditionNoneKey)]
	NoCondition,

	/// <summary>
	/// Open position only. Only send order if the current position is zero.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionOpenKey,
		Description = LocalizedStrings.PosConditionOpenDetailsKey)]
	OpenPosition,

	/// <summary>
	/// Increase position only. Only send order if it is of the same direction as the current position or if the current position is zero.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionIncreaseOnlyKey,
		Description = LocalizedStrings.PosConditionIncreaseOnlyDetailsKey)]
	IncreaseOnly,

	/// <summary>
	/// Reduce position only. Only send order if it is of the opposite direction of the current non-zero position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionReduceOnlyKey,
		Description = LocalizedStrings.PosConditionReduceOnlyDetailsKey)]
	ReduceOnly,

	/// <summary>
	/// Close position. Order volume is calculated automatically based on current position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionCloseKey,
		Description = LocalizedStrings.PosConditionCloseDetailsKey)]
	ClosePosition,

	/// <summary>
	/// Invert position to the opposite of the current one. Order volume is calculated automatically based on current position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionInvertKey,
		Description = LocalizedStrings.PosConditionInvertDetailsKey)]
	InvertPosition,
}
