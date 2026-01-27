namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="ISubscriptionRoutingState"/>.
/// </summary>
public class SubscriptionRoutingState : ISubscriptionRoutingState
{
	private readonly SynchronizedDictionary<long, (ISubscriptionMessage subMsg, IMessageAdapter[] adapters, DataType dt)> _subscriptions = [];
	private readonly SynchronizedDictionary<long, (ISubscriptionMessage subMsg, IMessageAdapter adapter)> _requestsById = [];

	/// <inheritdoc />
	public void AddSubscription(long transactionId, ISubscriptionMessage message, IMessageAdapter[] adapters, DataType dataType)
	{
		_subscriptions.TryAdd2(transactionId, (message, adapters, dataType));
	}

	/// <inheritdoc />
	public bool TryGetSubscription(long transactionId, out ISubscriptionMessage message, out IMessageAdapter[] adapters, out DataType dataType)
	{
		if (_subscriptions.TryGetValue(transactionId, out var tuple))
		{
			message = tuple.subMsg;
			adapters = tuple.adapters;
			dataType = tuple.dt;
			return true;
		}

		message = null;
		adapters = null;
		dataType = null;
		return false;
	}

	/// <inheritdoc />
	public bool RemoveSubscription(long transactionId)
	{
		return _subscriptions.Remove(transactionId);
	}

	/// <inheritdoc />
	public long[] GetSubscribers(DataType dataType)
	{
		return _subscriptions.SyncGet(c => c.Where(p => p.Value.dt == dataType).Select(p => p.Key).ToArray());
	}

	/// <inheritdoc />
	public void AddRequest(long transactionId, ISubscriptionMessage message, IMessageAdapter adapter)
	{
		_requestsById.TryAdd2(transactionId, (message, adapter));
	}

	/// <inheritdoc />
	public bool TryGetRequest(long transactionId, out ISubscriptionMessage message, out IMessageAdapter adapter)
	{
		if (_requestsById.TryGetValue(transactionId, out var tuple))
		{
			message = tuple.subMsg;
			adapter = tuple.adapter;
			return true;
		}

		message = null;
		adapter = null;
		return false;
	}

	/// <inheritdoc />
	public bool RemoveRequest(long transactionId)
	{
		return _requestsById.Remove(transactionId);
	}

	/// <inheritdoc />
	public void Clear()
	{
		_subscriptions.Clear();
		_requestsById.Clear();
	}
}
