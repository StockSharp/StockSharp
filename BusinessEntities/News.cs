namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Новость.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str395Key)]
	[DescriptionLoc(LocalizedStrings.Str510Key)]
	public class News : IExtendableEntity
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
		/// Биржевая площадка, для которой опубликована новость.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str511Key)]
		[DescriptionLoc(LocalizedStrings.Str512Key)]
		[MainCategory]
		public ExchangeBoard Board { get; set; }

		/// <summary>
		/// Инструмент, для которого опубликована новость.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str513Key)]
		[MainCategory]
		public Security Security { get; set; }

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
		/// Локальное время получения новости.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str514Key)]
		[DescriptionLoc(LocalizedStrings.Str515Key)]
		[MainCategory]
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// Ссылка на новость в интернете.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str221Key)]
		[DescriptionLoc(LocalizedStrings.Str222Key)]
		[MainCategory]
		[Url]
		public Uri Url { get; set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}
	}
}