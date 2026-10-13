namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Action recommended by the position modify engine.
/// </summary>
public class PositionModifyAction
{
	/// <summary>
	/// Types of actions.
	/// </summary>
	public enum ActionTypes
	{
		/// <summary>
		/// No action needed.
		/// </summary>
		None,

		/// <summary>
		/// Register a new order.
		/// </summary>
		Register,

		/// <summary>
		/// Cancel current order.
		/// </summary>
		Cancel,

		/// <summary>
		/// Algorithm is finished.
		/// </summary>
		Finished,
	}

	/// <summary>
	/// Type of action to take.
	/// </summary>
	public ActionTypes ActionType { get; init; }

	/// <summary>
	/// Order side.
	/// </summary>
	public Sides? Side { get; init; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal? Volume { get; init; }

	/// <summary>
	/// Order price.
	/// </summary>
	public decimal? Price { get; init; }

	/// <summary>
	/// Order type.
	/// </summary>
	public OrderTypes? OrderType { get; init; }

	/// <summary>
	/// Create "no action" result.
	/// </summary>
	public static PositionModifyAction None() => new() { ActionType = ActionTypes.None };

	/// <summary>
	/// Create "register order" result.
	/// </summary>
	public static PositionModifyAction Register(Sides side, decimal volume, decimal? price, OrderTypes orderType)
		=> new() { ActionType = ActionTypes.Register, Side = side, Volume = volume, Price = price, OrderType = orderType };

	/// <summary>
	/// Create "cancel order" result.
	/// </summary>
	public static PositionModifyAction CancelOrder() => new() { ActionType = ActionTypes.Cancel };

	/// <summary>
	/// Create "finished" result.
	/// </summary>
	public static PositionModifyAction Finished() => new() { ActionType = ActionTypes.Finished };
}

/// <summary>
/// Interface for position modify algorithms (detached from Diagram).
/// </summary>
public interface IPositionModifyAlgo : IDisposable
{
	/// <summary>
	/// Update market data.
	/// </summary>
	/// <param name="time">Current time.</param>
	/// <param name="price">Last trade price.</param>
	/// <param name="volume">Last trade volume.</param>
	void UpdateMarketData(DateTime time, decimal? price, decimal? volume);

	/// <summary>
	/// Update order book data.
	/// </summary>
	/// <param name="depth">Order book.</param>
	void UpdateOrderBook(IOrderBookMessage depth);

	/// <summary>
	/// Get the next action to take. The call advances the algorithm: a returned
	/// <see cref="PositionModifyAction.ActionTypes.Register"/> counts as dispatched, so the caller
	/// places it and reports back through <see cref="OnOrderMatched"/>, <see cref="OnOrderFailed"/>
	/// or <see cref="OnOrderCanceled"/>. An action that is discarded leaves the algorithm waiting
	/// for a report that never comes.
	/// </summary>
	PositionModifyAction GetNextAction();

	/// <summary>
	/// Notify that an order was matched.
	/// </summary>
	/// <param name="matchedVolume">Volume that traded since the previous notification for this order: a caller
	/// reporting every partial fill passes the increment, one reporting only the end passes the whole traded
	/// volume. The same volume is never reported twice.</param>
	void OnOrderMatched(decimal matchedVolume);

	/// <summary>
	/// Notify that an order failed. Nothing traded.
	/// </summary>
	void OnOrderFailed();

	/// <summary>
	/// Notify that an order was canceled.
	/// </summary>
	/// <param name="matchedVolume">Volume that traded since the previous notification and before cancellation.</param>
	void OnOrderCanceled(decimal matchedVolume);

	/// <summary>
	/// Cancel the current algo execution.
	/// </summary>
	void Cancel();

	/// <summary>
	/// Remaining volume to execute.
	/// </summary>
	decimal RemainingVolume { get; }

	/// <summary>
	/// Whether the algorithm has finished.
	/// </summary>
	bool IsFinished { get; }
}
