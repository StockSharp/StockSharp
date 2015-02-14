namespace StockSharp.Messages
{
	/// <summary>
	/// Сообщение очистки очереди обработки.
	/// </summary>
	public class ClearMessageQueueMessage : Message
	{
		/// <summary>
		/// Тип сообщений.
		/// </summary>
		public MessageTypes MessageTypes { get; set; }

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Дополнительный аргумент для фильтра маркет-данных.
		/// </summary>
		public object Arg { get; set; }

		/// <summary>
		/// Инициализировать <see cref="ClearMessageQueueMessage"/>.
		/// </summary>
		public ClearMessageQueueMessage()
			: base(MessageTypes.ClearMessageQueue)
		{
		}
	}
}
