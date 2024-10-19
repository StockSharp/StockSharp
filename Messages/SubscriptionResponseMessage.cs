namespace StockSharp.Messages;

/// <summary>
/// Subscription response message.
/// </summary>
public class SubscriptionResponseMessage : Message, IOriginalTransactionIdMessage, IErrorMessage
{
	/// <summary>
	/// Not supported error.
	/// </summary>
	public static readonly NotSupportedException NotSupported = new();
	
	/// <inheritdoc />
	[DataMember]
	[XmlIgnore]
	public Exception Error { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <summary>
	/// Initialize <see cref="SubscriptionResponseMessage"/>.
	/// </summary>
	public SubscriptionResponseMessage()
		: base(MessageTypes.SubscriptionResponse)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionResponseMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SubscriptionResponseMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected void CopyTo(SubscriptionResponseMessage destination)
	{
		base.CopyTo(destination);

		destination.OriginalTransactionId = OriginalTransactionId;
		destination.Error = Error;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",OrigTrId={OriginalTransactionId}";

		if (Error != default)
			str += $",Error={Error.Message}";

		return str;
	}
}