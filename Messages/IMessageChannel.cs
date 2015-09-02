namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Message channel base interface.
	/// </summary>
	public interface IMessageChannel : IDisposable
	{
		/// <summary>
		/// Is channel opened.
		/// </summary>
		bool IsOpened { get; }

		/// <summary>
		/// Open channel.
		/// </summary>
		void Open();

		/// <summary>
		/// Close channel.
		/// </summary>
		void Close();

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendInMessage(Message message);

		/// <summary>
		/// New message event.
		/// </summary>
		event Action<Message> NewOutMessage;
	}

	/// <summary>
	/// Message channel, which passes directly to the output all incoming messages.
	/// </summary>
	public class PassThroughMessageChannel : IMessageChannel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PassThroughMessageChannel"/>.
		/// </summary>
		public PassThroughMessageChannel()
		{
		}

		void IDisposable.Dispose()
		{
		}

		bool IMessageChannel.IsOpened
		{
			get { return true; }
		}

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		void IMessageChannel.SendInMessage(Message message)
		{
			_newMessage.SafeInvoke(message);
		}

		private Action<Message> _newMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add { _newMessage += value; }
			remove { _newMessage -= value; }
		}
	}
}