namespace StockSharp.Messages;

/// <summary>
/// Message indicating that the connection was restored.
/// </summary>
/// <remarks>
/// Uses when the underlying <see cref="IMessageAdapter"/> controls the connection itself.
/// </remarks>
[DataContract]
[Serializable]
public class ConnectionRestoredMessage : BaseConnectionControlMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectionRestoredMessage"/>.
	/// </summary>
	public ConnectionRestoredMessage()
		: base(MessageTypes.ConnectionRestored)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="ConnectionRestoredMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
		=> CopyTo(new ConnectionRestoredMessage());
}