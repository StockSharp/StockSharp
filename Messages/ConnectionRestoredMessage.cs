namespace StockSharp.Messages;

/// <summary>
/// Message indicating that the connection was restored.
/// </summary>
/// <remarks>
/// Uses when the underlying <see cref="IMessageAdapter"/> controls the connection itself.
/// </remarks>
[DataContract]
[Serializable]
public class ConnectionRestoredMessage : Message, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectionRestoredMessage"/>.
	/// </summary>
	public ConnectionRestoredMessage()
		: base(MessageTypes.ConnectionRestored)
	{
	}

	/// <summary>
	/// Determines a state should be reset.
	/// </summary>
	public bool IsResetState { get; set; }

	/// <summary>
	/// Create a copy of <see cref="ConnectionRestoredMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new ConnectionRestoredMessage { IsResetState = IsResetState };
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"{base.ToString()},R={IsResetState}";

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => LocalTime;
		set => LocalTime = value;
	}
}