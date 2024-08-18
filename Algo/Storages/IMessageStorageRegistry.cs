namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface describing the storage of market data.
/// </summary>
public interface IMessageStorageRegistry
{
	/// <summary>
	/// To get news storage.
	/// </summary>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The news storage.</returns>
	IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get board state storage.
	/// </summary>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The news storage.</returns>
	IMarketDataStorage<BoardStateMessage> GetBoardStateMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the storage of tick trades for the specified instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The storage of tick trades.</returns>
	IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the storage of order books for the specified instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <param name="passThroughOrderBookIncrement">Pass through incremental <see cref="QuoteChangeMessage"/>.</param>
	/// <returns>The order books storage.</returns>
	IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary, bool passThroughOrderBookIncrement = false);

	/// <summary>
	/// To get the storage of orders log for the specified instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The storage of orders log.</returns>
	IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the storage of level1 data.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The storage of level1 data.</returns>
	IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the storage of position changes data.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The storage of position changes data.</returns>
	IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the candles storage for the specified instrument.
	/// </summary>
	/// <param name="candleMessageType">The type of candle message.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The candles storage.</returns>
	IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleMessageType, SecurityId securityId, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the <see cref="ExecutionMessage"/> storage for the specified instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="type">Data type, information about which is contained in the <see cref="ExecutionMessage"/>.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The <see cref="ExecutionMessage"/> storage.</returns>
	IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(SecurityId securityId, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the transactions storage for the specified instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The transactions storage.</returns>
	IMarketDataStorage<ExecutionMessage> GetTransactionStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To get the market-data storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Market data type.</param>
	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, candle arg.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>Market-data storage.</returns>
	IMarketDataStorage GetStorage(SecurityId securityId, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

	/// <summary>
	/// To register tick trades storage.
	/// </summary>
	/// <param name="storage">The storage of tick trades.</param>
	void RegisterTradeStorage(IMarketDataStorage<ExecutionMessage> storage);

	/// <summary>
	/// To register the order books storage.
	/// </summary>
	/// <param name="storage">The order books storage.</param>
	void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage);

	/// <summary>
	/// To register the order log storage.
	/// </summary>
	/// <param name="storage">The storage of orders log.</param>
	void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage);

	/// <summary>
	/// To register the storage of level1 data.
	/// </summary>
	/// <param name="storage">The storage of level1 data.</param>
	void RegisterLevel1Storage(IMarketDataStorage<Level1ChangeMessage> storage);

	/// <summary>
	/// To register the storage of position changes data.
	/// </summary>
	/// <param name="storage">The storage of position changes data.</param>
	void RegisterPositionStorage(IMarketDataStorage<PositionChangeMessage> storage);

	/// <summary>
	/// To register the candles storage.
	/// </summary>
	/// <param name="storage">The candles storage.</param>
	void RegisterCandleStorage(IMarketDataStorage<CandleMessage> storage);
}