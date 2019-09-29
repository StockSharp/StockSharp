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
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The storage of trade objects.
	/// </summary>
	public class EntityRegistry : IEntityRegistry
	{
		/// <summary>
		/// The class for representation in the form of list of instruments, stored in external storage.
		/// </summary>
		private class SecurityList : BaseStorageEntityList<Security>, IStorageSecurityList
		{
			private readonly IEntityRegistry _registry;

			//private readonly DatabaseCommand _readAllByCodeAndType;
			//private readonly DatabaseCommand _readAllByCodeAndTypeAndExpiryDate;
			//private readonly DatabaseCommand _readAllByType;
			//private readonly DatabaseCommand _readAllByBoardAndType;
			//private readonly DatabaseCommand _readAllByTypeAndExpiryDate;
			//private readonly DatabaseCommand _readSecurityIds;

			//private const string _code = nameof(Security.Code);
			//private const string _type = nameof(Security.Type);
			//private const string _expiryDate = nameof(Security.ExpiryDate);
			//private const string _board = nameof(Security.Board);
			//private const string _id = nameof(Security.Id);

			/// <summary>
			/// Initializes a new instance of the <see cref="SecurityList"/>.
			/// </summary>
			/// <param name="registry">The storage of trade objects.</param>
			public SecurityList(IEntityRegistry registry)
				: base(registry.Storage)
			{
				_registry = registry;

				//if (!(Storage is Database database))
				//	return;

				//var readAllByCodeAndType = database.CommandType == CommandType.StoredProcedure
				//	? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndType")
				//	: Query
				//	  .Select(Schema)
				//	  .From(Schema)
				//	  .Where()
				//	  .Like(Schema.Fields[_code])
				//	  .And()
				//	  .OpenBracket()
				//	  .IsParamNull(Schema.Fields[_type])
				//	  .Or()
				//	  .Equals(Schema.Fields[_type])
				//	  .CloseBracket();

				//_readAllByCodeAndType = database.GetCommand(readAllByCodeAndType, Schema, new FieldList(Schema.Fields[_code], Schema.Fields[_type]), new FieldList());

				//var readAllByCodeAndTypeAndExpiryDate = database.CommandType == CommandType.StoredProcedure
				//	? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndTypeAndExpiryDate")
				//	: Query
				//	  .Select(Schema)
				//	  .From(Schema)
				//	  .Where()
				//	  .Like(Schema.Fields[_code])
				//	  .And()
				//	  .OpenBracket()
				//	  .IsParamNull(Schema.Fields[_type])
				//	  .Or()
				//	  .Equals(Schema.Fields[_type])
				//	  .CloseBracket()
				//	  .And()
				//	  .OpenBracket()
				//	  .IsNull(Schema.Fields[_expiryDate])
				//	  .Or()
				//	  .Equals(Schema.Fields[_expiryDate])
				//	  .CloseBracket();

				//_readAllByCodeAndTypeAndExpiryDate = database.GetCommand(readAllByCodeAndTypeAndExpiryDate, Schema, new FieldList(Schema.Fields[_code], Schema.Fields[_type], Schema.Fields[_expiryDate]), new FieldList());

				//if (database.CommandType == CommandType.Text)
				//{
				//	//var readSecurityIds = Query
				//	//	.Execute("SELECT group_concat(Id, ',') FROM Security");

				//	//_readSecurityIds = database.GetCommand(readSecurityIds, null, new FieldList(), new FieldList());

				//	var readAllByBoardAndType = Query
				//	                            .Select(Schema)
				//	                            .From(Schema)
				//	                            .Where()
				//	                            .Equals(Schema.Fields[_board])
				//	                            .And()
				//	                            .OpenBracket()
				//	                            .IsParamNull(Schema.Fields[_type])
				//	                            .Or()
				//	                            .Equals(Schema.Fields[_type])
				//	                            .CloseBracket();

				//	_readAllByBoardAndType = database.GetCommand(readAllByBoardAndType, Schema, new FieldList(Schema.Fields[_board], Schema.Fields[_type]), new FieldList());

				//	var readAllByTypeAndExpiryDate = Query
				//	                                 .Select(Schema)
				//	                                 .From(Schema)
				//	                                 .Where()
				//	                                 .Equals(Schema.Fields[_type])
				//	                                 .And()
				//	                                 .OpenBracket()
				//	                                 .IsNull(Schema.Fields[_expiryDate])
				//	                                 .Or()
				//	                                 .Equals(Schema.Fields[_expiryDate])
				//	                                 .CloseBracket();

				//	_readAllByTypeAndExpiryDate = database.GetCommand(readAllByTypeAndExpiryDate, Schema, new FieldList(Schema.Fields[_type], Schema.Fields[_expiryDate]), new FieldList());

				//	var readAllByType = Query
				//	                    .Select(Schema)
				//	                    .From(Schema)
				//	                    .Where()
				//	                    .Equals(Schema.Fields[_type]);

				//	_readAllByType = database.GetCommand(readAllByType, Schema, new FieldList(Schema.Fields[_type]), new FieldList());

				//	RemoveQuery = Query
				//	              .Delete()
				//	              .From(Schema)
				//	              .Where()
				//	              .Equals(Schema.Fields[_id]);
				//}

				((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
				((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
			}

			DelayAction IStorageEntityList<Security>.DelayAction => DelayAction;

			private Action<IEnumerable<Security>> _added;

			event Action<IEnumerable<Security>> ISecurityProvider.Added
			{
				add => _added += value;
				remove => _added -= value;
			}

			private Action<IEnumerable<Security>> _removed;

			event Action<IEnumerable<Security>> ISecurityProvider.Removed
			{
				add => _removed += value;
				remove => _removed -= value;
			}

			/// <inheritdoc />
			public IEnumerable<Security> Lookup(SecurityLookupMessage criteria)
			{
				var secId = criteria.SecurityId.ToStringId(nullIfEmpty: true);

				if (secId.IsEmpty())
					return this.Filter(criteria);

				var security = ReadById(secId);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}

			//private IEnumerable<Security> ReadAllByCodeAndType(Security criteria)
			//{
			//	var fields = new[]
			//	{
			//		new SerializationItem(Schema.Fields[_code], "%" + criteria.Code + "%"),
			//		new SerializationItem(Schema.Fields[_type], criteria.Type)
			//	};

			//	return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
			//}

			//private IEnumerable<Security> ReadAllByCodeAndTypeAndExpiryDate(Security criteria)
			//{
			//	if (criteria.ExpiryDate == null)
			//		throw new ArgumentNullException(nameof(criteria), "ExpiryDate == null");

			//	var fields = new[]
			//	{
			//		new SerializationItem(Schema.Fields[_code], "%" + criteria.Code + "%"),
			//		new SerializationItem(Schema.Fields[_type], criteria.Type),
			//		new SerializationItem(Schema.Fields[_expiryDate], criteria.ExpiryDate.Value)
			//	};

			//	return Database.ReadAll<Security>(_readAllByCodeAndTypeAndExpiryDate, new SerializationItemCollection(fields));
			//}

			//private IEnumerable<Security> ReadAllByBoardAndType(Security criteria)
			//{
			//	var fields = new[]
			//	{
			//		new SerializationItem(Schema.Fields[_board], criteria.Board.Code),
			//		new SerializationItem(Schema.Fields[_type], criteria.Type)
			//	};

			//	return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
			//}

			//private IEnumerable<Security> ReadAllByTypeAndExpiryDate(Security criteria)
			//{
			//	if (criteria.ExpiryDate == null)
			//		throw new ArgumentNullException(nameof(criteria), "ExpiryDate == null");

			//	var fields = new[]
			//	{
			//		new SerializationItem(Schema.Fields[_type], criteria.Type),
			//		new SerializationItem(Schema.Fields[_expiryDate], criteria.ExpiryDate.Value)
			//	};

			//	return Database.ReadAll<Security>(_readAllByTypeAndExpiryDate, new SerializationItemCollection(fields));
			//}

			//private IEnumerable<Security> ReadAllByType(Security criteria)
			//{
			//	var fields = new[]
			//	{
			//		new SerializationItem(Schema.Fields[_type], criteria.Type)
			//	};

			//	return Database.ReadAll<Security>(_readAllByType, new SerializationItemCollection(fields));
			//}

			/// <inheritdoc />
			public void Save(Security security, bool forced)
			{
				Save(security);
			}

			/// <inheritdoc />
			public override void Save(Security entity)
			{
				_registry.Exchanges.Save(entity.Board.Exchange);
				_registry.ExchangeBoards.Save(entity.Board);

				base.Save(entity);
			}

			///// <summary>
			///// To get identifiers of saved instruments.
			///// </summary>
			///// <returns>IDs securities.</returns>
			//public IEnumerable<string> GetSecurityIds()
			//{
			//	if (_readSecurityIds == null)
			//		return this.Select(s => s.Id);

			//	var str = _readSecurityIds.ExecuteScalar<string>(new SerializationItemCollection());
			//	return str.SplitByComma(",", true);
			//}

			/// <inheritdoc />
			protected override void OnAdd(Security entity)
			{
				_registry.Exchanges.Save(entity.Board.Exchange);
				_registry.ExchangeBoards.Save(entity.Board);

				base.OnAdd(entity);
			}

			/// <inheritdoc />
			public void Delete(Security security)
			{
				Remove(security);
			}

			/// <inheritdoc />
			public void DeleteBy(Security criteria)
			{
				this.Filter(criteria).ForEach(s => Remove(s));
			}

			void IDisposable.Dispose()
			{
			}
		}

		/// <summary>
		/// The class for the presentation in the form of stocks list, stored in the external storage.
		/// </summary>
		private class ExchangeList : BaseStorageEntityList<Exchange>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ExchangeList"/>.
			/// </summary>
			/// <param name="storage">The special interface for direct access to the storage.</param>
			public ExchangeList(IStorage storage)
				: base(storage)
			{
			}
		}

		/// <summary>
		/// The class for representation in the form of list of exchange sites, stored in the external storage.
		/// </summary>
		private class ExchangeBoardList : BaseStorageEntityList<ExchangeBoard>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ExchangeBoardList"/>.
			/// </summary>
			/// <param name="storage">The special interface for direct access to the storage.</param>
			public ExchangeBoardList(IStorage storage)
				: base(storage)
			{
			}

			///// <summary>
			///// To get identifiers.
			///// </summary>
			///// <returns>Identifiers.</returns>
			//public virtual IEnumerable<string> GetIds()
			//{
			//	return this.Select(b => b.Code);
			//}
		}

		///// <summary>
		///// The class for representation in the form of list of news, stored in the external storage.
		///// </summary>
		//private class NewsList : BaseStorageEntityList<News>
		//{
		//	/// <summary>
		//	/// Initializes a new instance of the <see cref="NewsList"/>.
		//	/// </summary>
		//	/// <param name="storage">The special interface for direct access to the storage.</param>
		//	public NewsList(IStorage storage)
		//		: base(storage)
		//	{
		//	}
		//}

		///// <summary>
		///// The class for representation in the form of list of orders, stored in external storage.
		///// </summary>
		//private class OrderList : BaseStorageEntityList<Order>
		//{
		//	/// <summary>
		//	/// Initializes a new instance of the <see cref="OrderList"/>.
		//	/// </summary>
		//	/// <param name="storage">The special interface for direct access to the storage.</param>
		//	public OrderList(IStorage storage)
		//		: base(storage)
		//	{
		//	}
		//}

		///// <summary>
		///// The class for representation in the form of list of orders with errors, stored in the external storage.
		///// </summary>
		//private class OrderFailList : BaseStorageEntityList<OrderFail>
		//{
		//	/// <summary>
		//	/// Initializes a new instance of the <see cref="OrderFailList"/>.
		//	/// </summary>
		//	/// <param name="storage">The special interface for direct access to the storage.</param>
		//	public OrderFailList(IStorage storage)
		//		: base(storage)
		//	{
		//	}
		//}

		///// <summary>
		///// The class for representation in the form of list of own trades, stored in external storage.
		///// </summary>
		//private class MyTradeList : BaseStorageEntityList<MyTrade>
		//{
		//	/// <summary>
		//	/// Initializes a new instance of the <see cref="MyTradeList"/>.
		//	/// </summary>
		//	/// <param name="storage">The special interface for direct access to the storage.</param>
		//	public MyTradeList(IStorage storage)
		//		: base(storage)
		//	{
		//		OverrideCreateDelete = true;
		//	}

		//	/// <summary>
		//	/// To get data from essence for creation.
		//	/// </summary>
		//	/// <param name="entity">Entity.</param>
		//	/// <returns>Data for creation.</returns>
		//	protected override SerializationItemCollection GetOverridedAddSource(MyTrade entity)
		//	{
		//		var source = CreateSource(entity);

		//		if (entity.Commission != null)
		//			source.Add(new SerializationItem<decimal>(new VoidField<decimal>("Commission"), entity.Commission.Value));

		//		return source;
		//	}

		//	/// <summary>
		//	/// To get data from essence for deletion.
		//	/// </summary>
		//	/// <param name="entity">Entity.</param>
		//	/// <returns>Data for deletion.</returns>
		//	protected override SerializationItemCollection GetOverridedRemoveSource(MyTrade entity)
		//	{
		//		return CreateSource(entity);
		//	}

		//	/// <summary>
		//	/// To load own trade.
		//	/// </summary>
		//	/// <param name="order">Order.</param>
		//	/// <param name="trade">Tick trade.</param>
		//	/// <returns>Own trade.</returns>
		//	public MyTrade ReadByOrderAndTrade(Order order, Trade trade)
		//	{
		//		return Read(CreateSource(order, trade));
		//	}

		//	/// <summary>
		//	/// To save the trading object.
		//	/// </summary>
		//	/// <param name="entity">The trading object.</param>
		//	public override void Save(MyTrade entity)
		//	{
		//		if (ReadByOrderAndTrade(entity.Order, entity.Trade) == null)
		//			Add(entity);
		//		//else
		//		//	Update(entity);
		//	}

		//	private static SerializationItemCollection CreateSource(MyTrade trade)
		//	{
		//		return CreateSource(trade.Order, trade.Trade);
		//	}

		//	private static SerializationItemCollection CreateSource(Order order, Trade trade)
		//	{
		//		return new SerializationItemCollection
		//		{
		//			new SerializationItem<long>(new VoidField<long>("Order"), order.TransactionId),
		//			new SerializationItem<string>(new VoidField<string>("Trade"), trade.Id == 0 ? trade.StringId : trade.Id.To<string>())
		//		};
		//	}
		//}

		//private class SecurityMyTradeList : MyTradeList
		//{
		//	public SecurityMyTradeList(IStorage storage, Security security)
		//		: base(storage)
		//	{
		//		AddFilter(Schema.Fields["Security"], security, () => security.Id);
		//	}
		//}

		//private class OrderMyTradeList : MyTradeList
		//{
		//	public OrderMyTradeList(IStorage storage, Order order)
		//		: base(storage)
		//	{
		//		AddFilter(Schema.Fields["Order"], order, () => order.Id);
		//	}
		//}

		/// <summary>
		/// The class for representation in the form of list of portfolios, stored in external storage.
		/// </summary>
		private class PortfolioList : BaseStorageEntityList<Portfolio>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="PortfolioList"/>.
			/// </summary>
			/// <param name="storage">The special interface for direct access to the storage.</param>
			public PortfolioList(IStorage storage)
				: base(storage)
			{
			}
		}

		/// <summary>
		/// The class for representation in the form of list of positions, stored in external storage.
		/// </summary>
		private class PositionList : BaseStorageEntityList<Position>, IStoragePositionList
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="PositionList"/>.
			/// </summary>
			/// <param name="storage">The special interface for direct access to the storage.</param>
			public PositionList(IStorage storage)
				: base(storage)
			{
			}

			DelayAction IStorageEntityList<Position>.DelayAction => DelayAction;

			/// <summary>
			/// To get data from essence for creation.
			/// </summary>
			/// <param name="entity">Entity.</param>
			/// <returns>Data for creation.</returns>
			protected override SerializationItemCollection GetOverridedAddSource(Position entity)
			{
				return CreateSource(entity);
			}

			/// <summary>
			/// To get data from essence for deletion.
			/// </summary>
			/// <param name="entity">Entity.</param>
			/// <returns>Data for deletion.</returns>
			protected override SerializationItemCollection GetOverridedRemoveSource(Position entity)
			{
				return CreateSource(entity);
			}

			/// <inheritdoc />
			public Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "")
			{
				return Read(CreateSource(security, portfolio));
			}

			/// <inheritdoc />
			public override void Save(Position entity)
			{
				if (GetPosition(entity.Portfolio, entity.Security) == null)
					Add(entity);
				else
					UpdateByKey(entity);
			}

			private void UpdateByKey(Position position)
			{
				var keyFields = new[]
				{
					Schema.Fields[nameof(Portfolio)],
					Schema.Fields[nameof(Security)]
				};
				var fields = Schema.Fields.Where(f => !keyFields.Contains(f)).ToArray();

				Database.Update(position, new FieldList(keyFields), new FieldList(fields));
			}

			private static SerializationItemCollection CreateSource(Position position)
			{
				return CreateSource(position.Security, position.Portfolio);
			}

			private static SerializationItemCollection CreateSource(Security security, Portfolio portfolio)
			{
				return new SerializationItemCollection
				{
					new SerializationItem<string>(new VoidField<string>(nameof(Security)), security.Id),
					new SerializationItem<string>(new VoidField<string>(nameof(Portfolio)), portfolio.GetUniqueId())
				};
			}
		}

		private class SubscriptionList : BaseStorageEntityList<MarketDataMessage>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="SubscriptionList"/>.
			/// </summary>
			/// <param name="storage">The special interface for direct access to the storage.</param>
			public SubscriptionList(IStorage storage)
				: base(storage)
			{
			}
		}

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
			Subscriptions = new SubscriptionList(storage);
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
		public IStorageEntityList<MarketDataMessage> Subscriptions { get; }

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
				((SubscriptionList)Subscriptions).DelayAction = _delayAction;
				//News.DelayAction = _delayAction;
			}
		}
	}
}