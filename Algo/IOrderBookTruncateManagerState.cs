namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="OrderBookTruncateManager"/>.
/// </summary>
public interface IOrderBookTruncateManagerState
{
	/// <summary>
	/// Add depth for subscription.
	/// </summary>
	void AddDepth(long transactionId, int depth);

	/// <summary>
	/// Try get depth for subscription.
	/// </summary>
	int? TryGetDepth(long transactionId);

	/// <summary>
	/// Remove depth for subscription.
	/// </summary>
	bool RemoveDepth(long transactionId);

	/// <summary>
	/// Whether any depths are tracked.
	/// </summary>
	bool HasDepths { get; }

	/// <summary>
	/// Group subscription IDs by their associated depths.
	/// Returns null-keyed group for IDs without depth tracking.
	/// </summary>
	IEnumerable<(int? depth, long[] ids)> GroupByDepth(long[] subscriptionIds);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
