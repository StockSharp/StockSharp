namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Интерфейс, описывающий транспортный канал сообщений.
	/// </summary>
	public interface IMessageChannel : IDisposable
	{
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
}