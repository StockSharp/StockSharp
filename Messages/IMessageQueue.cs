namespace StockSharp.Messages;

using Ecng.Collections;

/// <summary>
/// Describes a message queue with asynchronous semantics.
/// </summary>
public interface IMessageQueue
{
	/// <summary>
	/// Gets the current number of messages buffered in the queue.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Gets a value indicating whether the queue is closed.
	/// </summary>
	bool IsClosed { get; }

	/// <summary>
	/// Opens the queue and allows enqueue and dequeue operations.
	/// </summary>
	void Open();

	/// <summary>
	/// Closes the queue and prevents new messages from being enqueued.
	/// </summary>
	void Close();

	/// <summary>
	/// Enqueues the specified message.
	/// </summary>
	/// <param name="message">The message to enqueue.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask Enqueue(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Removes and returns the next message from the queue asynchronously.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The next message in the queue.</returns>
	ValueTask<Message> DequeueAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Returns an async enumerable that yields messages from the queue.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of messages.</returns>
	IAsyncEnumerable<Message> ReadAllAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Removes all messages from the queue.
	/// </summary>
	void Clear();
}

/// <summary>
/// Base implementation of <see cref="IMessageQueue"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseMessageQueue"/> class.
/// </remarks>
public abstract class BaseMessageQueue() : BaseOrderedChannel<long, Message, PriorityQueue<long, Message>>(new((p1, p2) => (p1 - p2).Abs()), -1), IMessageQueue
{
	/// <inheritdoc />
	public abstract ValueTask Enqueue(Message message, CancellationToken cancellationToken);
}

/// <summary>
/// Message queue that is sorted by <see cref="Message.LocalTime"/>.
/// </summary>
public class MessageByLocalTimeQueue : BaseMessageQueue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessageByLocalTimeQueue"/> class.
	/// </summary>
	public MessageByLocalTimeQueue()
	{
	}

	/// <inheritdoc />
	public override ValueTask Enqueue(Message message, CancellationToken cancellationToken)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return Enqueue(message.LocalTime.Ticks, message, cancellationToken);
	}
}

/// <summary>
/// Message queue that is sorted by the order in which messages are enqueued.
/// </summary>
public class MessageByOrderQueue : BaseMessageQueue
{
	private readonly IncrementalIdGenerator _idGen = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageByOrderQueue"/> class.
	/// </summary>
	public MessageByOrderQueue()
	{
	}

	/// <inheritdoc />
	public override ValueTask Enqueue(Message message, CancellationToken cancellationToken)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return Enqueue(_idGen.GetNextId(), message, cancellationToken);
	}
}