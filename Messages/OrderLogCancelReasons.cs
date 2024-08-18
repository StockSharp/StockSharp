namespace StockSharp.Messages;

/// <summary>
/// Reasons for orders cancelling in the orders log.
/// </summary>
public enum OrderLogCancelReasons
{
	/// <summary>
	/// The order re-registration.
	/// </summary>
	ReRegistered,

	/// <summary>
	/// Cancel order.
	/// </summary>
	Canceled,

	/// <summary>
	/// Group canceling of orders.
	/// </summary>
	GroupCanceled,

	/// <summary>
	/// The sign of deletion of order residual due to cross-trade.
	/// </summary>
	CrossTrade,
}