namespace StockSharp.Messages;

/// <summary>
/// The interface describing a message with <see cref="ClientCode"/> property.
/// </summary>
public interface IClientCodeMessage
{
	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	string ClientCode { get; set; }
}
