namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Новость.
	/// </summary>
	[DataContract]
	public class CommunityNews
	{
		/// <summary>
		/// Новость <see cref="CommunityNews"/>.
		/// </summary>
		public CommunityNews()
		{
		}

		/// <summary>
		/// Идентификатор новости.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Частота обновления новости (в часах).
		/// </summary>
		[DataMember]
		public int Frequency { get; set; }

		/// <summary>
		/// Окончания новости.
		/// </summary>
		[DataMember]
		public DateTime EndDate { get; set; }

		/// <summary>
		/// Заголовок на английском языке.
		/// </summary>
		[DataMember]
		public string EnglishTitle { get; set; }

		/// <summary>
		/// Текст на английском языке.
		/// </summary>
		[DataMember]
		public string EnglishBody { get; set; }

		/// <summary>
		/// Заголовок на русском языке.
		/// </summary>
		[DataMember]
		public string RussianTitle { get; set; }

		/// <summary>
		/// Текст на русском языке.
		/// </summary>
		[DataMember]
		public string RussianBody { get; set; }
	}
}