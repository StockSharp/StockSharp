namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее данные о новости.
	/// </summary>
	[Serializable]
	[DataContract]
	public class NewsMessage : Message
	{
		/// <summary>
		/// Номер новости.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.IdKey)]
		[DescriptionLoc(LocalizedStrings.NewsIdKey)]
		[MainCategory]
		//[Identity]
		public string Id { get; set; }

		/// <summary>
		/// Код электронной площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Идентификатор инструмента, для которого опубликована новость.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str212Key)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Источник новости.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str213Key)]
		[DescriptionLoc(LocalizedStrings.Str214Key)]
		[MainCategory]
		public string Source { get; set; }

		/// <summary>
		/// Заголовок.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str215Key)]
		[DescriptionLoc(LocalizedStrings.Str215Key, true)]
		[MainCategory]
		public string Headline { get; set; }

		/// <summary>
		/// Текст новости.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str217Key)]
		[DescriptionLoc(LocalizedStrings.Str218Key)]
		[MainCategory]
		public string Story { get; set; }

		/// <summary>
		/// Время появления новости.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str219Key)]
		[DescriptionLoc(LocalizedStrings.Str220Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Ссылка на новость в интернете.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str221Key)]
		[DescriptionLoc(LocalizedStrings.Str222Key)]
		[MainCategory]
		public Uri Url { get; set; }

		/// <summary>
		/// Создать <see cref="NewsMessage"/>.
		/// </summary>
		public NewsMessage()
			: base(MessageTypes.News)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0},Head={1}".Put(SecurityId, Headline);
		}

		/// <summary>
		/// Создать копию объекта <see cref="NewsMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new NewsMessage
			{
				LocalTime = LocalTime,
				ServerTime = ServerTime,
				SecurityId = SecurityId,
				BoardCode = BoardCode,
				Headline = Headline,
				Id = Id,
				Source = Source,
				Story = Story,
				Url = Url
			};
		}
	}
}