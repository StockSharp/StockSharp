namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Сообщение очистки очереди обработки.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ClearMessageQueueMessage : Message
	{
		/// <summary>
		/// Тип сообщений, которые необходимо удалить.
		/// Если значение равно <see langword="null"/>, то необходимо удалить все сообщения.
		/// </summary>
		public MessageTypes? ClearMessageType { get; set; }

		/// <summary>
		/// Идентификатор инструмента.
		/// Если значение равно <see langword="null"/>, то необходимо удалить сообщения для всех инструментов.
		/// </summary>
		[DataMember]
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// Дополнительный аргумент для фильтра маркет-данных.
		/// </summary>
		[DataMember]
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
