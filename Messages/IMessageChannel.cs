namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Интерфейс, описывающий транспортный канал сообщений.
	/// </summary>
	public interface IMessageChannel : IDisposable
	{
		/// <summary>
		/// Открыт ли канал.
		/// </summary>
		bool IsOpened { get; }

		/// <summary>
		/// Открыть канал.
		/// </summary>
		void Open();

		/// <summary>
		/// Закрыть канал.
		/// </summary>
		void Close();

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		void SendInMessage(Message message);

		/// <summary>
		/// Событие появления нового сообщения.
		/// </summary>
		event Action<Message> NewOutMessage;
	}

	/// <summary>
	/// Транспортный канал сообщений, который передает сразу на выход все входящие сообщения.
	/// </summary>
	public class PassThroughMessageChannel : IMessageChannel
	{
		/// <summary>
		/// Создать <see cref="PassThroughMessageChannel"/>.
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