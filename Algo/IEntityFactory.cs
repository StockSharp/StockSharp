namespace StockSharp.Algo;

/// <summary>
/// The interface of the business-essences factory (<see cref="Security"/>, <see cref="Order"/> etc.).
/// </summary>
[Obsolete]
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