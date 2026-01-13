namespace StockSharp.Messages;

/// <summary>
/// Message transport interface.
/// </summary>
public interface IMessageTransport
{
	/// <summary>
	/// Processes a generic message asynchronously.
	/// </summary>
	/// <param name="message">The message to process.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// New message event.
	/// </summary>
	event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;
}
