namespace StockSharp.Messages;

/// <summary>
/// Interface describes an order's state message.
/// </summary>
public interface IOrderMessage :
	ISecurityIdMessage, ILocalTimeMessage, IServerTimeMessage,
	IGeneratedMessage, ISeqNumMessage, ICurrencyMessage, ISystemMessage
{
	/// <summary>
	/// <see cref="Sides"/>
	/// </summary>
	Sides Side { get; }

	/// <summary>
	/// Order state.
	/// </summary>
	OrderStates State { get; }

	/// <summary>
	/// <see cref="TimeInForce"/>
	/// </summary>
	TimeInForce? TimeInForce { get; }

	/// <summary>
	/// <see cref="Type"/>
	/// </summary>
	OrderTypes? Type { get; }

	/// <summary>
	/// Order contracts balance.
	/// </summary>
	decimal Balance { get; }

	/// <summary>
	/// Order price.
	/// </summary>
	decimal Price { get; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	decimal Volume { get; }
}