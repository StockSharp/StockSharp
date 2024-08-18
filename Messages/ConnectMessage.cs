namespace StockSharp.Messages;

/// <summary>
/// Connect to a server message (uses as a command in outgoing case, event in incoming case).
/// </summary>
[DataContract]
[Serializable]
public class ConnectMessage : BaseConnectionMessage, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectMessage"/>.
	/// </summary>
	public ConnectMessage()
		: base(MessageTypes.Connect)
	{
	}

	/// <summary>
	/// Client app version.
	/// </summary>
	[DataMember]
	public string ClientVersion { get; set; }

	/// <summary>
	/// Optional server session id.
	/// </summary>
	[DataMember]
	public string SessionId { get; set; }

	/// <summary>
	/// Language.
	/// </summary>
	[DataMember]
	public string Language { get; set; }

	/// <summary>
	/// Create a copy of <see cref="ConnectMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new ConnectMessage
		{
			ClientVersion = ClientVersion,
			SessionId = SessionId,
			Language = Language,
		};

		CopyTo(clone);
		return clone;
	}

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => LocalTime;
		set => LocalTime = value;
	}
}