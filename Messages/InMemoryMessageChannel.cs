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

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Message channel, based on the queue and operate within a single process.
	/// </summary>
	public class InMemoryMessageChannel : BaseInMemoryChannel<KeyValuePair<long, Message>>, IMessageChannel
	{
		private static readonly MemoryStatisticsValue<Message> _msgStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Messages);

		static InMemoryMessageChannel()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private readonly Action<Exception> _errorHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <param name="name">Channel name.</param>
		/// <param name="errorHandler">Error handler.</param>
		public InMemoryMessageChannel(string name, Action<Exception> errorHandler)
			: base(new MessagePriorityQueue(), name, errorHandler)
		{
			_errorHandler = errorHandler;
		}

		/// <summary>
		/// Message queue count.
		/// </summary>
		public int MessageCount => Count;

		/// <summary>
		/// Max message queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get => MaxCount;
			set => MaxCount = value;
		}

		/// <inheritdoc />
		protected override void OnNewOut(KeyValuePair<long, Message> item)
		{
			var message = item.Value;

			_msgStat.Remove(message);
			NewOutMessage?.Invoke(message);
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
			SendIn(new KeyValuePair<long, Message>(message.LocalTime.UtcTicks, message));
		}

		/// <summary>
		/// New message event.
		/// </summary>
		public event Action<Message> NewOutMessage;

		/// <summary>
		/// Create a copy of <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual IMessageChannel Clone()
		{
			return new InMemoryMessageChannel(Name, _errorHandler) { MaxMessageCount = MaxMessageCount };
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}