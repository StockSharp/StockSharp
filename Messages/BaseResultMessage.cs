namespace StockSharp.Messages;

/// <summary>
/// Base result message.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseResultMessage{TMessage}"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class BaseResultMessage<TMessage>(MessageTypes type) : Message(type), IOriginalTransactionIdMessage
	where TMessage : BaseResultMessage<TMessage>, new()
{
	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="BaseResultMessage{TMessage}"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new TMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected virtual void CopyTo(TMessage destination)
	{
		base.CopyTo(destination);

		destination.OriginalTransactionId = OriginalTransactionId;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $",Orig={OriginalTransactionId}";
}