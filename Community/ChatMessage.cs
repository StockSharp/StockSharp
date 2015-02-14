namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Сообщение.
	/// </summary>
	[DataContract]
	public class ChatMessage
	{
		/// <summary>
		/// Идентификатор.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Идентификатор автора.
		/// </summary>
		[DataMember]
		public long AuthorId { get; set; }

		/// <summary>
		/// Дата создания.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// Тело сообщения.
		/// </summary>
		[DataMember]
		public string Body { get; set; }

		/// <summary>
		/// Идентификатор комнаты.
		/// </summary>
		[DataMember]
		public long RoomId { get; set; }
	}
}