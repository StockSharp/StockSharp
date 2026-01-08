namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class OrderBookTests : BaseTestClass
{
	private static SecurityId CreateSecId() => Helper.CreateSecurityId();

	[TestMethod]
	public void AddQuote_Bid_IncreasesTotalVolume()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Balance = 10,
			Volume = 10,
		});

		AreEqual(10m, book.TotalBidVolume);
		AreEqual(0m, book.TotalAskVolume);
		AreEqual(1, book.BidLevels);
		AreEqual(0, book.AskLevels);
	}

	[TestMethod]
	public void AddQuote_Ask_IncreasesTotalVolume()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Sell,
			Price = 101,
			Balance = 15,
			Volume = 15,
		});

		AreEqual(0m, book.TotalBidVolume);
		AreEqual(15m, book.TotalAskVolume);
		AreEqual(0, book.BidLevels);
		AreEqual(1, book.AskLevels);
	}

	[TestMethod]
	public void BestBid_ReturnsHighestBidPrice()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 98, Balance = 5, Volume = 5 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 3, Side = Sides.Buy, Price = 99, Balance = 7, Volume = 7 });

		var best = book.BestBid;
		IsNotNull(best);
		AreEqual(100m, best.Value.price);
		AreEqual(10m, best.Value.volume);
	}

	[TestMethod]
	public void BestAsk_ReturnsLowestAskPrice()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Sell, Price = 102, Balance = 5, Volume = 5 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Sell, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 3, Side = Sides.Sell, Price = 101, Balance = 7, Volume = 7 });

		var best = book.BestAsk;
		IsNotNull(best);
		AreEqual(100m, best.Value.price);
		AreEqual(10m, best.Value.volume);
	}

	[TestMethod]
	public void BestBid_EmptyBook_ReturnsNull()
	{
		var book = new OrderBook(CreateSecId());
		IsNull(book.BestBid);
	}

	[TestMethod]
	public void BestAsk_EmptyBook_ReturnsNull()
	{
		var book = new OrderBook(CreateSecId());
		IsNull(book.BestAsk);
	}

	[TestMethod]
	public void RemoveQuote_ExistingOrder_RemovesAndUpdatesVolume()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Buy, Price = 100, Balance = 5, Volume = 5 });

		AreEqual(15m, book.TotalBidVolume);

		var removed = book.RemoveQuote(1, Sides.Buy, 100);

		IsTrue(removed);
		AreEqual(5m, book.TotalBidVolume);
		AreEqual(1, book.BidLevels);
	}

	[TestMethod]
	public void RemoveQuote_LastOrderAtLevel_RemovesLevel()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });

		var removed = book.RemoveQuote(1, Sides.Buy, 100);

		IsTrue(removed);
		AreEqual(0m, book.TotalBidVolume);
		AreEqual(0, book.BidLevels);
		IsNull(book.BestBid);
	}

	[TestMethod]
	public void RemoveQuote_NonExistingOrder_ReturnsFalse()
	{
		var book = new OrderBook(CreateSecId());

		var removed = book.RemoveQuote(999, Sides.Buy, 100);

		IsFalse(removed);
	}

	[TestMethod]
	public void UpdateLevel_AddsNewLevel()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Buy, 100, 20);

		AreEqual(20m, book.TotalBidVolume);
		AreEqual(1, book.BidLevels);
		AreEqual(100m, book.BestBid.Value.price);
	}

	[TestMethod]
	public void UpdateLevel_UpdatesExistingLevel()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Buy, 100, 20);
		book.UpdateLevel(Sides.Buy, 100, 30);

		AreEqual(30m, book.TotalBidVolume);
		AreEqual(1, book.BidLevels);
	}

	[TestMethod]
	public void UpdateLevel_ZeroVolume_RemovesLevel()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Buy, 100, 20);
		book.UpdateLevel(Sides.Buy, 100, 0);

		AreEqual(0m, book.TotalBidVolume);
		AreEqual(0, book.BidLevels);
	}

	[TestMethod]
	public void Clear_RemovesAllQuotes()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Sell, Price = 101, Balance = 15, Volume = 15 });

		book.Clear();

		AreEqual(0m, book.TotalBidVolume);
		AreEqual(0m, book.TotalAskVolume);
		AreEqual(0, book.BidLevels);
		AreEqual(0, book.AskLevels);
	}

	[TestMethod]
	public void Clear_Side_OnlyRemovesSpecifiedSide()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Sell, Price = 101, Balance = 15, Volume = 15 });

		book.Clear(Sides.Buy);

		AreEqual(0m, book.TotalBidVolume);
		AreEqual(15m, book.TotalAskVolume);
		AreEqual(0, book.BidLevels);
		AreEqual(1, book.AskLevels);
	}

	[TestMethod]
	public void SetSnapshot_ReplacesMarketQuotes()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Buy, 100, 10);
		book.UpdateLevel(Sides.Sell, 101, 10);

		book.SetSnapshot(
			[new QuoteChange(99, 20), new QuoteChange(98, 30)],
			[new QuoteChange(102, 25), new QuoteChange(103, 35)]
		);

		AreEqual(50m, book.TotalBidVolume);
		AreEqual(60m, book.TotalAskVolume);
		AreEqual(2, book.BidLevels);
		AreEqual(2, book.AskLevels);
		AreEqual(99m, book.BestBid.Value.price);
		AreEqual(102m, book.BestAsk.Value.price);
	}

	[TestMethod]
	public void SetSnapshot_PreservesUserOrders()
	{
		var book = new OrderBook(CreateSecId());

		// Add user order
		book.AddQuote(new EmulatorOrder
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Balance = 10,
			Volume = 10,
			PortfolioName = "TestPortfolio"
		});

		book.SetSnapshot(
			[new QuoteChange(99, 20)],
			[new QuoteChange(101, 25)]
		);

		// User order should be preserved
		AreEqual(30m, book.TotalBidVolume); // 20 market + 10 user
		AreEqual(2, book.BidLevels);

		var orders = book.GetOrdersAtPrice(Sides.Buy, 100).ToList();
		AreEqual(1, orders.Count);
		AreEqual("TestPortfolio", orders[0].PortfolioName);
	}

	[TestMethod]
	public void GetLevels_ReturnsSortedLevels()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 98, Balance = 5, Volume = 5 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 3, Side = Sides.Buy, Price = 99, Balance = 7, Volume = 7 });

		var levels = book.GetLevels(Sides.Buy).ToList();

		AreEqual(3, levels.Count);
		// Bids should be sorted descending (best first)
		AreEqual(100m, levels[0].Price);
		AreEqual(99m, levels[1].Price);
		AreEqual(98m, levels[2].Price);
	}

	[TestMethod]
	public void GetLevels_Asks_ReturnsSortedAscending()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Sell, Price = 103, Balance = 5, Volume = 5 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Sell, Price = 101, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 3, Side = Sides.Sell, Price = 102, Balance = 7, Volume = 7 });

		var levels = book.GetLevels(Sides.Sell).ToList();

		AreEqual(3, levels.Count);
		// Asks should be sorted ascending (best first)
		AreEqual(101m, levels[0].Price);
		AreEqual(102m, levels[1].Price);
		AreEqual(103m, levels[2].Price);
	}

	[TestMethod]
	public void ToMessage_CreatesCorrectQuoteChangeMessage()
	{
		var secId = CreateSecId();
		var book = new OrderBook(secId);
		var now = DateTime.UtcNow;

		book.UpdateLevel(Sides.Buy, 100, 10);
		book.UpdateLevel(Sides.Buy, 99, 20);
		book.UpdateLevel(Sides.Sell, 101, 15);
		book.UpdateLevel(Sides.Sell, 102, 25);

		var msg = book.ToMessage(now, now);

		AreEqual(secId, msg.SecurityId);
		AreEqual(2, msg.Bids.Length);
		AreEqual(2, msg.Asks.Length);
		AreEqual(100m, msg.Bids[0].Price);
		AreEqual(10m, msg.Bids[0].Volume);
		AreEqual(101m, msg.Asks[0].Price);
		AreEqual(15m, msg.Asks[0].Volume);
	}

	[TestMethod]
	public void TrimToDepth_RemovesWorstLevels()
	{
		var book = new OrderBook(CreateSecId());

		for (int i = 0; i < 10; i++)
		{
			book.AddQuote(new EmulatorOrder
			{
				TransactionId = i + 1,
				Side = Sides.Buy,
				Price = 100 - i,
				Balance = 10,
				Volume = 10
			});
		}

		AreEqual(10, book.BidLevels);

		var removed = book.TrimToDepth(Sides.Buy, 5).ToList();

		AreEqual(5, book.BidLevels);
		AreEqual(5, removed.Count);
		AreEqual(100m, book.BestBid.Value.price);
	}

	[TestMethod]
	public void ConsumeVolume_ConsumesFromBestPrice()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Sell, 101, 10);
		book.UpdateLevel(Sides.Sell, 102, 20);
		book.UpdateLevel(Sides.Sell, 103, 30);

		var executions = book.ConsumeVolume(Sides.Sell, 105m, 25m).ToList();

		AreEqual(2, executions.Count);
		AreEqual(101m, executions[0].price);
		AreEqual(10m, executions[0].volume);
		AreEqual(102m, executions[1].price);
		AreEqual(15m, executions[1].volume);
		AreEqual(35m, book.TotalAskVolume); // 60 - 25 = 35
	}

	[TestMethod]
	public void ConsumeVolume_RespectsLimitPrice()
	{
		var book = new OrderBook(CreateSecId());

		book.UpdateLevel(Sides.Sell, 101, 10);
		book.UpdateLevel(Sides.Sell, 102, 20);
		book.UpdateLevel(Sides.Sell, 103, 30);

		// Limit price is 101, so only first level should be consumed
		var executions = book.ConsumeVolume(Sides.Sell, 101m, 50m).ToList();

		AreEqual(1, executions.Count);
		AreEqual(101m, executions[0].price);
		AreEqual(10m, executions[0].volume);
	}

	[TestMethod]
	public void HasLevel_ExistingLevel_ReturnsTrue()
	{
		var book = new OrderBook(CreateSecId());
		book.UpdateLevel(Sides.Buy, 100, 10);

		IsTrue(book.HasLevel(Sides.Buy, 100));
	}

	[TestMethod]
	public void HasLevel_NonExistingLevel_ReturnsFalse()
	{
		var book = new OrderBook(CreateSecId());

		IsFalse(book.HasLevel(Sides.Buy, 100));
	}

	[TestMethod]
	public void GetVolumeAtPrice_ReturnsCorrectVolume()
	{
		var book = new OrderBook(CreateSecId());
		book.UpdateLevel(Sides.Buy, 100, 25);

		AreEqual(25m, book.GetVolumeAtPrice(Sides.Buy, 100));
	}

	[TestMethod]
	public void GetVolumeAtPrice_NonExistingLevel_ReturnsZero()
	{
		var book = new OrderBook(CreateSecId());

		AreEqual(0m, book.GetVolumeAtPrice(Sides.Buy, 100));
	}

	[TestMethod]
	public void MultipleOrdersAtSameLevel_AggregatesVolume()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Buy, Price = 100, Balance = 20, Volume = 20 });
		book.AddQuote(new EmulatorOrder { TransactionId = 3, Side = Sides.Buy, Price = 100, Balance = 15, Volume = 15 });

		AreEqual(45m, book.TotalBidVolume);
		AreEqual(1, book.BidLevels);
		AreEqual(45m, book.BestBid.Value.volume);
	}

	[TestMethod]
	public void TryRemoveOrder_FindsAndRemovesOrder()
	{
		var book = new OrderBook(CreateSecId());

		book.AddQuote(new EmulatorOrder { TransactionId = 1, Side = Sides.Buy, Price = 100, Balance = 10, Volume = 10 });
		book.AddQuote(new EmulatorOrder { TransactionId = 2, Side = Sides.Buy, Price = 99, Balance = 20, Volume = 20 });

		var found = book.TryRemoveOrder(2, Sides.Buy, out var order);

		IsTrue(found);
		IsNotNull(order);
		AreEqual(2, order.TransactionId);
		AreEqual(10m, book.TotalBidVolume);
	}
}
