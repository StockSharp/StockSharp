namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Интерфейс обработчика сообщений.
	/// </summary>
	public interface IMessageProcessor
	{
		/// <summary>
		/// <see langword="true"/>, если обработчик сообщений запущен, иначе <see langword="false"/>.
		/// </summary>
		bool IsStarted { get; }

		/// <summary>
		/// Количество сообщений в очереди.
		/// </summary>
		int MessageCount { get; }

		/// <summary>
		/// Максимальный размер очереди сообщений. 
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно -1, что соответствует размеру без ограничений.
		/// </remarks>
		int MaxMessageCount { get; set; }

		/// <summary>
		/// Событие обработки нового сообщения.
		/// </summary>
		event Action<Message, IMessageAdapter> NewMessage;

		/// <summary>
		/// Событие остановки обработчика.
		/// </summary>
		event Action Stopped;

		/// <summary>
		/// Добавить сообщение в очередь на обработку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="force">Добавить сообщения даже в случае превышения очереди размера <see cref="MaxMessageCount"/>.</param>
		void EnqueueMessage(Message message, IMessageAdapter adapter, bool force);

		/// <summary>
		/// Запустить обработку сообщений.
		/// </summary>
		void Start();

		/// <summary>
		/// Остановить обработку сообщений.
		/// </summary>
		void Stop();

		/// <summary>
		/// Очистить очередь сообщений по указанному фильтру.
		/// </summary>
		/// <param name="message">Фильтр.</param>
		void Clear(ClearMessageQueueMessage message);
	}
}