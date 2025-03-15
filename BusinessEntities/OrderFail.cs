namespace StockSharp.BusinessEntities;

/// <summary>
/// Description of the error that occurred during the registration or cancellation of the order.
/// </summary>
[Serializable]
[DataContract]
public class OrderFail : IErrorMessage, ILocalTimeMessage, IServerTimeMessage, ISeqNumMessage, ITransactionIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OrderFail"/>.
	/// </summary>
	public OrderFail()
	{
	}

	/// <summary>
	/// The order which was not registered or was canceled due to an error.
	/// </summary>
	[DataMember]
	public Order Order { get; set; }

	/// <summary>
	/// System information about error containing the reason for the refusal or cancel of registration.
	/// </summary>
	[DataMember]
	public Exception Error { get; set; }

	/// <summary>
	/// Server time.
	/// </summary>
	[DataMember]
	public DateTimeOffset ServerTime { get; set; }

	/// <summary>
	/// Local time, when the error has been received.
	/// </summary>
	public DateTimeOffset LocalTime { get; set; }

	/// <summary>
	/// Sequence number.
	/// </summary>
	/// <remarks>Zero means no information.</remarks>
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc/>
	public long TransactionId { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Error?.Message}/{Order}";
	}
}