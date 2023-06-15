namespace StockSharp.Messages;

/// <summary>
/// Interface describes an order log message.
/// </summary>
public interface IOrderLogMessage
{
	/// <summary>
	/// <see cref="IOrderMessage"/>
	/// </summary>
	IOrderMessage Order { get; }

	/// <summary>
	/// <see cref="ITickTradeMessage"/>
	/// </summary>
	ITickTradeMessage Trade { get; }
}