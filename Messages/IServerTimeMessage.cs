namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="ServerTime"/> property.
/// </summary>
public interface IServerTimeMessage
{
	/// <summary>
	/// Server time.
	/// </summary>
	DateTime ServerTime { get; set; }
}