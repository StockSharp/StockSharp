namespace StockSharp.Messages;

/// <summary>
/// Message portfolio lookup for specified criteria.
/// </summary>
[DataContract]
[Serializable]
public class PortfolioLookupMessage : PortfolioMessage, INullableSecurityIdMessage, IStrategyIdMessage, ISubscriptionMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	public PortfolioLookupMessage()
		: base(MessageTypes.PortfolioLookup)
	{
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TransactionKey,
		Description = LocalizedStrings.TransactionIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public long TransactionId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public bool IsSubscribe { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FromKey,
		Description = LocalizedStrings.StartDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? From { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UntilKey,
		Description = LocalizedStrings.ToDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? To { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Skip { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Count { get; set; }

	/// <inheritdoc />
	[DataMember]
	public FillGapsDays? FillGaps { get; set; }

	/// <inheritdoc />
	[DataMember]
	public string StrategyId { get; set; }

	/// <summary>
	/// Side.
	/// </summary>
	[DataMember]
	public Sides? Side { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.PositionChanges;

	/// <inheritdoc />
	[TypeConverter(typeof(StringToSecurityIdTypeConverter))]
	public SecurityId? SecurityId { get; set; }

	bool ISubscriptionMessage.FilterEnabled
		=>
		!PortfolioName.IsEmpty() || Currency != null ||
		!BoardCode.IsEmpty() || !ClientCode.IsEmpty();

	bool ISubscriptionMessage.SpecificItemRequest => false;

	/// <summary>
	/// Create a copy of <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new PortfolioLookupMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected virtual void CopyTo(PortfolioLookupMessage destination)
	{
		base.CopyTo(destination);

		destination.TransactionId = TransactionId;
		destination.SecurityId = SecurityId;
		destination.StrategyId = StrategyId;
		destination.Side = Side;
		destination.IsSubscribe = IsSubscribe;
		destination.From = From;
		destination.To = To;
		destination.Skip = Skip;
		destination.Count = Count;
		destination.FillGaps = FillGaps;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString();

		if (TransactionId > 0)
			str += $",TransId={TransactionId}";

		if (!IsSubscribe)
			str += $",IsSubscribe={IsSubscribe}";

		if (SecurityId != null)
			str += $",Sec={SecurityId}";

		if (!StrategyId.IsEmpty())
			str += $",Strategy={StrategyId}";

		if (Side != null)
			str += $",Side={Side.Value}";

		if (From != default)
			str += $",From={From}";

		if (To != default)
			str += $",To={To}";

		if (Skip != default)
			str += $",Skip={Skip}";

		if (Count != default)
			str += $",Count={Count}";

		if (FillGaps != default)
			str += $",Gaps={FillGaps}";

		return str;
	}
}