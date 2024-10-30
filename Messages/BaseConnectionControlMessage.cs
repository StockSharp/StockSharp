namespace StockSharp.Messages;

/// <summary>
/// Base class for messages controlling the connection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseConnectionControlMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
public abstract class BaseConnectionControlMessage(MessageTypes type)
	: Message(type), IServerTimeMessage
{
	/// <summary>
	/// Determines a state should be reset.
	/// </summary>
	public bool IsResetState { get; set; }

	/// <inheritdoc />
	public override string ToString()
		=> $"{base.ToString()},R={IsResetState}";

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => LocalTime;
		set => LocalTime = value;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <returns>The object, to which copied information.</returns>
	protected BaseConnectionControlMessage CopyTo(BaseConnectionControlMessage destination)
	{
		base.CopyTo(destination);

		destination.IsResetState = IsResetState;

		return destination;
	}
}
