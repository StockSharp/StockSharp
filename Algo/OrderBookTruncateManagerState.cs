namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="IOrderBookTruncateManagerState"/>.
/// </summary>
public class OrderBookTruncateManagerState : IOrderBookTruncateManagerState
{
	private readonly SynchronizedDictionary<long, int> _depths = [];

	/// <inheritdoc />
	public void AddDepth(long transactionId, int depth)
	{
		_depths.Add(transactionId, depth);
	}

	/// <inheritdoc />
	public int? TryGetDepth(long transactionId)
	{
		return _depths.TryGetValue2(transactionId);
	}

	/// <inheritdoc />
	public bool RemoveDepth(long transactionId)
	{
		return _depths.Remove(transactionId);
	}

	/// <inheritdoc />
	public bool HasDepths => _depths.Count > 0;

	/// <inheritdoc />
	public IEnumerable<(int? depth, long[] ids)> GroupByDepth(long[] subscriptionIds)
	{
		return subscriptionIds
			.GroupBy(id => _depths.TryGetValue2(id))
			.Select(g => (g.Key, g.ToArray()));
	}

	/// <inheritdoc />
	public void Clear()
	{
		_depths.Clear();
	}
}
