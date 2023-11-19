#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: News.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// News.
	/// </summary>
	[Serializable]
	[DataContract]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NewsKey,
		Description = LocalizedStrings.NewsDescKey)]
	public class News : NotifiableObject
	{
		/// <summary>
		/// News ID.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IdKey,
			Description = LocalizedStrings.NewsIdKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Id { get; set; }

		/// <summary>
		/// Exchange board for which the news is published.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BoardKey,
			Description = LocalizedStrings.ElectronicBoardDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public ExchangeBoard Board { get; set; }

		/// <summary>
		/// Security, for which news have been published.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecurityKey,
			Description = LocalizedStrings.NewsSecurityKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public Security Security { get; set; }

		/// <summary>
		/// News source.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SourceKey,
			Description = LocalizedStrings.NewsSourceKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Source { get; set; }

		/// <summary>
		/// Header.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.HeaderKey,
			Description = LocalizedStrings.HeaderKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Headline { get; set; }

		private string _story;

		/// <summary>
		/// News text.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TextKey,
			Description = LocalizedStrings.NewsTextKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Story
		{
			get => _story;
			set
			{
				_story = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Time of news arrival.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TimeKey,
			Description = LocalizedStrings.NewsTimeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// News received local time.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LocalTimeKey,
			Description = LocalizedStrings.LocalTimeDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DateTimeOffset LocalTime { get; set; }

		/// <summary>
		/// News link in the internet.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LinkKey,
			Description = LocalizedStrings.NewsLinkKey,
			GroupName = LocalizedStrings.GeneralKey)]
		//[Url]
		public string Url { get; set; }

		/// <summary>
		/// News priority.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriorityKey,
			Description = LocalizedStrings.NewsPriorityKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public NewsPriorities? Priority { get; set; }

		/// <summary>
		/// Language.
		/// </summary>
		[DataMember]
		public string Language { get; set; }

		/// <summary>
		/// Expiration date.
		/// </summary>
		[DataMember]
		public DateTimeOffset? ExpiryDate { get; set; }

		/// <summary>
		/// Sequence number.
		/// </summary>
		/// <remarks>Zero means no information.</remarks>
		[DataMember]
		public long SeqNum { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{ServerTime} {Headline} {Story} {Source}";
		}
	}
}