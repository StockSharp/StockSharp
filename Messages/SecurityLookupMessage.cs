namespace StockSharp.Messages;

/// <summary>
/// Message security lookup for specified criteria.
/// </summary>
[DataContract]
[Serializable]
public class SecurityLookupMessage : SecurityMessage, ISubscriptionMessage, ISecurityTypesMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TransactionKey,
		Description = LocalizedStrings.TransactionIdKey,
		GroupName = LocalizedStrings.OptionsKey)]
	public long TransactionId { get; set; }

	/// <summary>
	/// Securities types.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.SecurityTypeDescKey,
		GroupName = LocalizedStrings.OptionsKey)]
	public SecurityTypes[] SecurityTypes { get; set; }

	/// <summary>
	/// Request only <see cref="SecurityMessage.SecurityId"/>.
	/// </summary>
	[DataMember]
	public bool OnlySecurityId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Skip { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Count { get; set; }

	/// <inheritdoc />
	[DataMember]
	public FillGapsDays? FillGaps { get; set; }

	private SecurityId[] _securityIds = [];

	/// <summary>
	/// Security identifiers.
	/// </summary>
	[DataMember]
	public SecurityId[] SecurityIds
	{
		get => _securityIds;
		set => _securityIds = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Include expired securities.
	/// </summary>
	[DataMember]
	public bool IncludeExpired { get; set; }

	/// <summary>
	/// Disable translates the result of the request into the archive <see cref="SubscriptionFinishedMessage.Body"/>.
	/// </summary>
	[DataMember]
	public bool DisableArchive { get; set; }

	bool ISubscriptionMessage.SpecificItemRequest => false;

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityLookupMessage"/>.
	/// </summary>
	public SecurityLookupMessage()
		: base(MessageTypes.SecurityLookup)
	{
	}

	DataType ISubscriptionMessage.DataType => DataType.Securities;

	bool ISubscriptionMessage.FilterEnabled
		=>
		SecurityType != null || SecurityId != default ||
		!Name.IsEmpty() || !ShortName.IsEmpty() ||
		SecurityTypes?.Length > 0 || OptionType != null ||
		Strike != null || VolumeStep != null || PriceStep != null ||
		!CfiCode.IsEmpty() || MinVolume != null || MaxVolume != null ||
		Multiplier != null || Decimals != null || ExpiryDate != null ||
		SettlementDate != null || IssueDate != null || IssueSize != null ||
		UnderlyingSecurityId != default || UnderlyingSecurityMinVolume != null ||
		UnderlyingSecurityType != null || !Class.IsEmpty() || Currency != null ||
		!BinaryOptionType.IsEmpty() || Shortable != null || FaceValue != null ||
		SettlementType != null || OptionStyle != null;

	/// <summary>
	/// Create a copy of <see cref="SecurityLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SecurityLookupMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(SecurityLookupMessage destination)
	{
		if (destination == null)
			throw new ArgumentNullException(nameof(destination));

		destination.TransactionId = TransactionId;
		destination.SecurityTypes = SecurityTypes?.ToArray();
		destination.OnlySecurityId = OnlySecurityId;
		destination.Skip = Skip;
		destination.Count = Count;
		destination.SecurityIds = [.. SecurityIds];
		destination.FillGaps = FillGaps;
		destination.IncludeExpired = IncludeExpired;
		destination.DisableArchive = DisableArchive;

		base.CopyTo(destination);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",TransId={TransactionId},SecId={SecurityId},Name={Name},SecType={this.GetSecurityTypes().Select(t => t.To<string>()).JoinPipe()},ExpDate={ExpiryDate}";

		if (Skip != default)
			str += $",Skip={Skip}";

		if (Count != default)
			str += $",Cnt={Count}";

		if (OnlySecurityId)
			str += $",OnlyId={OnlySecurityId}";

		if (SecurityIds.Length > 0)
			str += $",Ids={SecurityIds.Select(id => id.ToString()).JoinComma()}";

		if (FillGaps is not null)
			str += $",gaps={FillGaps}";

		if (IncludeExpired)
			str += $",expired={IncludeExpired}";

		if (DisableArchive)
			str += $",Archive={DisableArchive}";

		return str;
	}

	DateTimeOffset? ISubscriptionMessage.From
	{
		get => null;
		set { }
	}

	DateTimeOffset? ISubscriptionMessage.To
	{
		// prevent for online mode
		get => DateTimeOffset.MaxValue;
		set { }
	}

	bool ISubscriptionMessage.IsSubscribe
	{
		get => true;
		set { }
	}
}