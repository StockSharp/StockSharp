namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="IsSystem"/> property.
/// </summary>
public interface ISystemMessage
{
	/// <summary>
	/// Is a system order.
	/// </summary>
	bool? IsSystem { get; }
}
