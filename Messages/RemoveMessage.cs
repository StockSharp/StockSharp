namespace StockSharp.Messages;

/// <summary>
/// Removing object types.
/// </summary>
[DataContract]
[Serializable]
public enum RemoveTypes
{
	/// <summary>
	/// Security.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityKey)]
	Security,

	/// <summary>
	/// Portfolio.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioKey)]
	Portfolio,

	/// <summary>
	/// Exchange.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExchangeKey)]
	Exchange,

	/// <summary>
	/// Board.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardKey)]
	Board,

	/// <summary>
	/// User.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UserKey)]
	User,
}

/// <summary>
/// Remove object request (security, portfolio etc.).
/// </summary>
[DataContract]
[Serializable]
public class RemoveMessage : Message, ITransactionIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RemoveMessage"/>.
	/// </summary>
	public RemoveMessage()
		: base(MessageTypes.Remove)
	{
	}

	/// <summary>
	/// Removing object type.
	/// </summary>
	[DataMember]
	public RemoveTypes RemoveType { get; set; }

	/// <summary>
	/// Removing object id.
	/// </summary>
	[DataMember]
	public string RemoveId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long TransactionId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="RemoveMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new RemoveMessage
		{
			RemoveType = RemoveType,
			RemoveId = RemoveId,
			TransactionId = TransactionId,
		};

		CopyTo(clone);

		return clone;
	}
}