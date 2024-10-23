namespace StockSharp.Messages;

/// <summary>
/// Remote file command.
/// </summary>
public class RemoteFileCommandMessage : CommandMessage, ISecurityIdMessage, IFileMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteFileCommandMessage"/>.
	/// </summary>
	public RemoteFileCommandMessage()
		: base(MessageTypes.RemoteFileCommand)
	{
		Scope = CommandScopes.File;
	}

	/// <inheritdoc />
	[DataMember]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Market data type.
	/// </summary>
	[DataMember]
	public DataType FileDataType { get; set; }

	/// <inheritdoc />
	[DataMember]
	public override DateTimeOffset? From { get; set; }

	/// <inheritdoc />
	[DataMember]
	public override DateTimeOffset? To { get; set; }

	/// <summary>
	/// Storage format.
	/// </summary>
	[DataMember]
	public int Format { get; set; }

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

	/// <summary>
	/// Create a copy of <see cref="RemoteFileCommandMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new RemoteFileCommandMessage
		{
			SecurityId = SecurityId,
			FileDataType = FileDataType?.TypedClone(),
			Format = Format,
			Body = Body,
		};

		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",SecId={SecurityId},DT={FileDataType},Fmt={Format},BodyLen={Body.Length}";
	}
}