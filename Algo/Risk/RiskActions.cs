namespace StockSharp.Algo.Risk;

/// <summary>
/// Types of actions.
/// </summary>
public enum RiskActions
{
	/// <summary>
	/// Close positions.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClosePositionsKey)]
	ClosePositions,

	/// <summary>
	/// Stop trading.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StopTradingKey)]
	StopTrading,

	/// <summary>
	/// Cancel orders.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CancelOrdersKey)]
	CancelOrders,
}