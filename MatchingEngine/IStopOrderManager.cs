namespace StockSharp.MatchingEngine;

/// <summary>
/// Stop order information.
/// </summary>
public class StopOrderInfo
{
	/// <summary>
	/// Transaction ID.
	/// </summary>
	public long TransactionId { get; set; }

	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Order side.
	/// </summary>
	public Sides Side { get; set; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get; set; }

	/// <summary>
	/// Portfolio name.
	/// </summary>
	public string PortfolioName { get; set; }

	/// <summary>
	/// Stop activation price.
	/// </summary>
	public decimal StopPrice { get; set; }

	/// <summary>
	/// Limit price for stop-limit orders. <see langword="null"/> means market order.
	/// </summary>
	public decimal? LimitPrice { get; set; }

	/// <summary>
	/// Whether this is a trailing stop.
	/// </summary>
	public bool IsTrailing { get; set; }

	/// <summary>
	/// Trailing offset from extremum.
	/// </summary>
	public decimal? TrailingOffset { get; set; }

	/// <summary>
	/// Best seen price for trailing stop (max for sell, min for buy).
	/// </summary>
	public decimal? BestSeenPrice { get; set; }

	/// <summary>
	/// When <see langword="true"/>, trigger logic is inverted (TakeProfit mode):
	/// Buy triggers when price &lt;= StopPrice, Sell triggers when price &gt;= StopPrice.
	/// </summary>
	public bool InvertTrigger { get; set; }
}

/// <summary>
/// Triggered stop order result.
/// </summary>
/// <param name="Info">Stop order info.</param>
/// <param name="TriggerPrice">The price that triggered the stop.</param>
/// <param name="ResultingOrder">The order to submit to the matcher.</param>
public record StopOrderTrigger(StopOrderInfo Info, decimal TriggerPrice, OrderRegisterMessage ResultingOrder);

/// <summary>
/// Interface for managing stop orders.
/// </summary>
public interface IStopOrderManager
{
	/// <summary>
	/// Register a new stop order.
	/// </summary>
	void Register(StopOrderInfo info);

	/// <summary>
	/// Cancel a stop order.
	/// </summary>
	/// <param name="transactionId">Transaction ID of the stop order.</param>
	/// <param name="info">Cancelled stop order info.</param>
	/// <returns><see langword="true"/> if the stop order was found and cancelled.</returns>
	bool Cancel(long transactionId, out StopOrderInfo info);

	/// <summary>
	/// Atomically replace an existing stop order with a new one.
	/// </summary>
	/// <param name="origTransactionId">Transaction ID of the stop order to replace.</param>
	/// <param name="newInfo">New stop order info.</param>
	/// <returns><see langword="true"/> if the old stop order was found and replaced.</returns>
	bool Replace(long origTransactionId, StopOrderInfo newInfo);

	/// <summary>
	/// Check price against all registered stop orders for the given security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="price">Current market price.</param>
	/// <param name="time">Current time.</param>
	/// <returns>List of triggered stop orders with resulting orders to submit.</returns>
	IReadOnlyList<StopOrderTrigger> CheckPrice(SecurityId securityId, decimal price, DateTime time);

	/// <summary>
	/// Clear all stop orders.
	/// </summary>
	void Clear();
}
