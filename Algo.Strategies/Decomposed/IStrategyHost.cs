namespace StockSharp.Algo.Strategies.Decomposed;

/// <summary>
/// Infrastructure abstraction replacing direct <see cref="Connector"/> dependency.
/// </summary>
public interface IStrategyHost
{
	/// <summary>
	/// Current UTC time.
	/// </summary>
	DateTime CurrentTimeUtc { get; }

	/// <summary>
	/// Send outgoing message (state change, order, etc.).
	/// </summary>
	void SendOutMessage(Message message);

	/// <summary>
	/// Get next transaction ID for subscriptions.
	/// </summary>
	long GetNextTransactionId();
}
