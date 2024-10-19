namespace StockSharp.Messages;

/// <summary>
/// The message contains information about the current time.
/// </summary>
[DataContract]
[Serializable]
public class TimeMessage : Message, ITransactionIdMessage, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TimeMessage"/>.
	/// </summary>
	public TimeMessage()
		: base(MessageTypes.Time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected TimeMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <inheritdoc />
	[DataMember]
	public long TransactionId { get; set; }

	/// <summary>
	/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
	/// </summary>
	[DataMember]
	public string OriginalTransactionId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",ID={TransactionId},Response={OriginalTransactionId}";
	}

	/// <summary>
	/// Create a copy of <see cref="TimeMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new TimeMessage
		{
			TransactionId = TransactionId,
			OriginalTransactionId = OriginalTransactionId,
			LocalTime = LocalTime,
			ServerTime = ServerTime,
		};
	}
}