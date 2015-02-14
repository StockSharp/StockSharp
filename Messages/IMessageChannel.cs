namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Интерфейс, описывающий транспортный канал сообщений.
	/// </summary>
	public interface IMessageChannel
	{
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
}