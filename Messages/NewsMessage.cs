#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: NewsMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// News priorities.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum NewsPriorities
	{
		/// <summary>
		/// Low.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LowKey)]
		Low,

		/// <summary>
		/// Regular.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1629Key)]
		Regular,

		/// <summary>
		/// High.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HighKey)]
		High,
	}

	/// <summary>
	/// The message contains information about the news.
	/// </summary>
	[Serializable]
	[DataContract]
	public class NewsMessage : BaseSubscriptionIdMessage, IServerTimeMessage
	{
		/// <summary>
		/// News ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.IdKey)]
		[DescriptionLoc(LocalizedStrings.NewsIdKey)]
		[MainCategory]
		//[Identity]
		public string Id { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Security ID, for which news have been published.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str212Key)]
		[MainCategory]
		[Ecng.Serialization.Nullable]
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// News source.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str213Key)]
		[DescriptionLoc(LocalizedStrings.Str214Key)]
		[MainCategory]
		public string Source { get; set; }

		/// <summary>
		/// Header.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str215Key)]
		[DescriptionLoc(LocalizedStrings.Str215Key, true)]
		[MainCategory]
		public string Headline { get; set; }

		/// <summary>
		/// News text.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str217Key)]
		[DescriptionLoc(LocalizedStrings.Str218Key)]
		[MainCategory]
		public string Story { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TimeKey)]
		[DescriptionLoc(LocalizedStrings.Str220Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// News link in the internet.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str221Key)]
		[DescriptionLoc(LocalizedStrings.Str222Key)]
		[MainCategory]
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
		[Ecng.Serialization.Nullable]
		public NewsPriorities? Priority { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NewsMessage"/>.
		/// </summary>
		public NewsMessage()
			: base(MessageTypes.News)
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Sec={SecurityId},Head={Headline}";
		}

		/// <summary>
		/// Create a copy of <see cref="NewsMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new NewsMessage
			{
				Id = Id,
				BoardCode = BoardCode,
				SecurityId = SecurityId,
				Source = Source,
				Headline = Headline,
				Story = Story,
				ServerTime = ServerTime,
				Url = Url,
				Priority = Priority,
			};

			CopyTo(clone);

			return clone;
		}
	}
}