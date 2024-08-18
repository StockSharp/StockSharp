namespace StockSharp.Messages;

/// <summary>
/// Disconnect from a server message (uses as a command in outgoing case, event in incoming case).
/// </summary>
[DataContract]
[Serializable]
public class DisconnectMessage : BaseConnectionMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DisconnectMessage"/>.
	/// </summary>
	public DisconnectMessage()
		: base(MessageTypes.Disconnect)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="DisconnectMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new DisconnectMessage();
		CopyTo(clone);
		return clone;
	}
}