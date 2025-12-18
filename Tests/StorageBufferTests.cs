namespace StockSharp.Tests;

/// <summary>
/// Tests for <see cref="StorageBuffer"/>.
/// </summary>
[TestClass]
public class StorageBufferTests : BaseTestClass
{
	#region Helper Methods

	private static SecurityId CreateSecurityId(string code = "SBER")
	{
		return new SecurityId { SecurityCode = code, BoardCode = "TQBR" };
	}

	private static ExecutionMessage CreateTick(SecurityId securityId, DateTime serverTime, decimal price, decimal volume)
	{
		return new ExecutionMessage
		{
			SecurityId = securityId,
			DataTypeEx = DataType.Ticks,
			ServerTime = serverTime,
			TradePrice = price,
			TradeVolume = volume,
		};
	}

	private static ExecutionMessage CreateOrderLog(SecurityId securityId, DateTime serverTime, decimal price, decimal volume, Sides side)
	{
		return new ExecutionMessage
		{
			SecurityId = securityId,
			DataTypeEx = DataType.OrderLog,
			ServerTime = serverTime,
			OrderPrice = price,
			OrderVolume = volume,
			Side = side,
		};
	}

	private static ExecutionMessage CreateTransaction(SecurityId securityId, DateTime serverTime, long transactionId)
	{
		return new ExecutionMessage
		{
			SecurityId = securityId,
			DataTypeEx = DataType.Transactions,
			ServerTime = serverTime,
			TransactionId = transactionId,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};
	}

	private static Level1ChangeMessage CreateLevel1(SecurityId securityId, DateTime serverTime, decimal lastPrice)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
		};
		msg.Add(Level1Fields.LastTradePrice, lastPrice);
		return msg;
	}

	private static QuoteChangeMessage CreateQuotes(SecurityId securityId, DateTime serverTime)
	{
		return new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
			Bids = [new QuoteChange(100, 10)],
			Asks = [new QuoteChange(101, 5)],
		};
	}

	private static TimeFrameCandleMessage CreateCandle(SecurityId securityId, DateTime openTime, TimeSpan timeFrame)
	{
		return new TimeFrameCandleMessage
		{
			SecurityId = securityId,
			OpenTime = openTime,
			CloseTime = openTime + timeFrame,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 102,
			TotalVolume = 1000,
			State = CandleStates.Finished,
			DataType = timeFrame.TimeFrame(),
		};
	}

	private static NewsMessage CreateNews(string headline, DateTime serverTime)
	{
		return new NewsMessage
		{
			Headline = headline,
			ServerTime = serverTime,
		};
	}

	private static BoardStateMessage CreateBoardState(string boardCode, SessionStates state, DateTime serverTime)
	{
		return new BoardStateMessage
		{
			BoardCode = boardCode,
			State = state,
			ServerTime = serverTime,
		};
	}

	private static PositionChangeMessage CreatePositionChange(SecurityId securityId, DateTime serverTime, decimal currentValue)
	{
		var msg = new PositionChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
			PortfolioName = "TestPortfolio",
		};
		msg.Add(PositionChangeTypes.CurrentValue, currentValue);
		return msg;
	}

	private static MarketDataMessage CreateSubscription(long transactionId, SecurityId securityId, DataType dataType)
	{
		return new MarketDataMessage
		{
			TransactionId = transactionId,
			SecurityId = securityId,
			DataType2 = dataType,
			IsSubscribe = true,
		};
	}

	private static void SetSubscriptionId(Message message, long subscriptionId)
	{
		if (message is ISubscriptionIdMessage subscrMsg)
			subscrMsg.SetSubscriptionIds([subscriptionId]);
	}

	#endregion

	#region Constructor and Default Values Tests

	[TestMethod]
	public void Constructor_DefaultValues_AreCorrect()
	{
		var buffer = new StorageBuffer();

		buffer.Enabled.AssertTrue();
		buffer.EnabledLevel1.AssertTrue();
		buffer.EnabledOrderBook.AssertTrue();
		buffer.EnabledTransactions.AssertTrue();
		buffer.EnabledPositions.AssertFalse();
		buffer.FilterSubscription.AssertFalse();
		buffer.DisableStorageTimer.AssertFalse();
		buffer.IgnoreGenerated.AssertNotNull();
	}

	#endregion

	#region ProcessOutMessage - Ticks Tests

	[TestMethod]
	public void ProcessOutMessage_Tick_BufferedCorrectly()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);

		buffer.ProcessOutMessage(tick);

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(1);
		ticks.ContainsKey(secId).AssertTrue();
		ticks[secId].Count().AssertEqual(1);
	}

	[TestMethod]
	public void ProcessOutMessage_MultipleTicks_SameSecurityBuffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		for (var i = 0; i < 5; i++)
		{
			var tick = CreateTick(secId, DateTime.UtcNow.AddSeconds(i), 100 + i, 10);
			buffer.ProcessOutMessage(tick);
		}

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(1);
		ticks[secId].Count().AssertEqual(5);
	}

	[TestMethod]
	public void ProcessOutMessage_MultipleTicks_DifferentSecurities()
	{
		var buffer = new StorageBuffer();
		var secId1 = CreateSecurityId("SBER");
		var secId2 = CreateSecurityId("GAZP");

		buffer.ProcessOutMessage(CreateTick(secId1, DateTime.UtcNow, 100, 10));
		buffer.ProcessOutMessage(CreateTick(secId2, DateTime.UtcNow, 200, 20));

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(2);
		ticks.ContainsKey(secId1).AssertTrue();
		ticks.ContainsKey(secId2).AssertTrue();
	}

	[TestMethod]
	public void GetTicks_ClearsBuffer()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		buffer.ProcessOutMessage(CreateTick(secId, DateTime.UtcNow, 100, 10));

		var ticks1 = buffer.GetTicks();
		ticks1.Count.AssertEqual(1);

		var ticks2 = buffer.GetTicks();
		ticks2.Count.AssertEqual(0);
	}

	#endregion

	#region ProcessOutMessage - OrderLog Tests

	[TestMethod]
	public void ProcessOutMessage_OrderLog_BufferedCorrectly()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var orderLog = CreateOrderLog(secId, DateTime.UtcNow, 100, 10, Sides.Buy);

		buffer.ProcessOutMessage(orderLog);

		var logs = buffer.GetOrderLog();
		logs.Count.AssertEqual(1);
		logs.ContainsKey(secId).AssertTrue();
	}

	#endregion

	#region ProcessOutMessage - Level1 Tests

	[TestMethod]
	public void ProcessOutMessage_Level1_BufferedWhenEnabled()
	{
		var buffer = new StorageBuffer { EnabledLevel1 = true };
		var secId = CreateSecurityId();
		var level1 = CreateLevel1(secId, DateTime.UtcNow, 100);

		buffer.ProcessOutMessage(level1);

		var result = buffer.GetLevel1();
		result.Count.AssertEqual(1);
	}

	[TestMethod]
	public void ProcessOutMessage_Level1_NotBufferedWhenDisabled()
	{
		var buffer = new StorageBuffer { EnabledLevel1 = false };
		var secId = CreateSecurityId();
		var level1 = CreateLevel1(secId, DateTime.UtcNow, 100);

		buffer.ProcessOutMessage(level1);

		var result = buffer.GetLevel1();
		result.Count.AssertEqual(0);
	}

	#endregion

	#region ProcessOutMessage - OrderBook Tests

	[TestMethod]
	public void ProcessOutMessage_OrderBook_BufferedWhenEnabled()
	{
		var buffer = new StorageBuffer { EnabledOrderBook = true };
		var secId = CreateSecurityId();
		var quotes = CreateQuotes(secId, DateTime.UtcNow);

		buffer.ProcessOutMessage(quotes);

		var result = buffer.GetOrderBooks();
		result.Count.AssertEqual(1);
	}

	[TestMethod]
	public void ProcessOutMessage_OrderBook_NotBufferedWhenDisabled()
	{
		var buffer = new StorageBuffer { EnabledOrderBook = false };
		var secId = CreateSecurityId();
		var quotes = CreateQuotes(secId, DateTime.UtcNow);

		buffer.ProcessOutMessage(quotes);

		var result = buffer.GetOrderBooks();
		result.Count.AssertEqual(0);
	}

	#endregion

	#region ProcessOutMessage - Candles Tests

	[TestMethod]
	public void ProcessOutMessage_FinishedCandle_Buffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var candle = CreateCandle(secId, DateTime.UtcNow, TimeSpan.FromMinutes(1));

		buffer.ProcessOutMessage(candle);

		var result = buffer.GetCandles();
		result.Count.AssertEqual(1);
	}

	[TestMethod]
	public void ProcessOutMessage_ActiveCandle_NotBuffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var candle = CreateCandle(secId, DateTime.UtcNow, TimeSpan.FromMinutes(1));
		candle.State = CandleStates.Active;

		buffer.ProcessOutMessage(candle);

		var result = buffer.GetCandles();
		result.Count.AssertEqual(0);
	}

	#endregion

	#region ProcessOutMessage - News Tests

	[TestMethod]
	public void ProcessOutMessage_News_Buffered()
	{
		var buffer = new StorageBuffer();
		var news = CreateNews("Test Headline", DateTime.UtcNow);

		buffer.ProcessOutMessage(news);

		var result = buffer.GetNews().ToArray();
		result.Length.AssertEqual(1);
		result[0].Headline.AssertEqual("Test Headline");
	}

	#endregion

	#region ProcessOutMessage - BoardState Tests

	[TestMethod]
	public void ProcessOutMessage_BoardState_Buffered()
	{
		var buffer = new StorageBuffer();
		var state = CreateBoardState("TQBR", SessionStates.Active, DateTime.UtcNow);

		buffer.ProcessOutMessage(state);

		var result = buffer.GetBoardStates().ToArray();
		result.Length.AssertEqual(1);
		result[0].State.AssertEqual(SessionStates.Active);
	}

	#endregion

	#region ProcessOutMessage - Transactions Tests

	[TestMethod]
	public void ProcessOutMessage_Transaction_Buffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var transaction = CreateTransaction(secId, DateTime.UtcNow, 123);

		buffer.ProcessOutMessage(transaction);

		var result = buffer.GetTransactions();
		result.Count.AssertEqual(1);
	}

	#endregion

	#region ProcessOutMessage - PositionChange Tests

	[TestMethod]
	public void ProcessOutMessage_PositionChange_Buffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();
		var position = CreatePositionChange(secId, DateTime.UtcNow, 100);

		buffer.ProcessOutMessage(position);

		var result = buffer.GetPositionChanges();
		result.Count.AssertEqual(1);
	}

	#endregion

	#region ProcessInMessage - Reset Tests

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsAllBuffers()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		// Fill buffers
		buffer.ProcessOutMessage(CreateTick(secId, DateTime.UtcNow, 100, 10));
		buffer.ProcessOutMessage(CreateLevel1(secId, DateTime.UtcNow, 100));
		buffer.ProcessOutMessage(CreateQuotes(secId, DateTime.UtcNow));
		buffer.ProcessOutMessage(CreateNews("Test", DateTime.UtcNow));

		// Reset
		buffer.ProcessInMessage(new ResetMessage());

		// Verify all cleared
		buffer.GetTicks().Count.AssertEqual(0);
		buffer.GetLevel1().Count.AssertEqual(0);
		buffer.GetOrderBooks().Count.AssertEqual(0);
		buffer.GetNews().Count().AssertEqual(0);
	}

	#endregion

	#region ProcessInMessage - Subscription Tracking Tests

	[TestMethod]
	public void ProcessInMessage_Subscribe_TracksSubscription()
	{
		var buffer = new StorageBuffer { FilterSubscription = true };
		var secId = CreateSecurityId();

		// Subscribe
		var subscription = CreateSubscription(1, secId, DataType.Ticks);
		buffer.ProcessInMessage(subscription);

		// Send tick with subscription id
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 1);
		buffer.ProcessOutMessage(tick);

		// Should be buffered because subscription is tracked
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(1);
	}

	[TestMethod]
	public void ProcessInMessage_Unsubscribe_RemovesTracking()
	{
		var buffer = new StorageBuffer { FilterSubscription = true };
		var secId = CreateSecurityId();

		// Subscribe
		buffer.ProcessInMessage(CreateSubscription(1, secId, DataType.Ticks));

		// Unsubscribe
		buffer.ProcessInMessage(new MarketDataMessage
		{
			OriginalTransactionId = 1,
			IsSubscribe = false,
		});

		// Send tick with subscription id
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 1);
		buffer.ProcessOutMessage(tick);

		// Should NOT be buffered because subscription was removed
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	#endregion

	#region ProcessOutMessage - SubscriptionResponse Tests

	[TestMethod]
	public void ProcessOutMessage_SubscriptionError_RemovesSubscription()
	{
		var buffer = new StorageBuffer { FilterSubscription = true };
		var secId = CreateSecurityId();

		// Subscribe
		buffer.ProcessInMessage(CreateSubscription(1, secId, DataType.Ticks));

		// Simulate error response
		buffer.ProcessOutMessage(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new NotSupportedException("Test error"),
		});

		// Send tick
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 1);
		buffer.ProcessOutMessage(tick);

		// Should NOT be buffered
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_RemovesSubscription()
	{
		var buffer = new StorageBuffer { FilterSubscription = true };
		var secId = CreateSecurityId();

		// Subscribe
		buffer.ProcessInMessage(CreateSubscription(1, secId, DataType.Ticks));

		// Simulate finished
		buffer.ProcessOutMessage(new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 1,
		});

		// Send tick
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 1);
		buffer.ProcessOutMessage(tick);

		// Should NOT be buffered
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	#endregion

	#region FilterSubscription Tests

	[TestMethod]
	public void FilterSubscription_Disabled_AllMessagesBuffered()
	{
		var buffer = new StorageBuffer { FilterSubscription = false };
		var secId = CreateSecurityId();

		// No subscription, but message should still be buffered
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		buffer.ProcessOutMessage(tick);

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(1);
	}

	[TestMethod]
	public void FilterSubscription_Enabled_OnlySubscribedMessagesBuffered()
	{
		var buffer = new StorageBuffer { FilterSubscription = true };
		var secId = CreateSecurityId();

		// No subscription - message should NOT be buffered
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 999); // Unknown subscription
		buffer.ProcessOutMessage(tick);

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	#endregion

	#region Enabled Tests

	[TestMethod]
	public void Enabled_False_NothingBuffered()
	{
		var buffer = new StorageBuffer { Enabled = false };
		var secId = CreateSecurityId();

		buffer.ProcessOutMessage(CreateTick(secId, DateTime.UtcNow, 100, 10));
		buffer.ProcessOutMessage(CreateLevel1(secId, DateTime.UtcNow, 100));
		buffer.ProcessOutMessage(CreateQuotes(secId, DateTime.UtcNow));

		buffer.GetTicks().Count.AssertEqual(0);
		buffer.GetLevel1().Count.AssertEqual(0);
		buffer.GetOrderBooks().Count.AssertEqual(0);
	}

	#endregion

	#region OfflineMode Tests

	[TestMethod]
	public void ProcessOutMessage_OfflineMode_NotBuffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		tick.OfflineMode = MessageOfflineModes.Ignore;
		buffer.ProcessOutMessage(tick);

		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_OfflineMode_NotProcessed()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		var subscription = CreateSubscription(1, secId, DataType.Ticks);
		subscription.OfflineMode = MessageOfflineModes.Ignore;
		buffer.ProcessInMessage(subscription);

		// Verify subscription was not tracked by checking FilterSubscription behavior
		buffer.FilterSubscription = true;
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		SetSubscriptionId(tick, 1);
		buffer.ProcessOutMessage(tick);

		buffer.GetTicks().Count.AssertEqual(0);
	}

	#endregion

	#region IPersistable Tests

	[TestMethod]
	public void Save_Load_PreservesSettings()
	{
		var buffer = new StorageBuffer
		{
			Enabled = false,
			EnabledLevel1 = false,
			EnabledOrderBook = false,
			EnabledPositions = true,
			EnabledTransactions = false,
			FilterSubscription = true,
			DisableStorageTimer = true,
		};

		var storage = new SettingsStorage();
		((IPersistable)buffer).Save(storage);

		var buffer2 = new StorageBuffer();
		((IPersistable)buffer2).Load(storage);

		buffer2.Enabled.AssertEqual(buffer.Enabled);
		buffer2.EnabledLevel1.AssertEqual(buffer.EnabledLevel1);
		buffer2.EnabledOrderBook.AssertEqual(buffer.EnabledOrderBook);
		buffer2.EnabledPositions.AssertEqual(buffer.EnabledPositions);
		buffer2.EnabledTransactions.AssertEqual(buffer.EnabledTransactions);
		buffer2.FilterSubscription.AssertEqual(buffer.FilterSubscription);
		buffer2.DisableStorageTimer.AssertEqual(buffer.DisableStorageTimer);
	}

	#endregion

	#region ProcessInMessage - OrderRegister Tests

	[TestMethod]
	public void ProcessInMessage_OrderRegister_BufferedAsTransaction()
	{
		var buffer = new StorageBuffer { EnabledTransactions = true };
		var secId = CreateSecurityId();

		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 123,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
		};

		buffer.ProcessInMessage(orderMsg);

		var transactions = buffer.GetTransactions();
		transactions.Count.AssertEqual(1);
		transactions.ContainsKey(secId).AssertTrue();
	}

	[TestMethod]
	public void ProcessInMessage_OrderReplace_BufferedAsTransaction()
	{
		var buffer = new StorageBuffer { EnabledTransactions = true };
		var secId = CreateSecurityId();

		var replaceMsg = new OrderReplaceMessage
		{
			SecurityId = secId,
			TransactionId = 124,
			OriginalTransactionId = 123,
			Price = 101,
			Volume = 15,
		};

		buffer.ProcessInMessage(replaceMsg);

		var transactions = buffer.GetTransactions();
		transactions.Count.AssertEqual(1);
	}

	#endregion

	#region IgnoreGenerated Tests

	[TestMethod]
	public void IgnoreGenerated_DefaultValues_ContainsExpectedTypes()
	{
		var buffer = new StorageBuffer();

		buffer.IgnoreGenerated.Contains(DataType.Ticks).AssertTrue();
		buffer.IgnoreGenerated.Contains(DataType.Level1).AssertTrue();
		buffer.IgnoreGenerated.Contains(DataType.MarketDepth).AssertTrue();
		buffer.IgnoreGenerated.Contains(DataType.OrderLog).AssertTrue();
		buffer.IgnoreGenerated.Contains(DataType.Transactions).AssertTrue();
		buffer.IgnoreGenerated.Contains(DataType.PositionChanges).AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_GeneratedMessage_NotBufferedWhenInIgnoreList()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		tick.BuildFrom = DataType.OrderLog; // Mark as generated

		buffer.ProcessOutMessage(tick);

		// Should NOT be buffered because Ticks is in IgnoreGenerated and BuildFrom is set
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_GeneratedMessage_BufferedWhenNotInIgnoreList()
	{
		var buffer = new StorageBuffer();
		buffer.IgnoreGenerated.Clear(); // Remove all from ignore list

		var secId = CreateSecurityId();
		var tick = CreateTick(secId, DateTime.UtcNow, 100, 10);
		tick.BuildFrom = DataType.OrderLog; // Mark as generated

		buffer.ProcessOutMessage(tick);

		// Should be buffered because IgnoreGenerated is empty
		var ticks = buffer.GetTicks();
		ticks.Count.AssertEqual(1);
	}

	#endregion

	#region Failed Transaction Tests

	[TestMethod]
	public void ProcessOutMessage_FailedTransaction_NotBuffered()
	{
		var buffer = new StorageBuffer();
		var secId = CreateSecurityId();

		var transaction = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Transactions,
			ServerTime = DateTime.UtcNow,
			TransactionId = 123,
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
		};

		buffer.ProcessOutMessage(transaction);

		var transactions = buffer.GetTransactions();
		transactions.Count.AssertEqual(0);
	}

	#endregion
}
