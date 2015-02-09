namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Пользователь.
	/// </summary>
	[DataContract]
	public class User
	{
		/// <summary>
		/// Идентификатор.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Имя.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Детальное описание.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Дата регистрации.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}