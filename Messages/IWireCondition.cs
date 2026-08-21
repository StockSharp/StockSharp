namespace StockSharp.Messages;

/// <summary>
/// Represents an order condition that can encode itself for a wire format without a dedicated condition record.
/// </summary>
public interface IWireCondition
{
	/// <summary>
	/// Encodes the condition into an opaque payload.
	/// </summary>
	/// <returns>The encoded condition payload.</returns>
	string ToWire();

	/// <summary>
	/// Populates the condition from an opaque payload produced by <see cref="ToWire"/>.
	/// </summary>
	/// <param name="payload">The encoded condition payload.</param>
	void FromWire(string payload);
}
