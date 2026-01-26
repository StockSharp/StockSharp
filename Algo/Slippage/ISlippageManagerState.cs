namespace StockSharp.Algo.Slippage;

/// <summary>
/// State storage for <see cref="SlippageManager"/>.
/// </summary>
public interface ISlippageManagerState
{
	/// <summary>
	/// Total accumulated slippage.
	/// </summary>
	decimal Slippage { get; }

	/// <summary>
	/// Add to accumulated slippage.
	/// </summary>
	/// <param name="amount">Amount to add.</param>
	void AddSlippage(decimal amount);

	/// <summary>
	/// Update best bid/ask prices for a security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="bidPrice">Best bid price (null to keep existing).</param>
	/// <param name="askPrice">Best ask price (null to keep existing).</param>
	void UpdateBestPrices(SecurityId securityId, decimal? bidPrice, decimal? askPrice);

	/// <summary>
	/// Try get best price for order side.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="side">Order side (Buy returns ask, Sell returns bid).</param>
	/// <param name="price">Best price if found.</param>
	/// <returns><see langword="true"/> if price found and non-zero.</returns>
	bool TryGetBestPrice(SecurityId securityId, Sides side, out decimal price);

	/// <summary>
	/// Store planned execution price for an order.
	/// </summary>
	/// <param name="transactionId">Order transaction ID.</param>
	/// <param name="side">Order side.</param>
	/// <param name="price">Planned price.</param>
	void AddPlannedPrice(long transactionId, Sides side, decimal price);

	/// <summary>
	/// Try get planned price without removing.
	/// </summary>
	/// <param name="transactionId">Order transaction ID.</param>
	/// <param name="side">Order side if found.</param>
	/// <param name="price">Planned price if found.</param>
	/// <returns><see langword="true"/> if found.</returns>
	bool TryGetPlannedPrice(long transactionId, out Sides side, out decimal price);

	/// <summary>
	/// Remove planned price for an order.
	/// </summary>
	/// <param name="transactionId">Order transaction ID.</param>
	void RemovePlannedPrice(long transactionId);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
