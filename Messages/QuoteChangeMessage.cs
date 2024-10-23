namespace StockSharp.Messages;

/// <summary>
/// Order book states.
/// </summary>
[DataContract]
[Serializable]
public enum QuoteChangeStates
{
	/// <summary>
	/// Snapshot started.
	/// </summary>
	[EnumMember]
	SnapshotStarted,

	/// <summary>
	/// Snapshot building.
	/// </summary>
	[EnumMember]
	SnapshotBuilding,

	/// <summary>
	/// Snapshot complete.
	/// </summary>
	[EnumMember]
	SnapshotComplete,

	/// <summary>
	/// Incremental.
	/// </summary>
	[EnumMember]
	Increment,
}

/// <summary>
/// Messages containing quotes.
/// </summary>
[DataContract]
[Serializable]
public sealed class QuoteChangeMessage : BaseSubscriptionIdMessage<QuoteChangeMessage>, IOrderBookMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId SecurityId { get; set; }

	private QuoteChange[] _bids = [];

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BidsKey,
		Description = LocalizedStrings.QuotesBuyKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public QuoteChange[] Bids
	{
		get => _bids;
		set => _bids = value ?? throw new ArgumentNullException(nameof(value));
	}

	private QuoteChange[] _asks = [];

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AsksKey,
		Description = LocalizedStrings.QuotesSellKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public QuoteChange[] Asks
	{
		get => _asks;
		set => _asks = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ServerTimeKey,
		Description = LocalizedStrings.ChangeServerTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <summary>
	/// The quote change contains filtered quotes.
	/// </summary>
	[Browsable(false)]
	public bool IsFiltered { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Ecng.Serialization.Nullable]
	public CurrencyTypes? Currency { get; set; }

	/// <inheritdoc />
	[DataMember]
	public QuoteChangeStates? State { get; set; }

	/// <summary>
	/// Determines a <see cref="QuoteChange.StartPosition"/> initialized.
	/// </summary>
	[DataMember]
	public bool HasPositions { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.MarketDepth;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public QuoteChangeMessage()
		: base(MessageTypes.QuoteChange)
	{
	}

	/// <inheritdoc />
	public override void CopyTo(QuoteChangeMessage destination)
	{
		base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.Bids = [.. Bids];
		destination.Asks = [.. Asks];
		destination.ServerTime = ServerTime;
		destination.Currency = Currency;
		destination.BuildFrom = BuildFrom;
		destination.IsFiltered = IsFiltered;
		destination.State = State;
		destination.HasPositions = HasPositions;
		destination.SeqNum = SeqNum;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Sec={SecurityId},T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff},B={Bids.Length},A={Asks.Length}";

		if (State != default)
			str += $",State={State.Value}";

		if (SeqNum != default)
			str += $",SQ={SeqNum}";

		return str;
	}
}