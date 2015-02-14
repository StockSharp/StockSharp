namespace StockSharp.Algo.Storages
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс, описывающий хранилище торговых объектов.
	/// </summary>
	public interface IEntityRegistry
	{
		/// <summary>
		/// Список бирж.
		/// </summary>
		IStorageEntityList<Exchange> Exchanges { get; }

		/// <summary>
		/// Список биржевых площадок.
		/// </summary>
		IStorageEntityList<ExchangeBoard> ExchangeBoards { get; }

		/// <summary>
		/// Список инструментов.
		/// </summary>
		IStorageSecurityList Securities { get; }

		/// <summary>
		/// Список портфелей.
		/// </summary>
		IStorageEntityList<Portfolio> Portfolios { get; }

		/// <summary>
		/// Список позиций.
		/// </summary>
		IStorageEntityList<Position> Positions { get; }

		/// <summary>
		/// Список собственных сделок.
		/// </summary>
		IStorageEntityList<MyTrade> MyTrades { get; }

		/// <summary>
		/// Список тиковых сделок.
		/// </summary>
		IStorageEntityList<Trade> Trades { get; }

		/// <summary>
		/// Список заявок.
		/// </summary>
		IStorageEntityList<Order> Orders { get; }

		/// <summary>
		/// Список ошибок регистрации и снятия заявок.
		/// </summary>
		IStorageEntityList<OrderFail> OrderFails { get; }

		/// <summary>
		/// Список новостей.
		/// </summary>
		IStorageEntityList<News> News { get; }
	}
}