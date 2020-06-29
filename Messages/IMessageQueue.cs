namespace StockSharp.Messages
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// The interfaces described message queue.
	/// </summary>
	public interface IMessageQueue : IBlockingQueue<KeyValuePair<long, Message>>
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
	public abstract class BaseMessageQueue :
		BaseBlockingQueue<KeyValuePair<long, Message>,
		OrderedPriorityQueue<long, Message>>,
		IMessageQueue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseMessageQueue"/>.
		/// </summary>
		protected BaseMessageQueue()
			: base(new OrderedPriorityQueue<long, Message>())
		{
		}

		/// <inheritdoc />
		public bool TryDequeue(out Message message, bool exitOnClose = true, bool block = true)
		{
			if (TryDequeue(out KeyValuePair<long, Message> pair, exitOnClose, block))
			{
				message = pair.Value;
				return true;
			}

			message = null;
			return false;
		}

		/// <inheritdoc />
		public abstract void Enqueue(Message message);

		/// <summary>
		/// Add new message.
		/// </summary>
		/// <param name="sort">Sort order.</param>
		/// <param name="message">Message.</param>
		protected void Enqueue(long sort, Message message)
		{
			Enqueue(new KeyValuePair<long, Message>(sort, message));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="force"></param>
		protected override void OnEnqueue(KeyValuePair<long, Message> item, bool force)
		{
			InnerCollection.Enqueue(item.Key, item.Value);
		}

		/// <summary>
		/// Dequeue the next element.
		/// </summary>
		/// <returns>The next element.</returns>
		protected override KeyValuePair<long, Message> OnDequeue()
		{
			return InnerCollection.Dequeue();
		}

		/// <summary>
		/// To get from top the current element.
		/// </summary>
		/// <returns>The current element.</returns>
		protected override KeyValuePair<long, Message> OnPeek()
		{
			return InnerCollection.Peek();
		}
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
		public override void Enqueue(Message message) => Enqueue(message.LocalTime.UtcTicks, message);
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
}