namespace StockSharp.Messages;

/// <summary>
/// Base connect/disconnect message.
/// </summary>
/// <remarks>
/// Initialize <see cref="BaseConnectionMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class BaseConnectionMessage(MessageTypes type) : Message(type), IErrorMessage
{
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