namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Обработчик сообщений.
	/// </summary>
	public class MessageProcessor : IMessageProcessor
	{
		private readonly InMemoryMessageChannel _channel;

		/// <summary>
		/// Создать <see cref="MessageProcessor"/>.
		/// </summary>
		/// <param name="name">Название обработчика.</param>
		/// <param name="errorHandler">Обработчик ошибок.</param>
		public MessageProcessor(string name, Action<Exception> errorHandler)
		{
			_channel = new InMemoryMessageChannel(name, errorHandler);
			_channel.NewOutMessage += ChannelOnNewOutMessage;
		}

		private void ChannelOnNewOutMessage(Message message, IMessageAdapter adapter)
		{
			_newMessage.SafeInvoke(message, adapter);
		}

		int IMessageProcessor.MessageCount
		{
			get { return _channel.MessageCount; }
		}

		int IMessageProcessor.MaxMessageCount
		{
			get { return _channel.MaxMessageCount; }
			set { _channel.MaxMessageCount = value; }
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

		void IMessageProcessor.Start()
		{
			_channel.Open();
		}

		void IMessageProcessor.Stop()
		{
			_channel.Close();
		}

		void IMessageProcessor.Clear(ClearMessageQueueMessage message)
		{
			_messageQueue.Clear(message);
		}

		void IMessageProcessor.EnqueueMessage(Message message, IMessageAdapter adapter, bool force)
		{
			_channel.SendInMessage(message, adapter);
		}
	}
}