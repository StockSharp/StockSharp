namespace StockSharp.Messages;

/// <summary>
/// Message indicating that the connection was lost and reconnection is in progress.
/// </summary>
/// <remarks>
/// Uses when the underlying <see cref="IMessageAdapter"/> controls the connection itself.
/// </remarks>
[DataContract]
[Serializable]
public class ConnectionLostMessage : Message, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectionLostMessage"/>.
	/// </summary>
	public ConnectionLostMessage()
		: base(MessageTypes.ConnectionLost)
	{
	}

    /// <summary>
    /// Create a copy of <see cref="ConnectionLostMessage"/>.
    /// </summary>
    /// <returns>Copy.</returns>
    public override Message Clone()
	{
		var clone = new ConnectionLostMessage();
		CopyTo(clone);
		return clone;
	}

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => LocalTime;
		set => LocalTime = value;
	}
}