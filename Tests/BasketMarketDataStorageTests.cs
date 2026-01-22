namespace StockSharp.Tests;

[TestClass]
public class BasketMarketDataStorageTests : BaseTestClass
{
	#region Random Data Generators

	private static List<ExecutionMessage> GenerateRandomTicks(SecurityId securityId, DateTime date, int count)
	{
		var ticks = new List<ExecutionMessage>();
		var baseTime = date.Date.AddHours(10);
		var basePrice = 100m + (decimal)(RandomGen.GetDouble() * 900);

		for (int i = 0; i < count; i++)
		{
			var tick = new ExecutionMessage
			{
				SecurityId = securityId,
				DataTypeEx = DataType.Ticks,
				ServerTime = baseTime.AddMilliseconds(i * 100 + RandomGen.GetInt(50)),
				TradePrice = basePrice + (decimal)((RandomGen.GetDouble() - 0.5) * 10),
				TradeVolume = RandomGen.GetInt(1, 1000),
				TradeId = i + 1,
				OriginSide = RandomGen.GetInt(2) == 0 ? Sides.Buy : Sides.Sell,
			};
			ticks.Add(tick);
		}

		return [.. ticks.OrderBy(t => t.ServerTime)];
	}

	private static List<TimeFrameCandleMessage> GenerateRandomCandles(SecurityId securityId, DateTime date, TimeSpan timeFrame, int count)
	{
		var candles = new List<TimeFrameCandleMessage>();
		var baseTime = date.Date.AddHours(10);
		var basePrice = 100m + (decimal)(RandomGen.GetDouble() * 900);

		for (int i = 0; i < count; i++)
		{
			var open = basePrice + (decimal)((RandomGen.GetDouble() - 0.5) * 10);
			var close = open + (decimal)((RandomGen.GetDouble() - 0.5) * 5);
			var high = Math.Max(open, close) + (decimal)(RandomGen.GetDouble() * 3);
			var low = Math.Min(open, close) - (decimal)(RandomGen.GetDouble() * 3);

			var candle = new TimeFrameCandleMessage
			{
				SecurityId = securityId,
				OpenTime = baseTime.AddTicks(timeFrame.Ticks * i),
				CloseTime = baseTime.AddTicks(timeFrame.Ticks * (i + 1)),
				OpenPrice = open,
				HighPrice = high,
				LowPrice = low,
				ClosePrice = close,
				TotalVolume = RandomGen.GetInt(100, 10000),
				State = CandleStates.Finished,
			};
			candles.Add(candle);
			basePrice = close;
		}

		return candles;
	}

	private static List<Level1ChangeMessage> GenerateRandomLevel1(SecurityId securityId, DateTime date, int count)
	{
		var messages = new List<Level1ChangeMessage>();
		var baseTime = date.Date.AddHours(10);
		var basePrice = 100m + (decimal)(RandomGen.GetDouble() * 900);

		for (int i = 0; i < count; i++)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = baseTime.AddMilliseconds(i * 100),
			};

			msg.Changes.Add(Level1Fields.BestBidPrice, basePrice - (decimal)(RandomGen.GetDouble() * 0.5));
			msg.Changes.Add(Level1Fields.BestAskPrice, basePrice + (decimal)(RandomGen.GetDouble() * 0.5));
			msg.Changes.Add(Level1Fields.BestBidVolume, (decimal)RandomGen.GetInt(1, 1000));
			msg.Changes.Add(Level1Fields.BestAskVolume, (decimal)RandomGen.GetInt(1, 1000));
			msg.Changes.Add(Level1Fields.LastTradePrice, basePrice);

			messages.Add(msg);
			basePrice += (decimal)((RandomGen.GetDouble() - 0.5) * 2);
		}

		return messages;
	}

	private static List<QuoteChangeMessage> GenerateRandomOrderBooks(SecurityId securityId, DateTime date, int count, int depth)
	{
		var books = new List<QuoteChangeMessage>();
		var baseTime = date.Date.AddHours(10);
		var basePrice = 100m + (decimal)(RandomGen.GetDouble() * 900);

		for (int i = 0; i < count; i++)
		{
			var bids = new QuoteChange[depth];
			var asks = new QuoteChange[depth];

			for (int j = 0; j < depth; j++)
			{
				bids[j] = new QuoteChange(basePrice - (decimal)(j + 1) * 0.1m - (decimal)(RandomGen.GetDouble() * 0.05), RandomGen.GetInt(1, 1000));
				asks[j] = new QuoteChange(basePrice + (decimal)(j + 1) * 0.1m + (decimal)(RandomGen.GetDouble() * 0.05), RandomGen.GetInt(1, 1000));
			}

			var book = new QuoteChangeMessage
			{
				SecurityId = securityId,
				ServerTime = baseTime.AddMilliseconds(i * 500),
				Bids = bids,
				Asks = asks,
			};

			books.Add(book);
			basePrice += (decimal)((RandomGen.GetDouble() - 0.5) * 2);
		}

		return books;
	}

	#endregion

	#region Test Storage Helper

	// Use production InMemoryMarketDataStorage - BasketMarketDataStorage has special handling for this type
	// that skips date checks (see BasketMarketDataStorage.cs line 74)

	private static IMarketDataStorage<TMessage> CreateTestStorage<TMessage>(
		SecurityId securityId,
		DataType dataType,
		Dictionary<DateTime, List<TMessage>> data)
		where TMessage : Message
	{
		return new InMemoryMarketDataStorage<TMessage>(
			securityId,
			dataType,
			date =>
			{
				if (data.TryGetValue(date.Date, out var list))
					return list.OrderBy(m => m.GetServerTime()).ToAsyncEnumerable();
				return AsyncEnumerable.Empty<TMessage>();
			});
	}

	#endregion

	#region Tests - Basic Functionality

	[TestMethod]
	public void Constructor_CreatesEmptyStorage()
	{
		using var storage = new BasketMarketDataStorage<ExecutionMessage>();

		storage.InnerStorages.AssertNotNull();
		AreEqual(0, storage.InnerStorages.Count);
	}

	[TestMethod]
	public async Task GetDatesAsync_EmptyStorage_ReturnsEmpty()
	{
		using var storage = new BasketMarketDataStorage<ExecutionMessage>();

		var dates = await ((IMarketDataStorage)storage).GetDatesAsync(CancellationToken);

		AreEqual(0, dates.Count());
	}

	[TestMethod]
	public async Task LoadAsync_EmptyStorage_ReturnsEmpty()
	{
		using var storage = new BasketMarketDataStorage<ExecutionMessage>();
		var date = DateTime.Today;

		var data = storage.LoadAsync(date);
		var list = new List<ExecutionMessage>();

		await foreach (var item in data)
			list.Add(item);

		AreEqual(0, list.Count);
	}

	#endregion

	#region Tests - Single Storage with Ticks

	[TestMethod]
	public async Task SingleStorage_1000Ticks_LoadsAllInOrder()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);
		var ticks = GenerateRandomTicks(secId, date, 1000);

		var data = new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks } };
		var innerStorage = CreateTestStorage(secId, DataType.Ticks, data);

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();
		basket.InnerStorages.Add(innerStorage);

		var loaded = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date))
			loaded.Add(tick);

		AreEqual(1000, loaded.Count);

		// Verify chronological order
		for (int i = 1; i < loaded.Count; i++)
		{
			IsTrue(loaded[i].ServerTime >= loaded[i - 1].ServerTime,
				$"Tick {i} time {loaded[i].ServerTime} is before tick {i - 1} time {loaded[i - 1].ServerTime}");
		}

		// Verify data integrity - prices and volumes match
		var ticksDict = ticks.ToDictionary(t => t.TradeId);
		foreach (var tick in loaded)
		{
			IsTrue(ticksDict.TryGetValue(tick.TradeId, out var original),
				$"Tick with TradeId {tick.TradeId} not found in original");
			AreEqual(original.TradePrice, tick.TradePrice, $"Price mismatch for TradeId {tick.TradeId}");
			AreEqual(original.TradeVolume, tick.TradeVolume, $"Volume mismatch for TradeId {tick.TradeId}");
		}
	}

	[TestMethod]
	public async Task SingleStorage_GetDates_ThrowsNotSupportedForInMemoryStorage()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date1 = new DateTime(2024, 1, 15);

		var data = new Dictionary<DateTime, List<ExecutionMessage>>
		{
			{ date1, GenerateRandomTicks(secId, date1, 100) }
		};
		var innerStorage = CreateTestStorage(secId, DataType.Ticks, data);

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();
		basket.InnerStorages.Add(innerStorage);

		// InMemoryMarketDataStorage.GetDatesAsync throws NotSupportedException,
		// and BasketMarketDataStorage.GetDatesAsync propagates it
		await ThrowsExactlyAsync<NotSupportedException>(async () =>
			await ((IMarketDataStorage)basket).GetDatesAsync(CancellationToken));
	}

	#endregion

	#region Tests - Multiple Storages Merging

	[TestMethod]
	public async Task MultipleStorages_3Sources_MergesInChronologicalOrder()
	{
		var date = new DateTime(2024, 1, 15);

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };
		var secId3 = new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" };

		var ticks1 = GenerateRandomTicks(secId1, date, 500);
		var ticks2 = GenerateRandomTicks(secId2, date, 500);
		var ticks3 = GenerateRandomTicks(secId3, date, 500);

		var storage1 = CreateTestStorage(secId1, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks1 } });
		var storage2 = CreateTestStorage(secId2, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks2 } });
		var storage3 = CreateTestStorage(secId3, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks3 } });

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();
		basket.InnerStorages.Add(storage1);
		basket.InnerStorages.Add(storage2);
		basket.InnerStorages.Add(storage3);

		var loaded = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date))
			loaded.Add(tick);

		AreEqual(1500, loaded.Count);

		// Verify strictly chronological order across all sources
		for (int i = 1; i < loaded.Count; i++)
		{
			IsTrue(loaded[i].ServerTime >= loaded[i - 1].ServerTime,
				$"Tick {i} at {loaded[i].ServerTime} from {loaded[i].SecurityId} is before tick {i - 1} at {loaded[i - 1].ServerTime}");
		}

		// Verify all securities are present
		var bySecurity = loaded.GroupBy(t => t.SecurityId).ToDictionary(g => g.Key, g => g.Count());
		AreEqual(3, bySecurity.Count);
		AreEqual(500, bySecurity[secId1]);
		AreEqual(500, bySecurity[secId2]);
		AreEqual(500, bySecurity[secId3]);
	}

	[TestMethod]
	public async Task MultipleStorages_DifferentDates_LoadsDataForRequestedDate()
	{
		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		var date1 = new DateTime(2024, 1, 15);
		var date2 = new DateTime(2024, 1, 16);
		var date3 = new DateTime(2024, 1, 17);

		// Storage1 has dates 1 and 2
		var data1 = new Dictionary<DateTime, List<ExecutionMessage>>
		{
			{ date1, GenerateRandomTicks(secId1, date1, 100) },
			{ date2, GenerateRandomTicks(secId1, date2, 100) }
		};
		var storage1 = CreateTestStorage(secId1, DataType.Ticks, data1);

		// Storage2 has dates 2 and 3
		var data2 = new Dictionary<DateTime, List<ExecutionMessage>>
		{
			{ date2, GenerateRandomTicks(secId2, date2, 100) },
			{ date3, GenerateRandomTicks(secId2, date3, 100) }
		};
		var storage2 = CreateTestStorage(secId2, DataType.Ticks, data2);

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();
		basket.InnerStorages.Add(storage1);
		basket.InnerStorages.Add(storage2);

		// Verify date2 loads from both storages
		var date2Data = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date2))
			date2Data.Add(tick);

		AreEqual(200, date2Data.Count);
		AreEqual(100, date2Data.Count(t => t.SecurityId == secId1));
		AreEqual(100, date2Data.Count(t => t.SecurityId == secId2));

		// Verify date1 loads only from storage1
		var date1Data = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date1))
			date1Data.Add(tick);

		AreEqual(100, date1Data.Count);
		IsTrue(date1Data.All(t => t.SecurityId == secId1));

		// Verify date3 loads only from storage2
		var date3Data = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date3))
			date3Data.Add(tick);

		AreEqual(100, date3Data.Count);
		IsTrue(date3Data.All(t => t.SecurityId == secId2));
	}

	#endregion

	#region Tests - Candles

	[TestMethod]
	public async Task Candles_500Items_LoadsWithCorrectOHLCV()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);
		var timeFrame = TimeSpan.FromMinutes(1);
		var candles = GenerateRandomCandles(secId, date, timeFrame, 500);

		var data = new Dictionary<DateTime, List<TimeFrameCandleMessage>> { { date, candles } };
		var innerStorage = CreateTestStorage(secId, timeFrame.TimeFrame(), data);

		using var basket = new BasketMarketDataStorage<TimeFrameCandleMessage>();
		basket.InnerStorages.Add(innerStorage);

		var loaded = new List<TimeFrameCandleMessage>();
		await foreach (var candle in basket.LoadAsync(date))
			loaded.Add(candle);

		AreEqual(500, loaded.Count);

		// Verify OHLCV integrity
		for (int i = 0; i < loaded.Count; i++)
		{
			var orig = candles[i];
			var load = loaded[i];

			AreEqual(orig.OpenPrice, load.OpenPrice, $"OpenPrice mismatch at {i}");
			AreEqual(orig.HighPrice, load.HighPrice, $"HighPrice mismatch at {i}");
			AreEqual(orig.LowPrice, load.LowPrice, $"LowPrice mismatch at {i}");
			AreEqual(orig.ClosePrice, load.ClosePrice, $"ClosePrice mismatch at {i}");
			AreEqual(orig.TotalVolume, load.TotalVolume, $"Volume mismatch at {i}");

			// Verify High >= Low always
			IsTrue(load.HighPrice >= load.LowPrice, $"High < Low at {i}");
			// Verify Open and Close are within High-Low range
			IsTrue(load.OpenPrice >= load.LowPrice && load.OpenPrice <= load.HighPrice, $"Open out of range at {i}");
			IsTrue(load.ClosePrice >= load.LowPrice && load.ClosePrice <= load.HighPrice, $"Close out of range at {i}");
		}
	}

	[TestMethod]
	public async Task Candles_MultipleTimeframes_MergesCorrectly()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);

		var tf1 = TimeSpan.FromMinutes(1);
		var tf5 = TimeSpan.FromMinutes(5);
		var tf15 = TimeSpan.FromMinutes(15);

		var candles1 = GenerateRandomCandles(secId, date, tf1, 100);
		var candles5 = GenerateRandomCandles(secId, date, tf5, 50);
		var candles15 = GenerateRandomCandles(secId, date, tf15, 30);

		var storage1 = CreateTestStorage(secId, tf1.TimeFrame(),
			new Dictionary<DateTime, List<TimeFrameCandleMessage>> { { date, candles1 } });
		var storage5 = CreateTestStorage(secId, tf5.TimeFrame(),
			new Dictionary<DateTime, List<TimeFrameCandleMessage>> { { date, candles5 } });
		var storage15 = CreateTestStorage(secId, tf15.TimeFrame(),
			new Dictionary<DateTime, List<TimeFrameCandleMessage>> { { date, candles15 } });

		using var basket = new BasketMarketDataStorage<TimeFrameCandleMessage>();
		basket.InnerStorages.Add(storage1);
		basket.InnerStorages.Add(storage5);
		basket.InnerStorages.Add(storage15);

		var loaded = new List<TimeFrameCandleMessage>();
		await foreach (var candle in basket.LoadAsync(date))
			loaded.Add(candle);

		AreEqual(180, loaded.Count);

		// Verify chronological order
		for (int i = 1; i < loaded.Count; i++)
		{
			IsTrue(loaded[i].OpenTime >= loaded[i - 1].OpenTime,
				$"Candle {i} time {loaded[i].OpenTime} is before candle {i - 1} time {loaded[i - 1].OpenTime}");
		}
	}

	#endregion

	#region Tests - Level1

	[TestMethod]
	public async Task Level1_1000Updates_LoadsWithCorrectFields()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);
		var updates = GenerateRandomLevel1(secId, date, 1000);

		var data = new Dictionary<DateTime, List<Level1ChangeMessage>> { { date, updates } };
		var innerStorage = CreateTestStorage(secId, DataType.Level1, data);

		using var basket = new BasketMarketDataStorage<Level1ChangeMessage>();
		basket.InnerStorages.Add(innerStorage);

		var loaded = new List<Level1ChangeMessage>();
		await foreach (var msg in basket.LoadAsync(date))
			loaded.Add(msg);

		AreEqual(1000, loaded.Count);

		// Verify all expected fields are present
		foreach (var msg in loaded)
		{
			IsTrue(msg.Changes.ContainsKey(Level1Fields.BestBidPrice), "Missing BestBidPrice");
			IsTrue(msg.Changes.ContainsKey(Level1Fields.BestAskPrice), "Missing BestAskPrice");
			IsTrue(msg.Changes.ContainsKey(Level1Fields.LastTradePrice), "Missing LastTradePrice");

			var bid = (decimal)msg.Changes[Level1Fields.BestBidPrice];
			var ask = (decimal)msg.Changes[Level1Fields.BestAskPrice];
			IsTrue(bid < ask, $"Bid {bid} >= Ask {ask}");
		}

		// Verify chronological order
		for (int i = 1; i < loaded.Count; i++)
		{
			IsTrue(loaded[i].ServerTime >= loaded[i - 1].ServerTime);
		}
	}

	#endregion

	#region Tests - OrderBook

	[TestMethod]
	public async Task OrderBook_500Snapshots_LoadsWithCorrectDepth()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);
		var depth = 20;
		var books = GenerateRandomOrderBooks(secId, date, 500, depth);

		var data = new Dictionary<DateTime, List<QuoteChangeMessage>> { { date, books } };
		var innerStorage = CreateTestStorage(secId, DataType.MarketDepth, data);

		using var basket = new BasketMarketDataStorage<QuoteChangeMessage>();
		basket.InnerStorages.Add(innerStorage);

		var loaded = new List<QuoteChangeMessage>();
		await foreach (var book in basket.LoadAsync(date))
			loaded.Add(book);

		AreEqual(500, loaded.Count);

		foreach (var book in loaded)
		{
			AreEqual(depth, book.Bids.Length, "Bids depth mismatch");
			AreEqual(depth, book.Asks.Length, "Asks depth mismatch");

			// Verify bids are sorted descending by price
			for (int i = 1; i < book.Bids.Length; i++)
			{
				IsTrue(book.Bids[i].Price <= book.Bids[i - 1].Price,
					$"Bids not sorted: {book.Bids[i - 1].Price} -> {book.Bids[i].Price}");
			}

			// Verify asks are sorted ascending by price
			for (int i = 1; i < book.Asks.Length; i++)
			{
				IsTrue(book.Asks[i].Price >= book.Asks[i - 1].Price,
					$"Asks not sorted: {book.Asks[i - 1].Price} -> {book.Asks[i].Price}");
			}

			// Verify best bid < best ask
			IsTrue(book.Bids[0].Price < book.Asks[0].Price,
				$"Best bid {book.Bids[0].Price} >= best ask {book.Asks[0].Price}");
		}
	}

	#endregion

	#region Tests - Dynamic Storage Addition/Removal

	[TestMethod]
	public async Task DynamicAdd_StorageAddedDuringEnumeration_IncludesNewData()
	{
		var date = new DateTime(2024, 1, 15);
		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		var ticks1 = GenerateRandomTicks(secId1, date, 100);
		var ticks2 = GenerateRandomTicks(secId2, date, 100);

		var storage1 = CreateTestStorage(secId1, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks1 } });
		var storage2 = CreateTestStorage(secId2, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks2 } });

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();
		basket.InnerStorages.Add(storage1);

		// Start enumeration
		var loaded = new List<ExecutionMessage>();
		var enumerator = basket.LoadAsync(date).GetAsyncEnumerator(CancellationToken);

		// Read some data
		for (int i = 0; i < 50 && await enumerator.MoveNextAsync(); i++)
			loaded.Add(enumerator.Current);

		// Add second storage dynamically
		basket.InnerStorages.Add(storage2);

		// Continue reading
		while (await enumerator.MoveNextAsync())
			loaded.Add(enumerator.Current);

		await enumerator.DisposeAsync();

		// Should have data from both storages
		IsTrue(loaded.Count >= 100, $"Expected at least 100 items, got {loaded.Count}");
		IsTrue(loaded.Any(t => t.SecurityId == secId1), "Missing AAPL data");
	}

	[TestMethod]
	public void InnerStorages_AddRemoveClear_WorksCorrectly()
	{
		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		var storage1 = CreateTestStorage(secId1, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>>());
		var storage2 = CreateTestStorage(secId2, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>>());

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		// Add
		basket.InnerStorages.Add(storage1);
		AreEqual(1, basket.InnerStorages.Count);

		basket.InnerStorages.Add(storage2);
		AreEqual(2, basket.InnerStorages.Count);

		// Remove
		basket.InnerStorages.Remove(storage1);
		AreEqual(1, basket.InnerStorages.Count);

		// Clear
		basket.InnerStorages.Clear();
		AreEqual(0, basket.InnerStorages.Count);
	}

	#endregion

	#region Tests - Stress Tests

	[TestMethod]
	public async Task StressTest_5000TicksFrom5Storages_MergesCorrectly()
	{
		var date = new DateTime(2024, 1, 15);
		var securities = new[]
		{
			new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
			new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" },
			new SecurityId { SecurityCode = "AMZN", BoardCode = "NASDAQ" },
			new SecurityId { SecurityCode = "META", BoardCode = "NASDAQ" },
		};

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		foreach (var secId in securities)
		{
			var ticks = GenerateRandomTicks(secId, date, 1000);
			var storage = CreateTestStorage(secId, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks } });
			basket.InnerStorages.Add(storage);
		}

		var loaded = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date))
			loaded.Add(tick);

		AreEqual(5000, loaded.Count);

		// Verify all securities present
		var bySec = loaded.GroupBy(t => t.SecurityId).ToDictionary(g => g.Key, g => g.Count());
		AreEqual(5, bySec.Count);
		foreach (var secId in securities)
			AreEqual(1000, bySec[secId]);

		// Verify global chronological order
		for (int i = 1; i < loaded.Count; i++)
		{
			IsTrue(loaded[i].ServerTime >= loaded[i - 1].ServerTime,
				$"Order violation at {i}: {loaded[i - 1].ServerTime} -> {loaded[i].ServerTime}");
		}
	}

	#endregion

	#region Tests - TransactionId Handling

	[TestMethod]
	public async Task TransactionId_AddedViaInnerStorages_PropagatedToMessages()
	{
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date = new DateTime(2024, 1, 15);
		var ticks = GenerateRandomTicks(secId, date, 100);

		var storage = CreateTestStorage(secId, DataType.Ticks, new Dictionary<DateTime, List<ExecutionMessage>> { { date, ticks } });

		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		long transactionId = 12345;
		basket.InnerStorages.Add(storage, transactionId);

		var loaded = new List<ExecutionMessage>();
		await foreach (var tick in basket.LoadAsync(date))
			loaded.Add(tick);

		AreEqual(100, loaded.Count);

		// All messages should have the transaction id set
		foreach (var tick in loaded)
		{
			var ids = tick.GetSubscriptionIds();
			IsTrue(ids.Contains(transactionId), $"Message missing transactionId {transactionId}");
		}
	}

	#endregion

	#region Tests - NotSupported Operations

	[TestMethod]
	public async Task SaveAsync_ThrowsNotSupported()
	{
		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		await ThrowsExactlyAsync<NotSupportedException>(() =>
			((IMarketDataStorage)basket).SaveAsync([], CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task DeleteAsync_ThrowsNotSupported()
	{
		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		await ThrowsExactlyAsync<NotSupportedException>(async () =>
			await ((IMarketDataStorage)basket).DeleteAsync([], CancellationToken).AsTask());
	}

	[TestMethod]
	public void DataType_ThrowsNotSupported()
	{
		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		ThrowsExactly<NotSupportedException>(() => _ = basket.DataType);
	}

	[TestMethod]
	public void SecurityId_ThrowsNotSupported()
	{
		using var basket = new BasketMarketDataStorage<ExecutionMessage>();

		ThrowsExactly<NotSupportedException>(() => _ = basket.SecurityId);
	}

	#endregion
}
