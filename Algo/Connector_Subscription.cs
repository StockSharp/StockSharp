namespace StockSharp.Algo;

partial class Connector
{
	/// <inheritdoc />
	public IEnumerable<Subscription> Subscriptions => _subscriptionManager.Subscriptions;

	/// <inheritdoc />
	public void Subscribe(Subscription subscription) => _subscriptionManager.Subscribe(subscription);

	/// <inheritdoc />
	public void UnSubscribe(Subscription subscription) => _subscriptionManager.UnSubscribe(subscription);
}