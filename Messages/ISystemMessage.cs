namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="IsSystem"/> property.
/// </summary>
public interface ISystemMessage
{
	/// <summary>
	/// Is a system.
	/// </summary>
	bool? IsSystem { get; }

	/// <summary>
	/// System status.
	/// </summary>
	long? Status { get; }
}
