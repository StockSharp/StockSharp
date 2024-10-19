namespace StockSharp.Messages;

/// <summary>
/// The interfaces described message queue.
/// </summary>
public interface IMessageQueue : IBlockingQueue<(long sort, Message elem)>
{
	/// <summary>
	/// Enqueue the specified message.
	/// </summary>
	/// <param name="message">Message.</param>
	void Enqueue(Message message);

	/// <summary>
	/// Try dequeue a message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="exitOnClose">Exit from method if the queue closed.</param>
	/// <param name="block">Block the operation.</param>
	/// <returns>Operation result.</returns>
	bool TryDequeue(out Message message, bool exitOnClose = true, bool block = true);
}

/// <summary>
/// Message queue.
/// </summary>
public abstract class BaseMessageQueue : BaseOrderedBlockingQueue<long, Message, Ecng.Collections.PriorityQueue<long, Message>>, IMessageQueue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseMessageQueue"/>.
	/// </summary>
	protected BaseMessageQueue()
		: base(new((p1, p2) => (p1 - p2).Abs()))
	{
	}

	/// <summary>
	/// Enqueue message.
	/// </summary>
	/// <param name="message"><see cref="Message"/>.</param>
	public abstract void Enqueue(Message message);
}

/// <summary>
/// Sorted by <see cref="Message.LocalTime"/> queue.
/// </summary>
public class MessageByLocalTimeQueue : BaseMessageQueue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessageByLocalTimeQueue"/>.
	/// </summary>
	public MessageByLocalTimeQueue()
	{
	}

	/// <inheritdoc />
	public override void Enqueue(Message message) => Enqueue((message.LocalTime.UtcTicks, message), message.Forced);
}

/// <summary>
/// Sorted by incoming order queue.
/// </summary>
public class MessageByOrderQueue : BaseMessageQueue
{
	private readonly IdGenerator _idGen = new IncrementalIdGenerator();

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageByOrderQueue"/>.
	/// </summary>
	public MessageByOrderQueue()
	{
	}

	/// <inheritdoc />
	public override void Enqueue(Message message) => Enqueue(_idGen.GetNextId(), message);
}