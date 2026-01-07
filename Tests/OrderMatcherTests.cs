namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class OrderMatcherTests : BaseTestClass
{
	private static SecurityId CreateSecId() => Helper.CreateSecurityId();

	private static OrderBook CreateBookWithSpread(decimal bid = 100, decimal ask = 101, decimal volume = 10)
	{
		var book = new OrderBook(CreateSecId());
		book.UpdateLevel(Sides.Buy, bid, volume);
		book.UpdateLevel(Sides.Sell, ask, volume);
		return book;
	}

	private static MatchingSettings DefaultSettings => new()
	{
		PriceStep = 1,
		VolumeStep = 1,
		SpreadSize = 1,
		MaxDepth = 10,
	};

	[TestMethod]
	public void Match_LimitBuy_BelowBestAsk_NoMatch()
	{
		var book = CreateBookWithSpread();
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100, // At bid, below ask
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		IsTrue(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Active, result.FinalState);
	}

	[TestMethod]
	public void Match_LimitBuy_AtBestAsk_FullMatch()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101, // At best ask
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		IsFalse(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Done, result.FinalState);
		AreEqual(1, result.Trades.Count);
		AreEqual(101m, result.Trades[0].Price);
		AreEqual(5m, result.Trades[0].Volume);
	}

	[TestMethod]
	public void Match_LimitBuy_AboveBestAsk_FullMatchAtBestPrice()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 105, // Above best ask
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		AreEqual(101m, result.Trades[0].Price); // Matched at best ask, not order price
	}

	[TestMethod]
	public void Match_LimitSell_AboveBestBid_NoMatch()
	{
		var book = CreateBookWithSpread();
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Sell,
			Price = 101, // At ask, above bid
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		IsTrue(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Active, result.FinalState);
	}

	[TestMethod]
	public void Match_LimitSell_AtBestBid_FullMatch()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Sell,
			Price = 100, // At best bid
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		IsFalse(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Done, result.FinalState);
		AreEqual(100m, result.Trades[0].Price);
	}

	[TestMethod]
	public void Match_PartialFill_RemainingGoesToBook()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 5);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Balance = 10,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		IsTrue(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Active, result.FinalState);
		AreEqual(5m, result.Trades[0].Volume);
	}

	[TestMethod]
	public void Match_MarketOrder_MatchesAtAnyPrice()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 0, // Market order, no price
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Market,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		IsFalse(result.ShouldPlaceInBook); // Market orders never go to book
		AreEqual(OrderStates.Done, result.FinalState);
	}

	[TestMethod]
	public void Match_MarketOrder_NoLiquidity_NoMatch()
	{
		var book = new OrderBook(CreateSecId()); // Empty book
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Market,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		IsFalse(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Done, result.FinalState);
	}

	[TestMethod]
	public void Match_FOK_FullLiquidity_FullMatch()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel, // FOK
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		AreEqual(OrderStates.Done, result.FinalState);
	}

	[TestMethod]
	public void Match_FOK_PartialLiquidity_NoMatch()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 3);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Balance = 10, // Needs 10, only 3 available
			Volume = 10,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel, // FOK
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.HasTrades);
		AreEqual(10m, result.RemainingVolume); // Full volume rejected
		AreEqual(OrderStates.Done, result.FinalState);
		IsFalse(result.ShouldPlaceInBook);
	}

	[TestMethod]
	public void Match_IOC_PartialLiquidity_PartialMatch()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 3);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Balance = 10,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance, // IOC
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(7m, result.RemainingVolume); // 10 - 3 = 7 cancelled
		AreEqual(OrderStates.Done, result.FinalState);
		IsFalse(result.ShouldPlaceInBook); // IOC doesn't go to book
		AreEqual(3m, result.Trades[0].Volume);
	}

	[TestMethod]
	public void Match_IOC_NoLiquidity_CancelAll()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99, // Below best ask, no match
			Balance = 10,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance, // IOC
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.HasTrades);
		AreEqual(10m, result.RemainingVolume);
		AreEqual(OrderStates.Done, result.FinalState);
		IsFalse(result.ShouldPlaceInBook);
	}

	[TestMethod]
	public void Match_PostOnly_WouldCross_Rejected()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101, // Would cross (match with ask)
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.IsRejected);
		IsFalse(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		AreEqual(OrderStates.Done, result.FinalState);
		IsFalse(result.ShouldPlaceInBook);
	}

	[TestMethod]
	public void Match_PostOnly_WouldNotCross_PlacedInBook()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 99, // Would not cross (below bid)
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsFalse(result.IsRejected);
		IsFalse(result.HasTrades);
		AreEqual(5m, result.RemainingVolume);
		IsTrue(result.ShouldPlaceInBook);
		AreEqual(OrderStates.Active, result.FinalState);
	}

	[TestMethod]
	public void Match_MultipleLevels_MatchesAcrossLevels()
	{
		var book = new OrderBook(CreateSecId());
		book.UpdateLevel(Sides.Sell, 101, 5);
		book.UpdateLevel(Sides.Sell, 102, 10);
		book.UpdateLevel(Sides.Sell, 103, 15);

		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 105, // Can match all levels
			Balance = 12,
			Volume = 12,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		var result = matcher.Match(order, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(0m, result.RemainingVolume);
		AreEqual(2, result.Trades.Count); // Two levels consumed
		AreEqual(101m, result.Trades[0].Price);
		AreEqual(5m, result.Trades[0].Volume);
		AreEqual(102m, result.Trades[1].Price);
		AreEqual(7m, result.Trades[1].Volume);
	}

	[TestMethod]
	public void WouldCross_BuyAboveAsk_ReturnsTrue()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Buy,
			Price = 101,
			OrderType = OrderTypes.Limit,
		};

		IsTrue(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void WouldCross_BuyBelowAsk_ReturnsFalse()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Buy,
			Price = 100,
			OrderType = OrderTypes.Limit,
		};

		IsFalse(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void WouldCross_SellBelowBid_ReturnsTrue()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Sell,
			Price = 100,
			OrderType = OrderTypes.Limit,
		};

		IsTrue(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void WouldCross_SellAboveBid_ReturnsFalse()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Sell,
			Price = 101,
			OrderType = OrderTypes.Limit,
		};

		IsFalse(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void WouldCross_EmptyBook_ReturnsFalse()
	{
		var book = new OrderBook(CreateSecId());
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Buy,
			Price = 100,
			OrderType = OrderTypes.Limit,
		};

		IsFalse(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void WouldCross_MarketOrder_ReturnsTrue()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var order = new EmulatorOrder
		{
			Side = Sides.Buy,
			OrderType = OrderTypes.Market,
		};

		IsTrue(matcher.WouldCross(order, book));
	}

	[TestMethod]
	public void GetMarketPrice_Buy_ReturnsBestAsk()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var price = matcher.GetMarketPrice(Sides.Buy, book);

		AreEqual(101m, price);
	}

	[TestMethod]
	public void GetMarketPrice_Sell_ReturnsBestBid()
	{
		var book = CreateBookWithSpread(bid: 100, ask: 101, volume: 10);
		var matcher = new OrderMatcher();

		var price = matcher.GetMarketPrice(Sides.Sell, book);

		AreEqual(100m, price);
	}

	[TestMethod]
	public void GetMarketPrice_EmptyBook_ReturnsNull()
	{
		var book = new OrderBook(CreateSecId());
		var matcher = new OrderMatcher();

		var price = matcher.GetMarketPrice(Sides.Buy, book);

		IsNull(price);
	}

	[TestMethod]
	public void Match_UserOrdersInBook_ReturnsMatchedOrders()
	{
		var book = new OrderBook(CreateSecId());

		// Add user order to book
		var userOrder = new EmulatorOrder
		{
			TransactionId = 100,
			Side = Sides.Sell,
			Price = 101,
			Balance = 10,
			Volume = 10,
			PortfolioName = "UserPortfolio"
		};
		book.AddQuote(userOrder);

		var matcher = new OrderMatcher();

		var incomingOrder = new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 101,
			Balance = 5,
			Volume = 5,
			OrderType = OrderTypes.Limit,
		};

		var result = matcher.Match(incomingOrder, book, DefaultSettings);

		IsTrue(result.HasTrades);
		AreEqual(1, result.MatchedOrders.Count);
		AreEqual("UserPortfolio", result.MatchedOrders[0].PortfolioName);
	}
}
