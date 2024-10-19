namespace StockSharp.Messages;

/// <summary>
/// Message users lookup for specified criteria.
/// </summary>
[DataContract]
[Serializable]
public class UserLookupMessage : BaseSubscriptionMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UserLookupMessage"/>.
	/// </summary>
	public UserLookupMessage()
		: base(MessageTypes.UserLookup)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.Users;

	/// <summary>
	/// The filter for user search.
	/// </summary>
	[DataMember]
	public string Like { get; set; }

	/// <summary>
	/// Own.
	/// </summary>
	[DataMember]
	public bool Own { get; set; }

	/// <summary>
	/// Identifier.
	/// </summary>
	[DataMember]
	public long? UserId { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",Like={Like},Own={Own},UId={UserId}";
	}

	/// <summary>
	/// Create a copy of <see cref="UserLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return CopyTo(new UserLookupMessage());
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <returns>The object, to which copied information.</returns>
	protected UserLookupMessage CopyTo(UserLookupMessage destination)
	{
		base.CopyTo(destination);

		destination.Like = Like;
		destination.Own = Own;
		destination.UserId = UserId;

		return destination;
	}

	/// <inheritdoc />
	[DataMember]
	public override DateTimeOffset? From => null;

	/// <inheritdoc />
	[DataMember]
	public override DateTimeOffset? To => DateTimeOffset.MaxValue /* prevent for online mode */;

	/// <inheritdoc />
	[DataMember]
	public override bool IsSubscribe => true;

	/// <inheritdoc />
	[DataMember]
	public override long OriginalTransactionId => 0;
}