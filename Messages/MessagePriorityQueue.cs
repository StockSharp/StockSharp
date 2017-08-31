namespace StockSharp.Messages
{
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// Sorted by <see cref="Message.LocalTime"/> queue.
	/// </summary>
	public class MessagePriorityQueue : BaseBlockingQueue<KeyValuePair<long, Message>, OrderedPriorityQueue<long, Message>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePriorityQueue"/>.
		/// </summary>
		public MessagePriorityQueue()
			: base(new OrderedPriorityQueue<long, Message>())
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="exitOnClose"></param>
		/// <param name="block"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void Enqueue(Message message)
		{
			Enqueue(new KeyValuePair<long, Message>(message.LocalTime.UtcTicks, message));
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
}