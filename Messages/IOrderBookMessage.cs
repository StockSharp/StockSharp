namespace StockSharp.Messages;

/// <summary>
/// Interface describes an order book message.
/// </summary>
public interface IOrderBookMessage :
	ISecurityIdMessage, ISeqNumMessage, IServerTimeMessage,
	IGeneratedMessage, ICurrencyMessage, ILocalTimeMessage,
	ICloneable
{
	/// <summary>
	/// Quotes to buy.
	/// </summary>
	QuoteChange[] Bids { get; }

	/// <summary>
	/// Quotes to sell.
	/// </summary>
	QuoteChange[] Asks { get; }

	/// <summary>
	/// Order book state.
	/// </summary>
	QuoteChangeStates? State { get; set; }
}
