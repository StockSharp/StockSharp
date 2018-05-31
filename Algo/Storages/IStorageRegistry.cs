#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IStorageRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the storage of market data.
	/// </summary>
	public interface IStorageRegistry
	{
		/// <summary>
		/// The storage used by default.
		/// </summary>
		IMarketDataDrive DefaultDrive { get; set; }

		/// <summary>
		/// Exchanges and trading boards provider.
		/// </summary>
		IExchangeInfoProvider ExchangeInfoProvider { get; }

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The news storage.</returns>
		IMarketDataStorage<News> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The news storage.</returns>
		IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of tick trades.</returns>
		IMarketDataStorage<Trade> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The order books storage.</returns>
		IMarketDataStorage<MarketDepth> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of orders log.</returns>
		IMarketDataStorage<OrderLogItem> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the candles storage for the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">Security.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		IMarketDataStorage<Candle> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of tick trades.</returns>
		IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The order books storage.</returns>
		IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of orders log.</returns>
		IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of level1 data.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of level1 data.</returns>
		IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of position changes data.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of position changes data.</returns>
		IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the candles storage for the specified instrument.
		/// </summary>
		/// <param name="candleMessageType">The type of candle message.</param>
		/// <param name="security">Security.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleMessageType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the <see cref="ExecutionMessage"/> storage for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="type">Data type, information about which is contained in the <see cref="ExecutionMessage"/>.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The <see cref="ExecutionMessage"/> storage.</returns>
		IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(Security security, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the transactions storage for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The transactions storage.</returns>
		IMarketDataStorage<ExecutionMessage> GetTransactionStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>Market-data storage.</returns>
		IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the instruments storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The instruments storage.</returns>
		ISecurityStorage GetSecurityStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To register tick trades storage.
		/// </summary>
		/// <param name="storage">The storage of tick trades.</param>
		void RegisterTradeStorage(IMarketDataStorage<Trade> storage);

		/// <summary>
		/// To register the order books storage.
		/// </summary>
		/// <param name="storage">The order books storage.</param>
		void RegisterMarketDepthStorage(IMarketDataStorage<MarketDepth> storage);

		/// <summary>
		/// To register the order log storage.
		/// </summary>
		/// <param name="storage">The storage of orders log.</param>
		void RegisterOrderLogStorage(IMarketDataStorage<OrderLogItem> storage);

		/// <summary>
		/// To register the candles storage.
		/// </summary>
		/// <param name="storage">The candles storage.</param>
		void RegisterCandleStorage(IMarketDataStorage<Candle> storage);

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
}