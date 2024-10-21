namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RegularKey)]
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
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NewsKey)]
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
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdKey,
		Description = LocalizedStrings.NewsIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Id { get; set; }

	/// <summary>
	/// Electronic board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BoardCode { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.NewsSecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId? SecurityId { get; set; }

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
		Description = LocalizedStrings.HeaderKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Headline { get; set; }

	/// <summary>
	/// News text.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TextKey,
		Description = LocalizedStrings.NewsTextKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Story { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeKey,
		Description = LocalizedStrings.NewsTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset ServerTime { get; set; }

	/// <summary>
	/// News link in the internet.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LinkKey,
		Description = LocalizedStrings.NewsLinkKey,
		GroupName = LocalizedStrings.GeneralKey)]
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
	//[Ecng.Serialization.Nullable]
	public NewsPriorities? Priority { get; set; }

	/// <summary>
	/// Product id.
	/// </summary>
	[DataMember]
	[Browsable(false)]
	public long ProductId { get; set; }

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

	private long[] _attachments = [];

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

		if (ProductId != default)
			str += $",product={ProductId}";

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
		destination.Attachments = [.. Attachments];
		destination.SeqNum = SeqNum;
		destination.ProductId = ProductId;
	}
}