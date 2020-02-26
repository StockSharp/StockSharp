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

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The news storage.</returns>
		IEntityMarketDataStorage<News, NewsMessage> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of tick trades.</returns>
		IEntityMarketDataStorage<Trade, ExecutionMessage> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The order books storage.</returns>
		IEntityMarketDataStorage<MarketDepth, QuoteChangeMessage> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of orders log.</returns>
		IEntityMarketDataStorage<OrderLogItem, ExecutionMessage> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// To get the candles storage for the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">Security.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

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
	}
}