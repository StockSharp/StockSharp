namespace StockSharp.Algo.Positions;

/// <summary>
/// The interface for the position calculation manager.
/// </summary>
public interface IPositionManager : IPersistable
{
	/// <summary>
	/// To calculate position.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>The position by order or trade.</returns>
	PositionChangeMessage ProcessMessage(Message message);
}