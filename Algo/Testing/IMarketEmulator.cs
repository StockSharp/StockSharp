namespace StockSharp.Algo.Testing;

/// <summary>
/// The interface, describing paper trading.
/// </summary>
public interface IMarketEmulator : IMessageAdapter, ILogSource
{
	/// <summary>
	/// Emulator settings.
	/// </summary>
	MarketEmulatorSettings Settings { get; }

	/// <summary>
	/// The number of processed messages.
	/// </summary>
	long ProcessedMessageCount { get; }

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	ISecurityProvider SecurityProvider { get; }

	/// <summary>
	/// The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.
	/// </summary>
	IPortfolioProvider PortfolioProvider { get; }

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	IExchangeInfoProvider ExchangeInfoProvider { get; }
}