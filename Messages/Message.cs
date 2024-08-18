namespace StockSharp.Messages;

/// <summary>
/// <see cref="Message"/> offline modes.
/// </summary>
public enum MessageOfflineModes
{
	/// <summary>
	/// None.
	/// </summary>
	None,

	/// <summary>
	/// Ignore offline mode and continue processing.
	/// </summary>
	Ignore,

	/// <summary>
	/// Cancel message processing and create reply.
	/// </summary>
	Cancel,
}

/// <summary>
/// Message loopback modes.
/// </summary>
public enum MessageBackModes
{
	/// <summary>
	/// None.
	/// </summary>
	None,

	/// <summary>
	/// Direct.
	/// </summary>
	Direct,

	/// <summary>
	/// Via whole adapters chain.
	/// </summary>
	Chain,
}

/// <summary>
/// A message containing market data or command.
/// </summary>
[DataContract]
[Serializable]
public abstract class Message : Cloneable<Message>, IMessage
{
	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LocalTimeKey,
		Description = LocalizedStrings.LocalTimeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[DataMember]
	public DateTimeOffset LocalTime { get; set; }

	[field: NonSerialized]
	private readonly MessageTypes _type;

	/// <inheritdoc />
	public MessageTypes Type => _type;

	/// <summary>
	/// Is loopback message.
	/// </summary>
	[XmlIgnore]
	[Obsolete("Use BackMode property.")]
	public bool IsBack
	{
		get => this.IsBack();
		set => BackMode = value ? MessageBackModes.Direct : MessageBackModes.None;
	}

	/// <inheritdoc />
	[XmlIgnore]
	public MessageBackModes BackMode { get; set; }

	/// <summary>
	/// Offline mode handling message.
	/// </summary>
	[XmlIgnore]
	public MessageOfflineModes OfflineMode { get; set; }

	/// <inheritdoc />
	[XmlIgnore]
	public IMessageAdapter Adapter { get; set; }

	/// <summary>
	/// <see cref="IMessageChannel.SendInMessage"/>
	/// </summary>
	[XmlIgnore]
	public bool Forced { get; set; }

	/// <summary>
	/// Initialize <see cref="Message"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected Message(MessageTypes type)
	{
		_type = type;
#if MSG_TRACE
		StackTrace = Environment.StackTrace;
#endif
	}

#if MSG_TRACE
	internal string StackTrace;
#endif

	/// <inheritdoc />
	public override string ToString()
	{
		var str = Type + $",T(L)={LocalTime:yyyy/MM/dd HH:mm:ss.fff}";

		if (BackMode != default)
			str += $",Back={BackMode}";

		if (OfflineMode != default)
			str += $",Offline={OfflineMode}";

		return str;
	}

	/// <summary>
	/// Create a copy of <see cref="Message"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public abstract override Message Clone();

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected void CopyTo(Message destination)
	{
		if (destination == null)
			throw new ArgumentNullException(nameof(destination));

		destination.LocalTime = LocalTime;
		destination.Forced = Forced;
#if MSG_TRACE
		destination.StackTrace = StackTrace;
#endif
	}
}