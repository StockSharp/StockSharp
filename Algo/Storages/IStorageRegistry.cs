namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface describing the storage of market data.
/// </summary>
public interface IStorageRegistry : IMessageStorageRegistry
{
	/// <summary>
	/// The storage used by default.
	/// </summary>
	IMarketDataDrive DefaultDrive { get; set; }

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	IExchangeInfoProvider ExchangeInfoProvider { get; }
}