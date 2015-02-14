namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Хранилище торговых объектов.
	/// </summary>
	public class EntityRegistry : IEntityRegistry
	{
		private DelayAction _delayAction;

		/// <summary>
		/// Создать <see cref="EntityRegistry"/>.
		/// </summary>
		public EntityRegistry()
			: this(new InMemoryStorage())
		{
		}

		/// <summary>
		/// Создать <see cref="EntityRegistry"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public EntityRegistry(IStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			Storage = storage;

			ConfigManager.TryRegisterService(storage);

			Exchanges = new ExchangeList(storage) { BulkLoad = true };
			ExchangeBoards = new ExchangeBoardList(storage) { BulkLoad = true };
			Securities = new SecurityList(this);
			Trades = new TradeList(storage);
			MyTrades = new MyTradeList(storage);
			Orders = new OrderList(storage);
			OrderFails = new OrderFailList(storage);
			Portfolios = new PortfolioList(storage);
			Positions = new PositionList(storage);
			News = new NewsList(storage);
		}

		/// <summary>
		/// Специальный интерфейс для прямого доступа к хранилищу.
		/// </summary>
		public IStorage Storage { get; private set; }

		/// <summary>
		/// Список бирж.
		/// </summary>
		public virtual IStorageEntityList<Exchange> Exchanges { get; private set; }

		/// <summary>
		/// Список биржевых площадок.
		/// </summary>
		public virtual IStorageEntityList<ExchangeBoard> ExchangeBoards { get; private set; }

		/// <summary>
		/// Список инструментов.
		/// </summary>
		public virtual IStorageSecurityList Securities { get; private set; }

		/// <summary>
		/// Список портфелей.
		/// </summary>
		public virtual IStorageEntityList<Portfolio> Portfolios { get; private set; }

		/// <summary>
		/// Список позиций.
		/// </summary>
		public virtual IStorageEntityList<Position> Positions { get; private set; }

		/// <summary>
		/// Список собственных сделок.
		/// </summary>
		public virtual IStorageEntityList<MyTrade> MyTrades { get; private set; }

		/// <summary>
		/// Список тиковых сделок.
		/// </summary>
		public virtual IStorageEntityList<Trade> Trades { get; private set; }

		/// <summary>
		/// Список заявок.
		/// </summary>
		public virtual IStorageEntityList<Order> Orders { get; private set; }

		/// <summary>
		/// Список ошибок регистрации и снятия заявок.
		/// </summary>
		public virtual IStorageEntityList<OrderFail> OrderFails { get; private set; }

		/// <summary>
		/// Список новостей.
		/// </summary>
		public virtual IStorageEntityList<News> News { get; private set; }

		/// <summary>
		/// Отложенное действие.
		/// </summary>
		public DelayAction DelayAction
		{
			get { return _delayAction; }
			set
			{
				_delayAction = value;

				Exchanges.DelayAction = _delayAction;
				ExchangeBoards.DelayAction = _delayAction;
				Securities.DelayAction = _delayAction;
				Trades.DelayAction = _delayAction;
				MyTrades.DelayAction = _delayAction;
				Orders.DelayAction = _delayAction;
				OrderFails.DelayAction = _delayAction;
				Portfolios.DelayAction = _delayAction;
				Positions.DelayAction = _delayAction;
				News.DelayAction = _delayAction;
			}
		}

		///// <summary>
		///// Добавить инструмент в очередь сохранения.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		//public void EnqueueSecurity(Security security)
		//{
		//	if (security == null)
		//		throw new ArgumentNullException("security");

		//	SaveExchangeBoard(security.Board);

		//	Securities.Save(security);
		//}

		///// <summary>
		///// Сохранить биржевую площадку. Учитывается сохранение как самой площадки, так и биржу <see cref="ExchangeBoard.Exchange"/>.
		///// </summary>
		///// <param name="board">Биржевая площадка.</param>
		//public void SaveExchangeBoard(ExchangeBoard board)
		//{
		//	if (board == null)
		//		throw new ArgumentNullException("board");

		//	if (board.ExtensionInfo == null || board.Exchange.ExtensionInfo == null)
		//		throw new InvalidOperationException();

		//	Exchanges.Save(board.Exchange);
		//	ExchangeBoards.Save(board);
		//}
	}
}