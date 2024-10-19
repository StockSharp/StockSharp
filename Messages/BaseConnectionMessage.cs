namespace StockSharp.Messages;

/// <summary>
/// Base connect/disconnect message.
/// </summary>
[DataContract]
[Serializable]
public abstract class BaseConnectionMessage : Message, IErrorMessage
{
	/// <summary>
	/// Initialize <see cref="BaseConnectionMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected BaseConnectionMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <inheritdoc />
	[DataMember]
	[XmlIgnore]
	public Exception Error { get; set; }

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected virtual void CopyTo(BaseConnectionMessage destination)
	{
		base.CopyTo(destination);

		destination.Error = Error;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + (Error == null ? null : $",Error={Error.Message}");
	}
}