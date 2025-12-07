namespace StockSharp.Messages;

/// <summary>
/// Describes an asynchronous message adapter capable of processing incoming messages
/// and performing common adapter operations asynchronously.
/// </summary>
public interface IAsyncMessageAdapter : IMessageAdapter
{
	/// <summary>
	/// Gets the timeout to wait for a graceful disconnect.
	/// </summary>
	TimeSpan DisconnectTimeout { get; }

	/// <summary>
	/// Gets or sets the maximum number of parallel (non-control) messages that can be processed.
	/// Must be greater than or equal to1.
	/// </summary>
	int MaxParallelMessages { get; set; }

	/// <summary>
	/// Gets or sets the delay applied between faulted iterations.
	/// </summary>
	TimeSpan FaultDelay { get; set; }

	/// <summary>
	/// Processes a generic message asynchronously.
	/// </summary>
	/// <param name="message">The message to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken);
}