namespace StockSharp.Messages;

/// <summary>
/// Interface describes an order's state message.
/// </summary>
public interface IOrderMessage
{
	/// <summary>
	/// Order state.
	/// </summary>
	OrderStates State { get; }

	/// <summary>
	/// Order contracts balance.
	/// </summary>
	decimal Balance { get; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	decimal Volume { get; }
}