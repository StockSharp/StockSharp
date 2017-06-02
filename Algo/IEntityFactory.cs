#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: IEntityFactory.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface of the business-essences factory (<see cref="Security"/>, <see cref="Order"/> etc.).
	/// </summary>
	public interface IEntityFactory
	{
		/// <summary>
		/// To create the instrument by the identifier.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Created instrument.</returns>
		Security CreateSecurity(string id);

		/// <summary>
		/// To create the portfolio by the account number.
		/// </summary>
		/// <param name="name">Account number.</param>
		/// <returns>Created portfolio.</returns>
		Portfolio CreatePortfolio(string name);

		/// <summary>
		/// Create position.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="security">Security.</param>
		/// <returns>Created position.</returns>
		Position CreatePosition(Portfolio portfolio, Security security);

		/// <summary>
		/// To create the tick trade by its identifier.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="id">The trade identifier (equals <see langword="null" />, if string identifier is used).</param>
		/// <param name="stringId">Trade ID (as string, if electronic board does not use numeric order ID representation).</param>
		/// <returns>Created trade.</returns>
		Trade CreateTrade(Security security, long? id, string stringId);

		/// <summary>
		/// To create the order by the transaction identifier.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="type">Order type.</param>
		/// <param name="transactionId">The identifier of the order registration transaction.</param>
		/// <returns>Created order.</returns>
		Order CreateOrder(Security security, OrderTypes? type, long transactionId);

		/// <summary>
		/// To create the error description for the order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="error">The system description of error.</param>
		/// <returns>Created error description.</returns>
		OrderFail CreateOrderFail(Order order, Exception error);

		/// <summary>
		/// To create own trade.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Created own trade.</returns>
		MyTrade CreateMyTrade(Order order, Trade trade);

		/// <summary>
		/// To create the order book for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Created order book.</returns>
		MarketDepth CreateMarketDepth(Security security);

		/// <summary>
		/// To create the string of orders log.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Order log item.</returns>
		OrderLogItem CreateOrderLogItem(Order order, Trade trade);

		/// <summary>
		/// To create news.
		/// </summary>
		/// <returns>News.</returns>
		News CreateNews();

		/// <summary>
		/// To create exchange.
		/// </summary>
		/// <param name="code"><see cref="Exchange.Name"/> value.</param>
		/// <returns>Exchange.</returns>
		Exchange CreateExchange(string code);

		/// <summary>
		/// To create exchange.
		/// </summary>
		/// <param name="code"><see cref="ExchangeBoard.Code"/> value.</param>
		/// <param name="exchange"><see cref="ExchangeBoard.Exchange"/> value.</param>
		/// <returns>Exchange.</returns>
		ExchangeBoard CreateBoard(string code, Exchange exchange);
	}

	/// <summary>
	/// Entity factory (<see cref="Security"/>, <see cref="Order"/> etc.).
	/// </summary>
	public class EntityFactory : IEntityFactory, IStorage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EntityFactory"/>.
		/// </summary>
		public EntityFactory()
		{
		}

		/// <summary>
		/// To create the instrument by the identifier.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Created instrument.</returns>
		public virtual Security CreateSecurity(string id)
		{
			return new Security { Id = id };
		}

		/// <summary>
		/// To create the portfolio by the account number.
		/// </summary>
		/// <param name="name">Account number.</param>
		/// <returns>Created portfolio.</returns>
		public virtual Portfolio CreatePortfolio(string name)
		{
			return new Portfolio { Name = name };
		}

		/// <summary>
		/// Create position.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="security">Security.</param>
		/// <returns>Created position.</returns>
		public virtual Position CreatePosition(Portfolio portfolio, Security security)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return new Position
			{
				Portfolio = portfolio,
				Security = security,
			};
		}

		/// <summary>
		/// To create the tick trade by its identifier.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="id">The trade identifier (equals <see langword="null" />, if string identifier is used).</param>
		/// <param name="stringId">Trade ID (as string, if electronic board does not use numeric order ID representation).</param>
		/// <returns>Created trade.</returns>
		public virtual Trade CreateTrade(Security security, long? id, string stringId)
		{
			return new Trade { Security = security, Id = id ?? 0, StringId = stringId };
		}

		/// <summary>
		/// To create the order by the transaction identifier.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="type">Order type.</param>
		/// <param name="transactionId">The identifier of the order registration transaction.</param>
		/// <returns>Created order.</returns>
		public virtual Order CreateOrder(Security security, OrderTypes? type, long transactionId)
		{
			return new Order
			{
				Security = security,
				TransactionId = transactionId,
				Type = type,
			};
		}

		/// <summary>
		/// To create the error description for the order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="error">The system description of error.</param>
		/// <returns>Created error description.</returns>
		public virtual OrderFail CreateOrderFail(Order order, Exception error)
		{
			return new OrderFail { Order = order, Error = error };
		}

		/// <summary>
		/// To create own trade.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Created own trade.</returns>
		public virtual MyTrade CreateMyTrade(Order order, Trade trade)
		{
			return new MyTrade
			{
				Order = order,
				Trade = trade,
			};
		}

		/// <summary>
		/// To create the order book for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Created order book.</returns>
		public virtual MarketDepth CreateMarketDepth(Security security)
		{
			return new MarketDepth(security);
		}

		/// <summary>
		/// To create the string of orders log.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Order log item.</returns>
		public virtual OrderLogItem CreateOrderLogItem(Order order, Trade trade)
		{
			return new OrderLogItem
			{
				Order = order,
				Trade = trade,
			};
		}

		/// <summary>
		/// To create news.
		/// </summary>
		/// <returns>News.</returns>
		public virtual News CreateNews()
		{
			return new News();
		}

		/// <summary>
		/// To create exchange.
		/// </summary>
		/// <param name="code"><see cref="Exchange.Name"/> value.</param>
		/// <returns>Exchange.</returns>
		public Exchange CreateExchange(string code)
		{
			return new Exchange { Name = code };
		}

		/// <summary>
		/// To create exchange.
		/// </summary>
		/// <param name="code"><see cref="ExchangeBoard.Code"/> value.</param>
		/// <param name="exchange"><see cref="ExchangeBoard.Exchange"/> value.</param>
		/// <returns>Exchange.</returns>
		public ExchangeBoard CreateBoard(string code, Exchange exchange)
		{
			return new ExchangeBoard { Code = code, Exchange = exchange };
		}

		long IStorage.GetCount<TEntity>()
		{
			throw new NotSupportedException();
		}

		TEntity IStorage.Add<TEntity>(TEntity entity)
		{
			return entity;
		}

		TEntity IStorage.GetBy<TEntity>(SerializationItemCollection by)
		{
			throw new NotSupportedException();
		}

		TEntity IStorage.GetById<TEntity>(object id)
		{
			if (typeof(TEntity) == typeof(Security))
				return CreateSecurity((string)id).To<TEntity>();
			else if (typeof(TEntity) == typeof(Portfolio))
				return CreatePortfolio((string)id).To<TEntity>();
			else
				throw new NotSupportedException();
		}

		IEnumerable<TEntity> IStorage.GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			throw new NotSupportedException();
		}

		TEntity IStorage.Update<TEntity>(TEntity entity)
		{
			return entity;
		}

		void IStorage.Remove<TEntity>(TEntity entity)
		{
		}

		void IStorage.Clear<TEntity>()
		{
		}

		void IStorage.ClearCache()
		{
			throw new NotSupportedException();
		}

		IBatchContext IStorage.BeginBatch()
		{
			throw new NotSupportedException();
		}

		void IStorage.CommitBatch()
		{
			throw new NotSupportedException();
		}

		void IStorage.EndBatch()
		{
			throw new NotSupportedException();
		}

		event Action<object> IStorage.Added
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<object> IStorage.Updated
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<object> IStorage.Removed
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}
	}
}