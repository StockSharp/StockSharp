namespace StockSharp.Messages;

/// <summary>
/// Security mapping result message.
/// </summary>
[Serializable]
[DataContract]
public class SecurityMappingMessage : Message, ISubscriptionMessage
{
	/// <summary>
	/// Initialize <see cref="SecurityMappingMessage"/>.
	/// </summary>
	public SecurityMappingMessage()
		: base(MessageTypes.SecurityMapping)
	{
	}

	/// <summary>
	/// Remove security mapping.
	/// </summary>
	public bool IsDelete { get; set; }

	/// <summary>
	/// Security identifier mapping.
	/// </summary>
	[DataMember]
	public SecurityIdMapping Mapping { get; set; }

	/// <summary>
	/// Storage name.
	/// </summary>
	[DataMember]
	public string StorageName { get; set; }

	FillGapsDays? ISubscriptionMessage.FillGaps { get; set; }

	bool ISubscriptionMessage.FilterEnabled => false;
	bool ISubscriptionMessage.SpecificItemRequest => false;

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

	long? ISubscriptionMessage.Skip
	{
		get => null;
		set { }
	}

	long? ISubscriptionMessage.Count
	{
		get => null;
		set { }
	}

	bool ISubscriptionMessage.IsSubscribe
	{
		get => true;
		set { }
	}

	/// <inheritdoc />
	public DataType DataType { get; } = DataType.SecurityMapping;

	/// <inheritdoc />
	[DataMember]
	public long TransactionId { get; set; }
	
	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="SecurityMappingMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SecurityMappingMessage
		{
			Mapping = Mapping.Clone(),
			StorageName = StorageName,
			IsDelete = IsDelete,
			TransactionId = TransactionId,
			OriginalTransactionId = OriginalTransactionId,
		};

		CopyTo(clone);

		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $"Storage={StorageName},Mapping={Mapping},Del={IsDelete}";

		if (TransactionId != default)
			str += $",TrId={TransactionId}";

		if (OriginalTransactionId != default)
			str += $",OrigTrId={OriginalTransactionId}";

		return str;
	}
}