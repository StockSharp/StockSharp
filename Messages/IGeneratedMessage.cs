namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="BuildFrom"/> property.
/// </summary>
public interface IGeneratedMessage
{
	/// <summary>
	/// Determines the message is generated from the specified <see cref="DataType"/>.
	/// </summary>
	DataType BuildFrom { get; set; }
}