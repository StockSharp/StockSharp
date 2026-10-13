namespace StockSharp.Tests;

[TestClass]
public class OrderBookTruncateMessageAdapterTests : BaseTestClass
{
	private static QuoteChangeMessage CreateSnapshot(SecurityId securityId, DateTime time, long[] subscriptionIds, int depth)
	{
		var bids = new QuoteChange[depth];
		var asks = new QuoteChange[depth];

		for (var i = 0; i < depth; i++)
		{
			bids[i] = new QuoteChange(100m - i, i + 1);
			asks[i] = new QuoteChange(101m + i, i + 1);
		}

		var msg = new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = time,
			LocalTime = time,
			State = null,
			Bids = bids,
			Asks = asks,
		};

		msg.SetSubscriptionIds(subscriptionIds);

		return msg;
	}

	/// <summary>
	/// Someone who asks for five levels gets five levels. The feed underneath may only be able to
	/// serve ten, and asking it for ten is the right thing to do - but that is the adapter's business,
	/// not the subscriber's: the extra five are cut off before the book is handed over, and the
	/// request the subscriber still holds is left as it was written.
	/// </summary>
	[TestMethod]
	public async Task TruncatedBookReachesTheSubscriberAtTheDepthAsked()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);
		var depths = inner.SupportedOrderBookDepths.ToArray();
		AreEqual(1, depths.Length, "SupportedOrderBookDepths length.");
		AreEqual(10, depths[0], "SupportedOrderBookDepths[0].");
		AreEqual(10, inner.NearestSupportedDepth(5), "NearestSupportedDepth(5).");

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var md = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		};

		await adapter.SendInMessageAsync(md, token);

		md.MaxDepth.AssertEqual(5, "the subscriber's own request is not rewritten under it");

		inner.InMessages.Count.AssertEqual(1);
		AreEqual(10, ((MarketDataMessage)inner.InMessages[0]).MaxDepth, "MaxDepth passed to inner adapter.");

		output.Clear();

		// the feed serves the ten levels it was asked for
		await inner.SendOutMessageAsync(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10), token);

		var book = output.OfType<QuoteChangeMessage>().Single();

		book.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue("the book is delivered to the subscription that asked for it");

		// the five best levels of each side, and nothing of the five behind them
		AssertQuotes(book.Bids, [(100m, 1m), (99m, 2m), (98m, 3m), (97m, 4m), (96m, 5m)]);
		AssertQuotes(book.Asks, [(101m, 1m), (102m, 2m), (103m, 3m), (104m, 4m), (105m, 5m)]);
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenDoNotBuildOrderBookIncrement_DoesNotRewrite_AndDoesNotTruncateSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
			DoNotBuildOrderBookIncrement = true,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertEqual(5);

		output.Clear();

		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		await inner.SendOutMessageAsync(snapshot, CancellationToken);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, snapshot).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenMaxDepthIsNotSpecified_DoesNotTruncateSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertNull();

		output.Clear();

		var snapshot = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		await inner.SendOutMessageAsync(snapshot, CancellationToken);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, snapshot).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task MarketDepthSubscribe_WhenNoSupportedDepths_KeepsMaxDepth_AndTruncatesSnapshot()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: []);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		// When no supported depths, keep original MaxDepth (don't set to null)
		((MarketDataMessage)inner.InMessages[0]).MaxDepth.AssertEqual(5);

		output.Clear();

		// Inner adapter might still return more than requested
		await inner.SendOutMessageAsync(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10), CancellationToken);

		// But we still truncate to the originally requested depth
		var truncated = output.OfType<QuoteChangeMessage>().Single();
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
	}

	[TestMethod]
	public async Task QuoteChange_Snapshot_IsTruncatedPerSubscriptionId()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10), CancellationToken);

		var truncated = output.OfType<QuoteChangeMessage>().Single();
		truncated.Bids.Length.AssertEqual(5);
		truncated.Asks.Length.AssertEqual(5);
		truncated.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		truncated.Bids[0].Price.AssertEqual(100m);
		truncated.Bids[^1].Price.AssertEqual(96m);
		truncated.Asks[0].Price.AssertEqual(101m);
		truncated.Asks[^1].Price.AssertEqual(105m);
	}

	[TestMethod]
	public async Task QuoteChange_Snapshot_SplitsGroupsByDepth_AndKeepsUntrackedIdsInOriginal()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 10,
		}, token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 3,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 3,
		}, token);

		output.Clear();

		var original = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1, 2, 3], depth: 10);
		await inner.SendOutMessageAsync(original, CancellationToken);

		var quotes = output.OfType<QuoteChangeMessage>().ToArray();
		quotes.Length.AssertEqual(3);

		var kept = quotes.Single(q => ReferenceEquals(q, original));
		kept.GetSubscriptionIds().SequenceEqual([2L]).AssertTrue();
		kept.Bids.Length.AssertEqual(10);
		kept.Asks.Length.AssertEqual(10);

		var depth5 = quotes.Single(q => q.GetSubscriptionIds().SequenceEqual([1L]));
		depth5.Bids.Length.AssertEqual(5);
		depth5.Asks.Length.AssertEqual(5);

		var depth3 = quotes.Single(q => q.GetSubscriptionIds().SequenceEqual([3L]));
		depth3.Bids.Length.AssertEqual(3);
		depth3.Asks.Length.AssertEqual(3);
	}

	[TestMethod]
	public async Task QuoteChange_Increment_IsNotTruncated()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		var inc = CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10);
		inc.State = QuoteChangeStates.Increment;

		await inner.SendOutMessageAsync(inc, CancellationToken);

		var outMsg = output.OfType<QuoteChangeMessage>().Single();
		ReferenceEquals(outMsg, inc).AssertTrue();
		outMsg.Bids.Length.AssertEqual(10);
		outMsg.Asks.Length.AssertEqual(10);
	}

	[TestMethod]
	public async Task SubscriptionResponse_Error_RemovesDepthTracking()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedOrderBookDepths: [10]);

		using var adapter = new OrderBookTruncateMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			MaxDepth = 5,
		}, token);

		output.Clear();

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("error"),
		}, CancellationToken);

		await inner.SendOutMessageAsync(CreateSnapshot(secId, DateTime.UtcNow, subscriptionIds: [1], depth: 10), CancellationToken);

		var quote = output.OfType<QuoteChangeMessage>().Single();
		quote.Bids.Length.AssertEqual(10);
		quote.Asks.Length.AssertEqual(10);
	}

	#region Filtered market depth

	// Public book shared by the filtered depth tests: bid 100 holds 10 lots and bid 99 holds 8, so every
	// expected volume below is that public figure minus the own balance still working at that price.
	private static QuoteChangeMessage CreatePublicBook(SecurityId securityId, long bookSubscriptionId)
		=> new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10m), new QuoteChange(99m, 8m)],
			Asks = [new QuoteChange(101m, 12m), new QuoteChange(102m, 9m)],
		}.SetSubscriptionIds(subscriptionId: bookSubscriptionId);

	// An order announced by the order status subscription names itself by TransactionId.
	private static ExecutionMessage CreateOwnOrder(SecurityId securityId, long ordersSubscriptionId, long transactionId, Sides side, decimal price, decimal balance)
		=> new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = securityId,
			ServerTime = DateTime.UtcNow,
			TransactionId = transactionId,
			OrderState = OrderStates.Active,
			Side = side,
			OrderPrice = price,
			OrderVolume = balance,
			Balance = balance,
		}.SetSubscriptionIds(subscriptionId: ordersSubscriptionId);

	// A later state of an already known order names that order by OriginalTransactionId.
	private static ExecutionMessage CreateOwnOrderUpdate(SecurityId securityId, long ordersSubscriptionId, long orderTransactionId, OrderStates state, decimal balance)
		=> new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = securityId,
			ServerTime = DateTime.UtcNow,
			OriginalTransactionId = orderTransactionId,
			OrderState = state,
			Balance = balance,
		}.SetSubscriptionIds(subscriptionId: ordersSubscriptionId);

	private static OrderRegisterMessage CreateRegister(SecurityId securityId, long transactionId, Sides side, decimal price, decimal volume)
		=> new()
		{
			TransactionId = transactionId,
			SecurityId = securityId,
			Side = side,
			Price = price,
			Volume = volume,
			OrderType = OrderTypes.Limit,
			PortfolioName = "TestPf",
		};

	private static async Task<(long bookId, long ordersId)> SubscribeFilteredAsync(FilteredMarketDepthAdapter adapter, RecordingPassThroughMessageAdapter inner, SecurityId securityId, long transactionId, CancellationToken token)
	{
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = transactionId,
			SecurityId = securityId,
			DataType2 = DataType.FilteredMarketDepth,
		}, token);

		// The adapter splits the request into a plain book subscription and an order status subscription.
		var book = inner.InMessages.OfType<MarketDataMessage>().Single(m => m.DataType2 == DataType.MarketDepth);
		var orders = inner.InMessages.OfType<OrderStatusMessage>().Single();

		return (book.TransactionId, orders.TransactionId);
	}

	private static QuoteChangeMessage TakeFilteredBook(List<Message> output, long subscribeId)
	{
		var book = output.OfType<QuoteChangeMessage>().Single();

		book.IsFiltered.AssertTrue();
		book.GetSubscriptionIds().SequenceEqual([subscribeId]).AssertTrue();

		output.Clear();

		return book;
	}

	private static void AssertQuotes(QuoteChange[] quotes, (decimal price, decimal volume)[] expected)
	{
		quotes.Length.AssertEqual(expected.Length);

		for (var i = 0; i < expected.Length; i++)
		{
			quotes[i].Price.AssertEqual(expected[i].price);
			quotes[i].Volume.AssertEqual(expected[i].volume);
		}
	}

	[TestMethod]
	public async Task FilteredDepth_OwnOrder_IsRemovedFromPublicBookOnce()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreateOwnOrder(secId, ordersId, 555, Sides.Buy, 99m, 3m), token);

		// One own buy of 3 working at 99: the 99 bid shows 8 - 3 = 5, every other row is the public one.
		var book = TakeFilteredBook(output, 1000);
		AssertQuotes(book.Bids, [(100m, 10m), (99m, 5m)]);
		AssertQuotes(book.Asks, [(101m, 12m), (102m, 9m)]);
	}

	[TestMethod]
	public async Task FilteredDepth_SameOwnOrderDeliveredTwice_IsRemovedOnce()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreateOwnOrder(secId, ordersId, 555, Sides.Buy, 99m, 3m), token);

		var first = TakeFilteredBook(output, 1000);
		AssertQuotes(first.Bids, [(100m, 10m), (99m, 5m)]);

		// The same order announced again is still one order of 3 lots, so the 99 bid stays at 8 - 3 = 5.
		await inner.SendOutMessageAsync(CreateOwnOrder(secId, ordersId, 555, Sides.Buy, 99m, 3m), token);

		var second = TakeFilteredBook(output, 1000);
		AssertQuotes(second.Bids, [(100m, 10m), (99m, 5m)]);
		AssertQuotes(second.Asks, [(101m, 12m), (102m, 9m)]);
	}

	[TestMethod]
	public async Task FilteredDepth_PartiallyFilledOwnOrder_RemovesOnlyRemainingBalance()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);
		await inner.SendOutMessageAsync(CreateOwnOrder(secId, ordersId, 555, Sides.Buy, 99m, 3m), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreateOwnOrderUpdate(secId, ordersId, 555, OrderStates.Active, 1m), token);

		// 2 of the 3 lots are gone from the book by being traded, only the balance of 1 is still ours: 8 - 1 = 7.
		var book = TakeFilteredBook(output, 1000);
		AssertQuotes(book.Bids, [(100m, 10m), (99m, 7m)]);
		AssertQuotes(book.Asks, [(101m, 12m), (102m, 9m)]);
	}

	[TestMethod]
	public async Task FilteredDepth_CancelledOwnOrder_ReturnsItsVolumeToTheBook()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);
		await inner.SendOutMessageAsync(CreateOwnOrder(secId, ordersId, 555, Sides.Buy, 99m, 3m), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreateOwnOrderUpdate(secId, ordersId, 555, OrderStates.Done, 3m), token);

		// Nothing of ours is left working, so the filtered book is the public book again.
		var book = TakeFilteredBook(output, 1000);
		AssertQuotes(book.Bids, [(100m, 10m), (99m, 8m)]);
		AssertQuotes(book.Asks, [(101m, 12m), (102m, 9m)]);
	}

	[TestMethod]
	public async Task FilteredDepth_ReplacedOwnOrder_MovesRemovedVolumeToTheNewPrice()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await adapter.SendInMessageAsync(CreateRegister(secId, 555, Sides.Buy, 99m, 3m), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);

		var before = TakeFilteredBook(output, 1000);
		AssertQuotes(before.Bids, [(100m, 10m), (99m, 5m)]);

		await adapter.SendInMessageAsync(new OrderReplaceMessage
		{
			TransactionId = 556,
			OriginalTransactionId = 555,
			SecurityId = secId,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 3m,
			OrderType = OrderTypes.Limit,
			PortfolioName = "TestPf",
		}, token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateOwnOrderUpdate(secId, ordersId, 555, OrderStates.Done, 3m), token);

		// The 3 lots moved from 99 to 100: 99 is whole again at 8, and 100 shows 10 - 3 = 7.
		var after = TakeFilteredBook(output, 1000);
		AssertQuotes(after.Bids, [(100m, 7m), (99m, 8m)]);
		AssertQuotes(after.Asks, [(101m, 12m), (102m, 9m)]);
	}

	[TestMethod]
	public async Task FilteredDepth_TwoOwnOrdersAtOnePrice_AreBothAccountedFor()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeFilteredAsync(adapter, inner, secId, 1000, token);

		await adapter.SendInMessageAsync(CreateRegister(secId, 555, Sides.Buy, 99m, 3m), token);
		await adapter.SendInMessageAsync(CreateRegister(secId, 556, Sides.Buy, 99m, 2m), token);
		output.Clear();

		await inner.SendOutMessageAsync(CreatePublicBook(secId, bookId), token);

		// Both orders sit at 99, so 3 + 2 lots come out of that bid: 8 - 5 = 3.
		var both = TakeFilteredBook(output, 1000);
		AssertQuotes(both.Bids, [(100m, 10m), (99m, 3m)]);

		await inner.SendOutMessageAsync(CreateOwnOrderUpdate(secId, ordersId, 555, OrderStates.Done, 3m), token);

		// Only the order of 2 lots is left working: 8 - 2 = 6.
		var left = TakeFilteredBook(output, 1000);
		AssertQuotes(left.Bids, [(100m, 10m), (99m, 6m)]);
		AssertQuotes(left.Asks, [(101m, 12m), (102m, 9m)]);
	}

	#endregion

	#region Mock Manager Tests

	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOrderBookTruncateManager>();

		var toInner = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.MarketDepth,
			MaxDepth = 10,
		};
		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message)toInner, toOut: [toOut]));

		using var adapter = new OrderBookTruncateMessageAdapter(inner, manager.Object);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (message, _) =>
		{
			output.Add(message);
			return default;
		};

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);
		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task InnerMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOrderBookTruncateManager>();

		var forward = new ConnectMessage();
		var extra = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Bids = [],
			Asks = [],
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: [extra]));

		using var adapter = new OrderBookTruncateMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(new DisconnectMessage(), CancellationToken);

		output.Count.AssertEqual(2);
		output[0].AssertSame(forward);
		output[1].AssertSame(extra);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task InnerMessage_WhenForwardIsNull_DoesNotForwardOriginal()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOrderBookTruncateManager>();

		var extra = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Bids = [],
			Asks = [],
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)null, extraOut: [extra]));

		using var adapter = new OrderBookTruncateMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(new QuoteChangeMessage { SecurityId = Helper.CreateSecurityId(), Bids = [], Asks = [] }, CancellationToken);

		output.Count.AssertEqual(1);
		output[0].AssertSame(extra);
	}

	[TestMethod]
	public async Task InnerMessage_WhenNoExtraOut_OnlyForwardsOriginal()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOrderBookTruncateManager>();

		var forward = new ConnectMessage();

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: []));

		using var adapter = new OrderBookTruncateMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(new DisconnectMessage(), CancellationToken);

		output.Count.AssertEqual(1);
		output[0].AssertSame(forward);
	}

	#endregion
}
