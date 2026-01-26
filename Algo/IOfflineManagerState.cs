namespace StockSharp.Algo;

/// <summary>
/// State storage for <see cref="OfflineManager"/>.
/// </summary>
public interface IOfflineManagerState
{
	/// <summary>
	/// Get connection status.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Set connection status.
	/// </summary>
	void SetConnected(bool value);

	/// <summary>
	/// Get suspended messages count.
	/// </summary>
	int SuspendedCount { get; }

	/// <summary>
	/// Add message to suspended queue.
	/// </summary>
	void AddSuspended(Message message);

	/// <summary>
	/// Remove message from suspended queue.
	/// </summary>
	bool RemoveSuspended(Message message);

	/// <summary>
	/// Get and clear all suspended messages.
	/// </summary>
	Message[] GetAndClearSuspended();

	/// <summary>
	/// Add pending subscription.
	/// </summary>
	void AddPendingSubscription(long transactionId, ISubscriptionMessage subscription);

	/// <summary>
	/// Try get and remove pending subscription by transaction ID.
	/// </summary>
	bool TryGetAndRemovePendingSubscription(long transactionId, out ISubscriptionMessage subscription);

	/// <summary>
	/// Remove pending subscription by value.
	/// </summary>
	void RemovePendingSubscriptionByValue(ISubscriptionMessage subscription);

	/// <summary>
	/// Add pending order registration.
	/// </summary>
	void AddPendingRegistration(long transactionId, OrderRegisterMessage order);

	/// <summary>
	/// Try get and remove pending registration by transaction ID.
	/// </summary>
	bool TryGetAndRemovePendingRegistration(long transactionId, out OrderRegisterMessage order);

	/// <summary>
	/// Remove pending registration by value.
	/// </summary>
	void RemovePendingRegistrationByValue(OrderRegisterMessage order);

	/// <summary>
	/// Clear all state.
	/// </summary>
	void Clear();
}
