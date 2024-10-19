namespace StockSharp.Messages;

/// <summary>
/// Error message.
/// </summary>
[DataContract]
[Serializable]
public class ErrorMessage : Message, IErrorMessage, IOriginalTransactionIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorMessage"/>.
	/// </summary>
	public ErrorMessage()
		: base(MessageTypes.Error)
	{
	}

	/// <inheritdoc />
	[DataMember]
	[XmlIgnore]
	public Exception Error { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",Error={Error?.Message},OrigTrId={OriginalTransactionId}";
	}

	/// <summary>
	/// Create a copy of <see cref="ErrorMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new ErrorMessage
		{
			Error = Error,
			OriginalTransactionId = OriginalTransactionId,
		};

		CopyTo(clone);

		return clone;
	}
}