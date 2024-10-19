namespace StockSharp.Messages;

/// <summary>
/// Subscriptions list request message.
/// </summary>
[Serializable]
[DataContract]
public class SubscriptionListRequestMessage : BaseRequestMessage
{
	/// <summary>
	/// Initialize <see cref="SubscriptionListRequestMessage"/>.
	/// </summary>
	public SubscriptionListRequestMessage()
		: base(MessageTypes.SubscriptionListRequest)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.Create<MarketDataMessage>();

	/// <summary>
	/// Create a copy of <see cref="SubscriptionListRequestMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SubscriptionListRequestMessage();
		CopyTo(clone);
		return clone;
	}
}