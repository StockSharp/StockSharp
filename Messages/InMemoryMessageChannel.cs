#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: InMemoryMessageChannel.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		private class BlockingPriorityQueue : BaseBlockingQueue<KeyValuePair<DateTimeOffset, Message>, OrderedPriorityQueue<DateTimeOffset, Message>>
		{
			public BlockingPriorityQueue()
				: base(new OrderedPriorityQueue<DateTimeOffset, Message>())
			{
			}

			protected override void OnEnqueue(KeyValuePair<DateTimeOffset, Message> item, bool force)
			{
				InnerCollection.Enqueue(item.Key, item.Value);
			}

			protected override KeyValuePair<DateTimeOffset, Message> OnDequeue()
			{
				return InnerCollection.Dequeue();
			}

			protected override KeyValuePair<DateTimeOffset, Message> OnPeek()
			{
				return InnerCollection.Peek();
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
							KeyValuePair<DateTimeOffset, Message> pair;

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

			//if (!(message is TimeMessage) && message.GetType().Name != "BasketMessage")
			//	Console.WriteLine(">> ({0}) {1}", System.Threading.Thread.CurrentThread.Name, message);

			_msgStat.Add(message);
			_messageQueue.Enqueue(new KeyValuePair<DateTimeOffset, Message>(message.LocalTime, message));
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