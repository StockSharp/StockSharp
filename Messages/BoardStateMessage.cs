namespace StockSharp.Messages;

/// <summary>
/// Session states.
/// </summary>
[DataContract]
[Serializable]
public enum SessionStates
{
	/// <summary>
	/// Session assigned. Cannot register new orders, but can cancel.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AssignedKey)]
	Assigned,

	/// <summary>
	/// Session active. Can register and cancel orders.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,

	/// <summary>
	/// Suspended. Cannot register new orders, but can cancel.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SuspendedKey)]
	Paused,

	/// <summary>
	/// Rejected. Cannot register and cancel orders.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StoppedKey)]
	ForceStopped,

	/// <summary>
	/// Finished. Cannot register and cancel orders.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FinishedKey)]
	Ended,
}

/// <summary>
/// Session change changed message.
/// </summary>
[DataContract]
[Serializable]
public class BoardStateMessage : BaseSubscriptionIdMessage<BoardStateMessage>, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BoardStateMessage"/>.
	/// </summary>
	public BoardStateMessage()
		: base(MessageTypes.BoardState)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.BoardState;

	/// <summary>
	/// Board code.
	/// </summary>
	[DataMember]
	public string BoardCode { get; set; }

	/// <summary>
	/// Session state.
	/// </summary>
	[DataMember]
	public SessionStates State { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	public override void CopyTo(BoardStateMessage destination)
	{
		base.CopyTo(destination);

		destination.BoardCode = BoardCode;
		destination.State = State;
		destination.ServerTime = ServerTime;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",Board={BoardCode},State={State}";
	}
}