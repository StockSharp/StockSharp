namespace StockSharp.Messages;

/// <summary>
/// Subscription goes online message.
/// </summary>
[DataContract]
[Serializable]
public class SubscriptionOnlineMessage : Message, IOriginalTransactionIdMessage
{
	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <summary>
	/// Initialize <see cref="SubscriptionOnlineMessage"/>.
	/// </summary>
	public SubscriptionOnlineMessage()
		: base(MessageTypes.SubscriptionOnline)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionOnlineMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SubscriptionOnlineMessage
		{
			OriginalTransactionId = OriginalTransactionId,
		};

		CopyTo(clone);

		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
		=> base.ToString() + $",OrigTransId={OriginalTransactionId}";
}