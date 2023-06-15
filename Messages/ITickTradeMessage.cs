namespace StockSharp.Messages;

/// <summary>
/// Interface describes an tick trade message.
/// </summary>
public interface ITickTradeMessage :
	ISecurityIdMessage, ISeqNumMessage, ICurrencyMessage, ISystemMessage,
	IServerTimeMessage, IGeneratedMessage, ILocalTimeMessage
{
	/// <summary>
	/// Trade ID.
	/// </summary>
	long Id { get; }

	/// <summary>
	/// Trade ID (as string, if electronic board does not use numeric order ID representation).
	/// </summary>
	string StringId { get; }

	/// <summary>
	/// Trade price.
	/// </summary>
	decimal Price { get; }

	/// <summary>
	/// Number of contracts in the trade.
	/// </summary>
	decimal Volume { get; }

	/// <summary>
	/// Order side (buy or sell), which led to the trade.
	/// </summary>
	Sides? OriginSide { get; }

	/// <summary>
	/// Number of open positions (open interest).
	/// </summary>
	decimal? OpenInterest { get; }

	/// <summary>
	/// Is tick ascending or descending in price.
	/// </summary>
	bool? IsUpTick { get; }

	/// <summary>
	/// System trade status.
	/// </summary>
	int? Status { get; }

	/// <summary>
	/// Yield.
	/// </summary>
	decimal? Yield { get; }

	/// <summary>
	/// Order id (buy).
	/// </summary>
	long? OrderBuyId { get; }

	/// <summary>
	/// Order id (sell).
	/// </summary>
	long? OrderSellId { get; }
}