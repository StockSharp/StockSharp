namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="Id"/> property.
/// </summary>
public interface IComplexIdMessage
{
	/// <summary>
	/// ID.
	/// </summary>
	long? Id { get; }

	/// <summary>
	/// ID (as string, if electronic board does not use numeric ID representation).
	/// </summary>
	string StringId { get; }
}
