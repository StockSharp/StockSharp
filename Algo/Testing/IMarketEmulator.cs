namespace StockSharp.Algo.Testing
{
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий эмулятор торгов.
	/// </summary>
	public interface IMarketEmulator : IMessageChannel, ILogSource
	{
		/// <summary>
		/// Настройки эмулятора.
		/// </summary>
		MarketEmulatorSettings Settings { get; }
	}
}