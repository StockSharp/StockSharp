namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;

	using MessageItem = System.Tuple<Messages.Message, Messages.IMessageAdapter>;
	using Pair = System.Collections.Generic.KeyValuePair<System.DateTime, System.Tuple<Messages.Message, Messages.IMessageAdapter>>;
	using StockSharp.Localization;

	/// <summary>
	/// Обработчик сообщений.
	/// </summary>
	public class MessageProcessor : IMessageProcessor
	{
		private static readonly MemoryStatisticsValue<Message> _msgStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Messages);

		static MessageProcessor()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private sealed class BlockingPriorityQueue : BaseBlockingQueue<Pair, OrderedPriorityQueue<DateTime, MessageItem>>
		{
			public BlockingPriorityQueue()
				: base(new OrderedPriorityQueue<DateTime, MessageItem>())
			{
			}

			protected override void OnEnqueue(Pair item, bool force)
			{
				InnerCollection.Enqueue(item.Key, item.Value);
			}

			protected override Pair OnDequeue()
			{
				return InnerCollection.Dequeue();
			}

			protected override Pair OnPeek()
			{
				return InnerCollection.Peek();
			}

			public void Clear(ClearMessageQueueMessage message)
			{
				lock (SyncRoot)
				{
					IEnumerable<Pair> messages;

					switch (message.MessageTypes)
					{
						case MessageTypes.Execution:
							messages = InnerCollection
								.Where(m =>
								{
									if(m.Value.Item1.Type != MessageTypes.Execution)
										return false;

									var execMsg = (ExecutionMessage)m.Value.Item1;

									return execMsg.SecurityId == message.SecurityId && (message.Arg == null || message.Arg.Compare(execMsg.ExecutionType) == 0);
								});
							break;

						case MessageTypes.QuoteChange:
							messages = InnerCollection.Where(m => m.Value.Item1.Type == MessageTypes.QuoteChange && ((QuoteChangeMessage)m.Value.Item1).SecurityId == message.SecurityId);
							break;

						case MessageTypes.Level1Change:
							messages = InnerCollection.Where(m => m.Value.Item1.Type == MessageTypes.Level1Change && ((Level1ChangeMessage)m.Value.Item1).SecurityId == message.SecurityId);
							break;

						default:
							return;
					}

					InnerCollection.RemoveRange(messages);
				}
			}
		}

		private readonly Action<Exception> _errorHandler;
		private readonly BlockingPriorityQueue _messageQueue = new BlockingPriorityQueue();

		/// <summary>
		/// Создать <see cref="MessageProcessor"/>.
		/// </summary>
		/// <param name="name">Название обработчика.</param>
		/// <param name="errorHandler">Обработчик ошибок.</param>
		public MessageProcessor(string name, Action<Exception> errorHandler)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			Name = name;
			_errorHandler = errorHandler;
			_messageQueue.Close();
		}

		/// <summary>
		/// Название обработчика.
		/// </summary>
		public string Name { get; private set; }

		int IMessageProcessor.MessageCount
		{
			get { return _messageQueue.Count; }
		}

		int IMessageProcessor.MaxMessageCount
		{
			get { return _messageQueue.MaxSize; }
			set { _messageQueue.MaxSize = value; }
		}

		bool IMessageProcessor.IsStarted
		{
			get { return !_messageQueue.IsClosed; }
		}

		private Action<Message, IMessageAdapter> _newMessage;

		event Action<Message, IMessageAdapter> IMessageProcessor.NewMessage
		{
			add { _newMessage += value; }
			remove { _newMessage -= value; }
		}

		private Action _stopped;

		event Action IMessageProcessor.Stopped
		{
			add { _stopped += value; }
			remove { _stopped -= value; }
		}

		void IMessageProcessor.Start()
		{
			_messageQueue.Open();

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					while (!_messageQueue.IsClosed)
					{
						try
						{
							MessageItem item;

							if (!TryDequeue(out item))
								break;

							_newMessage.SafeInvoke(item.Item1, item.Item2);
						}
						catch (Exception ex)
						{
							_errorHandler(ex);
						}
					}

					_stopped.SafeInvoke();
				}))
				.Name("{0}. Messages thread.".Put(Name))
				//.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		void IMessageProcessor.Stop()
		{
			_messageQueue.Close();
		}

		void IMessageProcessor.Clear(ClearMessageQueueMessage message)
		{
			_messageQueue.Clear(message);
		}

		void IMessageProcessor.EnqueueMessage(Message message, IMessageAdapter adapter, bool force)
		{
			_msgStat.Add(message);
			_messageQueue.Enqueue(new Pair(message.LocalTime, new MessageItem(message, adapter)), force);
		}

		private bool TryDequeue(out MessageItem item)
		{
			Pair pair;

			if (!_messageQueue.TryDequeue(out pair))
			{
				item = null;
				return false;
			}

			_msgStat.Remove(pair.Value.Item1);

			item = pair.Value;
			return true;
		}
	}
}