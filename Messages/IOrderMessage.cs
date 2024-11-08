namespace StockSharp.Messages;

/// <summary>
/// Interface describes an order's state message.
/// </summary>
public interface IOrderMessage :
	ISecurityIdMessage, ILocalTimeMessage, IServerTimeMessage, IComplexIdMessage,
	IGeneratedMessage, ISeqNumMessage, ICurrencyMessage, ISystemMessage
{
	/// <summary>
	/// <see cref="Sides"/>
	/// </summary>
	Sides Side { get; }

	/// <summary>
	/// Order state.
	/// </summary>
	OrderStates? State { get; }

	/// <summary>
	/// <see cref="TimeInForce"/>
	/// </summary>
	TimeInForce? TimeInForce { get; }

	/// <summary>
	/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
	/// </summary>
	/// <remarks>
	/// If the value is equal <see langword="null" />, order will be GTC (good til cancel). Or uses exact date.
	/// </remarks>
	DateTimeOffset? ExpiryDate { get; set; }

	/// <summary>
	/// <see cref="Type"/>
	/// </summary>
	OrderTypes? Type { get; }

	/// <summary>
	/// Order contracts balance.
	/// </summary>
	decimal? Balance { get; }

	/// <summary>
	/// Order price.
	/// </summary>
	decimal Price { get; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	decimal? Volume { get; }
}