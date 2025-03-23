namespace StockSharp.Messages;

/// <summary>
/// Market data request finished message.
/// </summary>
[DataContract]
[Serializable]
public class SubscriptionFinishedMessage : BaseResultMessage<SubscriptionFinishedMessage>, IFileMessage
{
	/// <summary>
	/// Initialize <see cref="SubscriptionFinishedMessage"/>.
	/// </summary>
	public SubscriptionFinishedMessage()
		: base(MessageTypes.SubscriptionFinished)
	{
	}

	/// <summary>
	/// Recommended value for next <see cref="ISubscriptionMessage.From"/> (in case of partial requests).
	/// </summary>
	[DataMember]
	public DateTimeOffset? NextFrom { get; set; }

	private byte[] _body = [];

	/// <summary>
	/// Subscription data was sent as archive.
	/// </summary>
	[DataMember]
	public byte[] Body
	{
		get => _body;
		set => _body = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	protected override void CopyTo(SubscriptionFinishedMessage destination)
	{
		base.CopyTo(destination);

		destination.NextFrom = NextFrom;
		destination.Body = Body;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString();

		if (NextFrom != null)
			str += $",Next={NextFrom}";

		if (Body.Length > 0)
			str += $",BodyLen={Body.Length}";

		return str;
	}
}