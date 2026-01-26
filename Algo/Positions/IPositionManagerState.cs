namespace StockSharp.Algo.Positions;

/// <summary>
/// State storage for <see cref="PositionManager"/>.
/// </summary>
public interface IPositionManagerState
{
	/// <summary>
	/// Add or get existing order info.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="balance">Order balance.</param>
	/// <returns>Current balance of the order.</returns>
	decimal AddOrGetOrder(long transactionId, SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance);

	/// <summary>
	/// Try get order info by transaction ID.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="securityId">Security ID if found.</param>
	/// <param name="portfolioName">Portfolio name if found.</param>
	/// <param name="side">Order side if found.</param>
	/// <param name="balance">Current balance if found.</param>
	/// <returns><see langword="true"/> if order found.</returns>
	bool TryGetOrder(long transactionId, out SecurityId securityId, out string portfolioName, out Sides side, out decimal balance);

	/// <summary>
	/// Update order balance.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="newBalance">New balance value.</param>
	void UpdateOrderBalance(long transactionId, decimal newBalance);

	/// <summary>
	/// Remove order.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	void RemoveOrder(long transactionId);

	/// <summary>
	/// Update position by adding diff.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="diff">Position change.</param>
	/// <returns>New position value.</returns>
	decimal UpdatePosition(SecurityId securityId, string portfolioName, decimal diff);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
