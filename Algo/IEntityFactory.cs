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
	/// Интерфейс фабрики бизнес-сущностей (<see cref="Security"/>, <see cref="Order"/> и т.д.).
	/// </summary>
	public interface IEntityFactory
	{
		/// <summary>
		/// Создать инструмент по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <returns>Созданный инструмент.</returns>
		Security CreateSecurity(string id);

		/// <summary>
		/// Создать портфель по номеру счета.
		/// </summary>
		/// <param name="name">Номер счета.</param>
		/// <returns>Созданный портфель.</returns>
		Portfolio CreatePortfolio(string name);

		/// <summary>
		/// Создать позицию.
		/// </summary>
		/// <param name="portfolio">Портфель.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Созданная позиция.</returns>
		Position CreatePosition(Portfolio portfolio, Security security);

		/// <summary>
		/// Создать тиковую сделку по ее номеру.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="id">Номер сделки.</param>
		/// <param name="stringId">Номер сделки (ввиде строки, если электронная площадка не использует числовое представление идентификатора сделки).</param>
		/// <returns>Созданная сделка.</returns>
		Trade CreateTrade(Security security, long id, string stringId);

		/// <summary>
		/// Создать заявку по номеру транзакции.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="type">Тип заявки.</param>
		/// <param name="transactionId">Номер транзакции регистрации заявки.</param>
		/// <returns>Созданная заявка.</returns>
		Order CreateOrder(Security security, OrderTypes type, long transactionId);

		/// <summary>
		/// Создать описание ошибки для заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="error">Системное описание ошибки.</param>
		/// <returns>Созданное описание ошибки.</returns>
		OrderFail CreateOrderFail(Order order, Exception error);

		/// <summary>
		/// Создать собственную сделку.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Созданная собственная сделка.</returns>
		MyTrade CreateMyTrade(Order order, Trade trade);

		/// <summary>
		/// Создать стакан для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Созданный стакан.</returns>
		MarketDepth CreateMarketDepth(Security security);

		/// <summary>
		/// Создать строчку лога заявок.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Строчка лога заявок.</returns>
		OrderLogItem CreateOrderLogItem(Order order, Trade trade);

		/// <summary>
		/// Создать новость.
		/// </summary>
		/// <returns>Новость.</returns>
		News CreateNews();
	}

	/// <summary>
	/// Фабрика бизнес-сущностей (<see cref="Security"/>, <see cref="Order"/> и т.д.).
	/// </summary>
	public class EntityFactory : IEntityFactory, IStorage
	{
		static EntityFactory()
		{
			Instance = new EntityFactory();
		}

		/// <summary>
		/// Создать <see cref="EntityFactory"/>.
		/// </summary>
		public EntityFactory()
		{
		}

		/// <summary>
		/// Статический объект <see cref="EntityFactory"/>, который необходимо использовать, если требуется поддержать ссылочную целостность между разными подключениями.
		/// </summary>
		public static EntityFactory Instance { get; private set; }

		/// <summary>
		/// Создать инструмент по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <returns>Созданный инструмент.</returns>
		public virtual Security CreateSecurity(string id)
		{
			return new Security { Id = id };
		}

		/// <summary>
		/// Создать портфель по номеру счета.
		/// </summary>
		/// <param name="name">Номер счета.</param>
		/// <returns>Созданный портфель.</returns>
		public virtual Portfolio CreatePortfolio(string name)
		{
			return new Portfolio { Name = name };
		}

		/// <summary>
		/// Создать позицию.
		/// </summary>
		/// <param name="portfolio">Портфель.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Созданная позиция.</returns>
		public virtual Position CreatePosition(Portfolio portfolio, Security security)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (security == null)
				throw new ArgumentNullException("security");

			return new Position
			{
				Portfolio = portfolio,
				Security = security,
			};
		}

		/// <summary>
		/// Создать тиковую сделку по ее номеру.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="id">Номер сделки.</param>
		/// <param name="stringId">Номер сделки (ввиде строки, если электронная площадка не использует числовое представление идентификатора сделки).</param>
		/// <returns>Созданная сделка.</returns>
		public virtual Trade CreateTrade(Security security, long id, string stringId)
		{
			return new Trade { Security = security, Id = id, StringId = stringId };
		}

		/// <summary>
		/// Создать заявку по номеру транзакции.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="type">Тип заявки.</param>
		/// <param name="transactionId">Номер транзакции регистрации заявки.</param>
		/// <returns>Созданная заявка.</returns>
		public virtual Order CreateOrder(Security security, OrderTypes type, long transactionId)
		{
			return new Order
			{
				Security = security,
				TransactionId = transactionId,
				Type = type,
			};
		}

		/// <summary>
		/// Создать описание ошибки для заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="error">Системное описание ошибки.</param>
		/// <returns>Созданное описание ошибки.</returns>
		public virtual OrderFail CreateOrderFail(Order order, Exception error)
		{
			return new OrderFail { Order = order, Error = error };
		}

		/// <summary>
		/// Создать собственную сделку.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Созданная собственная сделка.</returns>
		public virtual MyTrade CreateMyTrade(Order order, Trade trade)
		{
			return new MyTrade
			{
				Order = order,
				Trade = trade,
			};
		}

		/// <summary>
		/// Создать стакан для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Созданный стакан.</returns>
		public virtual MarketDepth CreateMarketDepth(Security security)
		{
			return new MarketDepth(security);
		}

		/// <summary>
		/// Создать строчку лога заявок.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Строчка лога заявок.</returns>
		public virtual OrderLogItem CreateOrderLogItem(Order order, Trade trade)
		{
			return new OrderLogItem
			{
				Order = order,
				Trade = trade,
			};
		}

		/// <summary>
		/// Создать новость.
		/// </summary>
		/// <returns>Новость.</returns>
		public virtual News CreateNews()
		{
			return new News();
		}

		long IStorage.GetCount<TEntity>()
		{
			throw new NotSupportedException();
		}

		TEntity IStorage.Add<TEntity>(TEntity entity)
		{
			return entity;
		}

		TEntity IStorage.GetBy<TEntity>(SerializationItemCollection @by)
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

		BatchContext IStorage.BeginBatch()
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
			add { throw new NotSupportedException(); }
			remove { throw new NotSupportedException(); }
		}

		event Action<object> IStorage.Updated
		{
			add { throw new NotSupportedException(); }
			remove { throw new NotSupportedException(); }
		}

		event Action<object> IStorage.Removed
		{
			add { throw new NotSupportedException(); }
			remove { throw new NotSupportedException(); }
		}
	}
}