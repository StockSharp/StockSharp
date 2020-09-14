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
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
	[DisplayNameLoc(LocalizedStrings.NewsKey)]
	public class NewsMessage : BaseSubscriptionIdMessage<NewsMessage>,
		IServerTimeMessage, INullableSecurityIdMessage, ITransactionIdMessage, ISeqNumMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

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

		/// <inheritdoc />
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
		/// Language.
		/// </summary>
		[DataMember]
		public string Language { get; set; }

		/// <summary>
		/// Expiration date.
		/// </summary>
		[DataMember]
		public DateTimeOffset? ExpiryDate { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.News;

		private long[] _attachments = ArrayHelper.Empty<long>();

		/// <summary>
		/// Attachments.
		/// </summary>
		[DataMember]
		public long[] Attachments
		{
			get => _attachments;
			set => _attachments = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		[DataMember]
		public long SeqNum { get; set; }

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
			var str = base.ToString();

			if (TransactionId > 0)
				str += $",TrId={TransactionId}";

			str += $",Time={ServerTime:yyyy/MM/dd HH:mm:ss},Sec={SecurityId},Head={Headline}";

			if (Attachments.Length > 0)
				str += $",Attachments={Attachments.Select(id => id.To<string>()).JoinComma()}";

			if (SeqNum != default)
				str += $",SQ={SeqNum}";

			return str;
		}

		/// <inheritdoc />
		public override void CopyTo(NewsMessage destination)
		{
			base.CopyTo(destination);
			
			destination.TransactionId = TransactionId;
			destination.Id = Id;
			destination.BoardCode = BoardCode;
			destination.SecurityId = SecurityId;
			destination.Source = Source;
			destination.Headline = Headline;
			destination.Story = Story;
			destination.ServerTime = ServerTime;
			destination.Url = Url;
			destination.Priority = Priority;
			destination.Language = Language;
			destination.ExpiryDate = ExpiryDate;
			destination.Attachments = Attachments.ToArray();
			destination.SeqNum = SeqNum;
		}
	}
}