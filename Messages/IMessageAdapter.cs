namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Типы адаптеров <see cref="IMessageAdapter"/>.
	/// </summary>
	public enum MessageAdapterTypes
	{
		/// <summary>
		/// Транзакционный.
		/// </summary>
		Transaction,

		/// <summary>
		/// Маркет-данные.
		/// </summary>
		MarketData,
	}

	/// <summary>
	/// Адаптер, конвертирующий сообщения <see cref="Message"/> в команды торговой системы и обратно.
	/// </summary>
	public interface IMessageAdapter : IDisposable, IMessageChannel
	{
		/// <summary>
		/// Тип адаптера.
		/// </summary>
		MessageAdapterTypes Type { get; }

		/// <summary>
		/// Контейнер для сессии.
		/// </summary>
		IMessageSessionHolder SessionHolder { get; }

		/// <summary>
		/// Добавить <see cref="Message"/> в исходящую очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		void SendOutMessage(Message message);

		/// <summary>
		/// Обработчик входящих сообщений.
		/// </summary>
		IMessageProcessor InMessageProcessor { get; set; }

		/// <summary>
		/// Обработчик исходящих сообщений.
		/// </summary>
		IMessageProcessor OutMessageProcessor { get; set; }
	}
}