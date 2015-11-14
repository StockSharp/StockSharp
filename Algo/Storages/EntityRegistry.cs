namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The storage of trade objects.
	/// </summary>
	public class EntityRegistry : IEntityRegistry
	{
		private DelayAction _delayAction;

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
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

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
		/// The special interface for direct access to the storage.
		/// </summary>
		public IStorage Storage { get; }

		/// <summary>
		/// List of exchanges.
		/// </summary>
		public virtual IStorageEntityList<Exchange> Exchanges { get; }

		/// <summary>
		/// The list of stock boards.
		/// </summary>
		public virtual IStorageEntityList<ExchangeBoard> ExchangeBoards { get; }

		/// <summary>
		/// The list of instruments.
		/// </summary>
		public virtual IStorageSecurityList Securities { get; }

		/// <summary>
		/// The list of portfolios.
		/// </summary>
		public virtual IStorageEntityList<Portfolio> Portfolios { get; }

		/// <summary>
		/// The list of positions.
		/// </summary>
		public virtual IStorageEntityList<Position> Positions { get; }

		/// <summary>
		/// The list of own trades.
		/// </summary>
		public virtual IStorageEntityList<MyTrade> MyTrades { get; }

		/// <summary>
		/// The list of tick trades.
		/// </summary>
		public virtual IStorageEntityList<Trade> Trades { get; }

		/// <summary>
		/// The list of orders.
		/// </summary>
		public virtual IStorageEntityList<Order> Orders { get; }

		/// <summary>
		/// The list of orders registration and cancelling errors.
		/// </summary>
		public virtual IStorageEntityList<OrderFail> OrderFails { get; }

		/// <summary>
		/// The list of news.
		/// </summary>
		public virtual IStorageEntityList<News> News { get; }

		/// <summary>
		/// The time delayed action.
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
		///// Äîáàâèòü èíñòðóìåíò â î÷åðåäü ñîõðàíåíèÿ.
		///// </summary>
		///// <param name="security">Èíñòðóìåíò.</param>
		//public void EnqueueSecurity(Security security)
		//{
		//	if (security == null)
		//		throw new ArgumentNullException("security");

		//	SaveExchangeBoard(security.Board);

		//	Securities.Save(security);
		//}

		///// <summary>
		///// Ñîõðàíèòü áèðæåâóþ ïëîùàäêó. Ó÷èòûâàåòñÿ ñîõðàíåíèå êàê ñàìîé ïëîùàäêè, òàê è áèðæó <see cref="ExchangeBoard.Exchange"/>.
		///// </summary>
		///// <param name="board">Áèðæåâàÿ ïëîùàäêà.</param>
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