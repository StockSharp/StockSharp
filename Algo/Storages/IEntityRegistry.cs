namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface describing the trade objects storage.
/// </summary>
public interface IEntityRegistry
{
	/// <summary>
	/// </summary>
	[Obsolete("This property exists only for backward compatibility.")]
	object Storage { get; }

	/// <summary>
	/// The time delayed action.
	/// </summary>
	DelayAction DelayAction { get; set; }

	/// <summary>
	/// List of exchanges.
	/// </summary>
	IStorageEntityList<Exchange> Exchanges { get; }

	/// <summary>
	/// The list of stock boards.
	/// </summary>
	IStorageEntityList<ExchangeBoard> ExchangeBoards { get; }

	/// <summary>
	/// The list of instruments.
	/// </summary>
	IStorageSecurityList Securities { get; }

	/// <summary>
	/// Position storage.
	/// </summary>
	IPositionStorage PositionStorage { get; }

	/// <summary>
	/// The list of portfolios.
	/// </summary>
	IStorageEntityList<Portfolio> Portfolios { get; }

	/// <summary>
	/// The list of positions.
	/// </summary>
	IStoragePositionList Positions { get; }

	/// <summary>
	/// The list of subscriptions.
	/// </summary>
	IStorageEntityList<MarketDataMessage> Subscriptions { get; }

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
	IDictionary<object, Exception> Init();
}