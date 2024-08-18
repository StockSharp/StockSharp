namespace StockSharp.Messages;

/// <summary>
/// The message, informing about the emulator state change.
/// </summary>
public class EmulationStateMessage : Message, IErrorMessage
{
	/// <summary>
	/// Date in history for starting the paper trading.
	/// </summary>
	public DateTimeOffset StartDate { get; set; }

	/// <summary>
	/// Date in history to stop the paper trading (date is included).
	/// </summary>
	public DateTimeOffset StopDate { get; set; }

	/// <summary>
	/// State.
	/// </summary>
	public ChannelStates State { get; set; }

	/// <inheritdoc />
	public Exception Error { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EmulationStateMessage"/>.
	/// </summary>
	public EmulationStateMessage()
		: base(MessageTypes.EmulationState)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="EmulationStateMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new EmulationStateMessage
		{
			State = State,
			StartDate = StartDate,
			StopDate = StopDate,
			Error = Error,
		};
	}
}