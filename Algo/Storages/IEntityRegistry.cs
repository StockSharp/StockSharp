namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface describing the trade objects storage.
	/// </summary>
	public interface IEntityRegistry
	{
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
		/// The list of portfolios.
		/// </summary>
		IStorageEntityList<Portfolio> Portfolios { get; }

		/// <summary>
		/// The list of positions.
		/// </summary>
		IStorageEntityList<Position> Positions { get; }

		/// <summary>
		/// The list of own trades.
		/// </summary>
		IStorageEntityList<MyTrade> MyTrades { get; }

		/// <summary>
		/// The list of tick trades.
		/// </summary>
		IStorageEntityList<Trade> Trades { get; }

		/// <summary>
		/// The list of orders.
		/// </summary>
		IStorageEntityList<Order> Orders { get; }

		/// <summary>
		/// The list of orders registration and cancelling errors.
		/// </summary>
		IStorageEntityList<OrderFail> OrderFails { get; }

		/// <summary>
		/// The list of news.
		/// </summary>
		IStorageEntityList<News> News { get; }
	}
}