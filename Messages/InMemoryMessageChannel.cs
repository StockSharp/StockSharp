namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Message channel, based on the queue and operate within a single process.
	/// </summary>
	public class InMemoryMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
	{
		private class BlockingPriorityQueue : BaseBlockingQueue<KeyValuePair<DateTime, Message>, OrderedPriorityQueue<DateTime, Message>>
		{
			public BlockingPriorityQueue()
				: base(new OrderedPriorityQueue<DateTime, Message>())
			{
			}

			protected override void OnEnqueue(KeyValuePair<DateTime, Message> item, bool force)
			{
				InnerCollection.Enqueue(item.Key, item.Value);
			}

			protected override KeyValuePair<DateTime, Message> OnDequeue()
			{
				return InnerCollection.Dequeue();
			}

			protected override KeyValuePair<DateTime, Message> OnPeek()
			{
				return InnerCollection.Peek();
			}

			public void Clear(ClearQueueMessage message)
			{
				lock (SyncRoot)
				{
					switch (message.ClearMessageType)
					{
						case MessageTypes.Execution:
							InnerCollection
								.RemoveWhere(m =>
								{
									if (m.Value.Type != MessageTypes.Execution)
										return false;

									var execMsg = (ExecutionMessage)m.Value;

									return (message.SecurityId == null || execMsg.SecurityId == message.SecurityId) && (message.Arg == null || message.Arg.Compare(execMsg.ExecutionType) == 0);
								});

							break;

						case MessageTypes.QuoteChange:
							InnerCollection.RemoveWhere(m => m.Value.Type == MessageTypes.QuoteChange && (message.SecurityId == null || ((QuoteChangeMessage)m.Value).SecurityId == message.SecurityId));
							break;

						case MessageTypes.Level1Change:
							InnerCollection.RemoveWhere(m => m.Value.Type == MessageTypes.Level1Change && (message.SecurityId == null || ((Level1ChangeMessage)m.Value).SecurityId == message.SecurityId));
							break;

						case null:
							InnerCollection.Clear();
							break;
					}
				}
			}
		}

		private static readonly MemoryStatisticsValue<Message> _msgStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Messages);

		static InMemoryMessageChannel()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private readonly Action<Exception> _errorHandler;
		private readonly BlockingPriorityQueue _messageQueue = new BlockingPriorityQueue();

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <param name="name">Channel name.</param>
		/// <param name="errorHandler">Error handler.</param>
		public InMemoryMessageChannel(string name, Action<Exception> errorHandler)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			Name = name;

			_errorHandler = errorHandler;
			_messageQueue.Close();
		}

		/// <summary>
		/// Handler name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Message queue count.
		/// </summary>
		public int MessageCount => _messageQueue.Count;

		/// <summary>
		/// Max message queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get { return _messageQueue.MaxSize; }
			set { _messageQueue.MaxSize = value; }
		}

		/// <summary>
		/// Channel closing event.
		/// </summary>
		public event Action Closed;

		/// <summary>
		/// Is channel opened.
		/// </summary>
		public bool IsOpened => !_messageQueue.IsClosed;

		/// <summary>
		/// Open channel.
		/// </summary>
		public void Open()
		{
			_messageQueue.Open();

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					while (!_messageQueue.IsClosed)
					{
						try
						{
							KeyValuePair<DateTime, Message> pair;

							if (!_messageQueue.TryDequeue(out pair))
							{
								break;
							}

							//if (!(message is TimeMessage) && message.GetType().Name != "BasketMessage")
							//	Console.WriteLine("<< ({0}) {1}", System.Threading.Thread.CurrentThread.Name, message);

							_msgStat.Remove(pair.Value);
							NewOutMessage.SafeInvoke(pair.Value);
						}
						catch (Exception ex)
						{
							_errorHandler(ex);
						}
					}

					Closed.SafeInvoke();
				}))
				.Name("{0} channel thread.".Put(Name))
				//.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		/// <summary>
		/// Close channel.
		/// </summary>
		public void Close()
		{
			_messageQueue.Close();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendInMessage(Message message)
		{
			if (!IsOpened)
				throw new InvalidOperationException();

			var clearMsg = message as ClearQueueMessage;

			if (clearMsg != null)
			{
				_messageQueue.Clear(clearMsg);
			}
			else
			{
				//if (!(message is TimeMessage) && message.GetType().Name != "BasketMessage")
				//	Console.WriteLine(">> ({0}) {1}", System.Threading.Thread.CurrentThread.Name, message);

				_msgStat.Add(message);
				_messageQueue.Enqueue(new KeyValuePair<DateTime, Message>(message.LocalTime, message));	
			}
		}

		/// <summary>
		/// New message event.
		/// </summary>
		public event Action<Message> NewOutMessage;

		void IDisposable.Dispose()
		{
			Close();
		}

		/// <summary>
		/// Create a copy of <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new InMemoryMessageChannel(Name, _errorHandler) { MaxMessageCount = MaxMessageCount };
		}
	}
}