#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: EntityRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The storage of trade objects.
	/// </summary>
	public class EntityRegistry : IEntityRegistry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EntityRegistry"/>.
		/// </summary>
		public EntityRegistry()
			: this(new InMemoryStorage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EntityRegistry"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public EntityRegistry(IStorage storage)
		{
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));

			ConfigManager.TryRegisterService(storage);

			Exchanges = new ExchangeList(storage) { BulkLoad = true };
			ExchangeBoards = new ExchangeBoardList(storage) { BulkLoad = true };
			Securities = new SecurityList(this);
			//Trades = new TradeList(storage);
			//MyTrades = new MyTradeList(storage);
			//Orders = new OrderList(storage);
			//OrderFails = new OrderFailList(storage);
			Portfolios = new PortfolioList(storage);
			Positions = new PositionList(storage);
			//News = new NewsList(storage);

			PositionStorage = new PositionStorage(this);
		}

		/// <inheritdoc />
		public IStorage Storage { get; }

		/// <inheritdoc />
		public IStorageEntityList<Exchange> Exchanges { get; }

		/// <inheritdoc />
		public IStorageEntityList<ExchangeBoard> ExchangeBoards { get; }

		/// <inheritdoc />
		public IStorageSecurityList Securities { get; }

		/// <inheritdoc />
		public IStorageEntityList<Portfolio> Portfolios { get; }

		/// <inheritdoc />
		public IStoragePositionList Positions { get; }

		/// <inheritdoc />
		public IPositionStorage PositionStorage { get; }

		///// <summary>
		///// The list of own trades.
		///// </summary>
		//public virtual IStorageEntityList<MyTrade> MyTrades { get; }

		///// <summary>
		///// The list of tick trades.
		///// </summary>
		//public virtual IStorageEntityList<Trade> Trades { get; }

		///// <summary>
		///// The list of orders.
		///// </summary>
		//public virtual IStorageEntityList<Order> Orders { get; }

		///// <summary>
		///// The list of orders registration and cancelling errors.
		///// </summary>
		//public virtual IStorageEntityList<OrderFail> OrderFails { get; }

		///// <summary>
		///// The list of news.
		///// </summary>
		//public virtual IStorageEntityList<News> News { get; }

		IDictionary<object, Exception> IEntityRegistry.Init()
		{
			return new Dictionary<object, Exception>();
		}

		DelayAction IEntityRegistry.DelayAction
		{
			get => DelayAction;
			set => DelayAction = (StorageDelayAction)value;
		}

		private StorageDelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public StorageDelayAction DelayAction
		{
			get => _delayAction;
			set
			{
				_delayAction = value;

				((ExchangeList)Exchanges).DelayAction = _delayAction;
				((ExchangeBoardList)ExchangeBoards).DelayAction = _delayAction;
				((SecurityList)Securities).DelayAction = _delayAction;
				//Trades.DelayAction = _delayAction;
				//MyTrades.DelayAction = _delayAction;
				//Orders.DelayAction = _delayAction;
				//OrderFails.DelayAction = _delayAction;
				((PortfolioList)Portfolios).DelayAction = _delayAction;
				((PositionList)Positions).DelayAction = _delayAction;
				//News.DelayAction = _delayAction;
			}
		}
	}
}