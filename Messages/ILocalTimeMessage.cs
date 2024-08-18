namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="LocalTime"/> property.
/// </summary>
public interface ILocalTimeMessage
{
	/// <summary>
	/// Local timestamp when a message was received/created.
	/// </summary>
	DateTimeOffset LocalTime { get; }
}