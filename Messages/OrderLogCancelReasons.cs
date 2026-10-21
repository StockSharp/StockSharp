namespace StockSharp.Messages;

/// <summary>
/// Reasons for orders cancelling in the orders log.
/// </summary>
public enum OrderLogCancelReasons
{
	/// <summary>
	/// The order re-registration.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ReregisteringKey)]
	ReRegistered,

	/// <summary>
	/// Cancel order.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CancelledKey)]
	Canceled,

	/// <summary>
	/// Group canceling of orders.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderGroupCancelKey)]
	GroupCanceled,

	/// <summary>
	/// The sign of deletion of order residual due to cross-trade.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CrossTradeKey)]
	CrossTrade,
}