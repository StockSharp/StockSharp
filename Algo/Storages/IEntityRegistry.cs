#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IEntityRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface describing the trade objects storage.
	/// </summary>
	public interface IEntityRegistry
	{
		/// <summary>
		/// The special interface for direct access to the storage.
		/// </summary>
		IStorage Storage { get; }

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

		///// <summary>
		///// The list of own trades.
		///// </summary>
		//IStorageEntityList<MyTrade> MyTrades { get; }

		///// <summary>
		///// The list of tick trades.
		///// </summary>
		//IStorageEntityList<Trade> Trades { get; }

		///// <summary>
		///// The list of orders.
		///// </summary>
		//IStorageEntityList<Order> Orders { get; }

		///// <summary>
		///// The list of orders registration and cancelling errors.
		///// </summary>
		//IStorageEntityList<OrderFail> OrderFails { get; }

		///// <summary>
		///// The list of news.
		///// </summary>
		//IStorageEntityList<News> News { get; }

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
		IDictionary<object, Exception> Init();
	}
}