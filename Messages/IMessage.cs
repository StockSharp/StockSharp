namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="Type"/> method.
/// </summary>
public interface IMessage : ILocalTimeMessage, ICloneable
{
	/// <summary>
	/// Message type.
	/// </summary>
	MessageTypes Type { get; }

	/// <summary>
	/// Source adapter. Can be <see langword="null" />.
	/// </summary>
	IMessageAdapter Adapter { get; set; }

	/// <summary>
	/// Back mode.
	/// </summary>
	MessageBackModes BackMode { get; set; }
}