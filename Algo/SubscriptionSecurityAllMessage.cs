namespace StockSharp.Algo;

/// <summary>
/// Subscription ALL parent-child mapping message.
/// </summary>
[Serializable]
[DataContract]
public class SubscriptionSecurityAllMessage : MarketDataMessage
{
	/// <summary>
	/// Initialize <see cref="SubscriptionSecurityAllMessage"/>.
	/// </summary>
	public SubscriptionSecurityAllMessage()
		: base(ExtendedMessageTypes.SubscriptionSecurityAll)
	{
	}

	/// <summary>
	/// Parent <see cref="MarketDataMessage.TransactionId"/>.
	/// </summary>
	[DataMember]
	public long ParentTransactionId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="SubscriptionSecurityAllMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone =  new SubscriptionSecurityAllMessage
		{
			ParentTransactionId = ParentTransactionId,
		};

		CopyTo(clone);

		return clone;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $",Parent={ParentTransactionId}";
}