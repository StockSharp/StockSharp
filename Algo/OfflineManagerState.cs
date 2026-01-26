namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="IOfflineManagerState"/>.
/// </summary>
public class OfflineManagerState : IOfflineManagerState
{
	private readonly Lock _sync = new();
	private bool _connected;
	private readonly List<Message> _suspendedIn = [];
	private readonly PairSet<long, ISubscriptionMessage> _pendingSubscriptions = [];
	private readonly PairSet<long, OrderRegisterMessage> _pendingRegistration = [];

	/// <inheritdoc />
	public bool IsConnected
	{
		get
		{
			using (_sync.EnterScope())
				return _connected;
		}
	}

	/// <inheritdoc />
	public void SetConnected(bool value)
	{
		using (_sync.EnterScope())
			_connected = value;
	}

	/// <inheritdoc />
	public int SuspendedCount
	{
		get
		{
			using (_sync.EnterScope())
				return _suspendedIn.Count;
		}
	}

	/// <inheritdoc />
	public void AddSuspended(Message message)
	{
		using (_sync.EnterScope())
			_suspendedIn.Add(message);
	}

	/// <inheritdoc />
	public bool RemoveSuspended(Message message)
	{
		using (_sync.EnterScope())
			return _suspendedIn.Remove(message);
	}

	/// <inheritdoc />
	public Message[] GetAndClearSuspended()
	{
		using (_sync.EnterScope())
			return _suspendedIn.CopyAndClear();
	}

	/// <inheritdoc />
	public void AddPendingSubscription(long transactionId, ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			_pendingSubscriptions.Add(transactionId, subscription);
	}

	/// <inheritdoc />
	public bool TryGetAndRemovePendingSubscription(long transactionId, out ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			return _pendingSubscriptions.TryGetAndRemove(transactionId, out subscription);
	}

	/// <inheritdoc />
	public void RemovePendingSubscriptionByValue(ISubscriptionMessage subscription)
	{
		using (_sync.EnterScope())
			_pendingSubscriptions.RemoveByValue(subscription);
	}

	/// <inheritdoc />
	public void AddPendingRegistration(long transactionId, OrderRegisterMessage order)
	{
		using (_sync.EnterScope())
			_pendingRegistration.Add(transactionId, order);
	}

	/// <inheritdoc />
	public bool TryGetAndRemovePendingRegistration(long transactionId, out OrderRegisterMessage order)
	{
		using (_sync.EnterScope())
			return _pendingRegistration.TryGetAndRemove(transactionId, out order);
	}

	/// <inheritdoc />
	public void RemovePendingRegistrationByValue(OrderRegisterMessage order)
	{
		using (_sync.EnterScope())
			_pendingRegistration.RemoveByValue(order);
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_connected = false;
			_suspendedIn.Clear();
			_pendingSubscriptions.Clear();
			_pendingRegistration.Clear();
		}
	}
}
