namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="DataType"/> property.
/// </summary>
public interface IDataTypeMessage
{
	/// <summary>
	/// Data type info.
	/// </summary>
	DataType DataType { get; }
}
