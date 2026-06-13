namespace StockSharp.Algo.Strategies.Decomposed;

/// <summary>
/// Infrastructure abstraction replacing direct <see cref="Connector"/> dependency.
/// </summary>
public interface IStrategyHost
{
	/// <summary>
	/// Current UTC time.
	/// </summary>
	DateTime CurrentTime { get; }

	/// <summary>
	/// Strategy identifier.
	/// </summary>
	string StrategyId { get; }

	/// <summary>
	/// Whether strategy positions exist.
	/// </summary>
	bool HasPositions { get; }

	/// <summary>
	/// Whether PnL refresh is allowed at the specified time.
	/// </summary>
	/// <param name="time">Refresh time.</param>
	bool CanRefreshPnL(DateTime time);

	/// <summary>
	/// Send outgoing message (state change, order, etc.).
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Get next transaction ID for subscriptions.
	/// </summary>
	long GetNextTransactionId();
}
