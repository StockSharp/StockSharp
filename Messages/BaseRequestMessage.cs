namespace StockSharp.Messages;

/// <summary>
/// Base implementation of <see cref="ISubscriptionMessage"/> interface with non-online mode.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseRequestMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class BaseRequestMessage(MessageTypes type) : BaseSubscriptionMessage(type)
{
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