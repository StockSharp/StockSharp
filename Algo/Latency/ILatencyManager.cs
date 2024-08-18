namespace StockSharp.Algo.Latency;

/// <summary>
/// The interface of the order registration delay calculation manager.
/// </summary>
public interface ILatencyManager : IPersistable
{
	/// <summary>
	/// To zero calculations.
	/// </summary>
	void Reset();

	/// <summary>
	/// The aggregate value of registration delay by all orders.
	/// </summary>
	TimeSpan LatencyRegistration { get; }

	/// <summary>
	/// The aggregate value of cancelling delay by all orders.
	/// </summary>
	TimeSpan LatencyCancellation { get; }

	/// <summary>
	/// To process the message for transaction delay calculation. Messages of <see cref="OrderRegisterMessage"/>, <see cref="OrderReplaceMessage"/>, <see cref="OrderCancelMessage"/> and <see cref="ExecutionMessage"/> types are accepted.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>The transaction delay. If it is impossible to calculate delay, <see langword="null" /> will be returned.</returns>
	TimeSpan? ProcessMessage(Message message);
}