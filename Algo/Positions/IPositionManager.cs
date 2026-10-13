namespace StockSharp.Algo.Positions;

/// <summary>
/// The interface for the position calculation manager.
/// </summary>
public interface IPositionManager : IAsyncPersistable
{
	/// <summary>
	/// Create a copy of the manager.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Copy.</returns>
	ValueTask<IPositionManager> CloneAsync(CancellationToken cancellationToken);

	/// <summary>
	/// To calculate position.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>The position by order or trade.</returns>
	PositionChangeMessage ProcessMessage(Message message);
}