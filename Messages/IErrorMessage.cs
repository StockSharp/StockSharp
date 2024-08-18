namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="Error"/> property.
/// </summary>
public interface IErrorMessage
{
	/// <summary>
	/// Error info.
	/// </summary>
	Exception Error { get; set; }
}