namespace StockSharp.Messages;

/// <summary>
/// Remove file message (upload or download).
/// </summary>
public class RemoteFileMessage : BaseSubscriptionIdMessage<RemoteFileMessage>, ITransactionIdMessage, ISecurityIdMessage, IFileMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteFileMessage"/>.
	/// </summary>
	public RemoteFileMessage()
		: base(MessageTypes.RemoteFile)
	{
	}

	/// <inheritdoc />
	[DataMember]
	public long TransactionId { get; set; }

	private byte[] _body = [];

	/// <summary>
	/// File body.
	/// </summary>
	[DataMember]
	public byte[] Body
	{
		get => _body;
		set => _body = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	[DataMember]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Market data type.
	/// </summary>
	[DataMember]
	public DataType FileDataType { get; set; }

	/// <summary>
	/// Date.
	/// </summary>
	[DataMember]
	public DateTimeOffset Date { get; set; }

	/// <summary>
	/// Storage format.
	/// </summary>
	[DataMember]
	public int Format { get; set; }

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public override void CopyTo(RemoteFileMessage destination)
	{
		base.CopyTo(destination);

		destination.TransactionId = TransactionId;
		destination.Body = Body;
		destination.SecurityId = SecurityId;
		destination.FileDataType = FileDataType?.TypedClone();
		destination.Date = Date;
		destination.Format = Format;
	}

	/// <summary>
	/// Create a copy of <see cref="RemoteFileMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new RemoteFileMessage();
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.RemoteFile;

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",SecId={SecurityId},DT={FileDataType},Date={Date},Fmt={Format},BodyLen={Body.Length}";
	}
}