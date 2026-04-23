namespace StockSharp.Messages;

/// <summary>
/// The interface describing a message with <see cref="BrokerCode"/> property.
/// </summary>
public interface IBrokerCodeMessage
{
	/// <summary>
	/// Broker firm code.
	/// </summary>
	string BrokerCode { get; set; }
}
