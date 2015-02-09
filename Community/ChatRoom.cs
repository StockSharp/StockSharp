namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Комната.
	/// </summary>
	[DataContract]
	public class ChatRoom
	{
		/// <summary>
		/// Идентификатор.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Название.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Описание комнаты.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Доступна ли для всех или требуется подтверждение.
		/// </summary>
		[DataMember]
		public bool IsEveryOne { get; set; }

		/// <summary>
		/// Идентификатор родительской комнаты.
		/// </summary>
		[DataMember]
		public long? ParentRoomId { get; set; }

		/// <summary>
		/// Дата создания.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}