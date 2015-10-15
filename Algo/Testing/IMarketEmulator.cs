namespace StockSharp.Algo.Testing
{
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing paper trading.
	/// </summary>
	public interface IMarketEmulator : IMessageChannel, ILogSource
	{
		/// <summary>
		/// Emulator settings.
		/// </summary>
		MarketEmulatorSettings Settings { get; }
	}
}