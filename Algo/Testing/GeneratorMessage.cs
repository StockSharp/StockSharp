namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

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
	}
}