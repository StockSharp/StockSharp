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
		/// Тип сообщений.
		/// </summary>
		public MessageTypes MessageTypes { get; set; }

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		public SecurityId SecurityId { get; set; }

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
