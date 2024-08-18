namespace StockSharp.Algo.Testing;

/// <summary>
/// The message about creation or deletion of the market data generator.
/// </summary>
public class GeneratorMessage : MarketDataMessage
{
	/// <summary>
	/// The market data generator.
	/// </summary>
	public MarketDataGenerator Generator { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratorMessage"/>.
	/// </summary>
	public GeneratorMessage()
		: base(ExtendedMessageTypes.Generator)
	{
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(GeneratorMessage destination)
	{
		base.CopyTo(destination);

		destination.Generator = Generator?.Clone();
	}

	/// <summary>
	/// Create a copy of <see cref="GeneratorMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new GeneratorMessage();
		CopyTo(clone);
		return clone;
	}
}