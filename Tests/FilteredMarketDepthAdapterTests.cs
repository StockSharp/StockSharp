namespace StockSharp.Tests;

[TestClass]
public class FilteredMarketDepthAdapterTests : BaseTestClass
{
	private static readonly DateTime _time = new(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc);

	private sealed class SynchronousBookTerminalAdapter(bool finished) : MessageAdapter(new IncrementalIdGenerator())
	{
		public bool OrdersSubscriptionReceived { get; private set; }

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			if (message is MarketDataMessage { IsSubscribe: true, DataType2: var dataType } book && dataType == DataType.MarketDepth)
			{
				if (finished)
					await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = book.TransactionId }, cancellationToken);
				else
					await SendOutMessageAsync(new SubscriptionResponseMessage
					{
						OriginalTransactionId = book.TransactionId,
						Error = new InvalidOperationException("book failed"),
					}, cancellationToken);
			}
			else if (message is OrderStatusMessage { IsSubscribe: true })
				OrdersSubscriptionReceived = true;
		}

		public override IMessageAdapter Clone() => new SynchronousBookTerminalAdapter(finished);
	}

	// The external book never changes between pushes, so every difference in the filtered
	// result is caused by our own orders and by nothing else.
	private static QuoteChangeMessage CreateBook(SecurityId secId, long bookId)
	{
		var book = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = _time,
			LocalTime = _time,
			Bids = [new QuoteChange(100m, 20m), new QuoteChange(99m, 10m)],
			Asks = [new QuoteChange(101m, 30m), new QuoteChange(102m, 40m)],
		};

		return book.SetSubscriptionIds(subscriptionId: bookId);
	}

	// Subscribes to a filtered book and returns the ids of the two inner subscriptions the
	// adapter fans the request out into: the raw book, and the own-orders feed it filters with.
	private static async Task<(long bookId, long ordersId)> SubscribeAsync(
		RecordingPassThroughMessageAdapter inner,
		FilteredMarketDepthAdapter adapter,
		SecurityId secId,
		long subscribeId,
		CancellationToken token)
	{
		var before = inner.InMessages.Count;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = subscribeId,
			SecurityId = secId,
			DataType2 = DataType.FilteredMarketDepth,
		}, token);

		var sent = inner.InMessages.Skip(before).ToArray();

		var bookId = sent.OfType<MarketDataMessage>().Single().TransactionId;
		var ordersId = sent.OfType<OrderStatusMessage>().Single().TransactionId;

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = bookId }, token);
		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = ordersId }, token);

		return (bookId, ordersId);
	}

	private static async Task<QuoteChangeMessage> PushBookAsync(
		RecordingPassThroughMessageAdapter inner,
		List<Message> output,
		SecurityId secId,
		long bookId,
		CancellationToken token)
	{
		var before = output.Count;

		await inner.SendOutMessageAsync(CreateBook(secId, bookId), token);

		return output.Skip(before).OfType<QuoteChangeMessage>().Single();
	}

	private static ValueTask PushOrderAsync(
		RecordingPassThroughMessageAdapter inner,
		ExecutionMessage exec,
		long ordersId,
		CancellationToken token)
	{
		exec.SetSubscriptionIds(subscriptionId: ordersId);
		return inner.SendOutMessageAsync(exec, token);
	}

	private static OrderRegisterMessage Register(SecurityId secId, long transactionId, Sides side, decimal price, decimal volume) => new()
	{
		TransactionId = transactionId,
		SecurityId = secId,
		OrderType = OrderTypes.Limit,
		Side = side,
		Price = price,
		Volume = volume,
	};

	// An update of an order we registered ourselves: keyed by the register transaction id.
	private static ExecutionMessage OwnOrderUpdate(SecurityId secId, long registerId, OrderStates state, decimal balance) => new()
	{
		SecurityId = secId,
		DataTypeEx = DataType.Transactions,
		HasOrderInfo = true,
		ServerTime = _time,
		OriginalTransactionId = registerId,
		OrderState = state,
		Balance = balance,
	};

	// An order the server introduces to us over the order status feed: it carries its own
	// TransactionId, side and price because we hold no earlier record of it.
	private static ExecutionMessage ServerSideOrder(SecurityId secId, long transactionId, Sides side, decimal price, decimal volume, decimal balance) => new()
	{
		SecurityId = secId,
		DataTypeEx = DataType.Transactions,
		HasOrderInfo = true,
		ServerTime = _time,
		TransactionId = transactionId,
		OrderState = OrderStates.Active,
		Side = side,
		OrderPrice = price,
		OrderVolume = volume,
		Balance = balance,
	};

	// Cancels a filtered book subscription and completes the handshake for both inner
	// subscriptions the adapter opened for it, so the cancellation is finished, not in flight.
	private static async Task UnsubscribeAsync(
		RecordingPassThroughMessageAdapter inner,
		FilteredMarketDepthAdapter adapter,
		long subscribeId,
		long unsubscribeId,
		CancellationToken token)
	{
		var before = inner.InMessages.Count;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = unsubscribeId,
			OriginalTransactionId = subscribeId,
		}, token);

		var sent = inner.InMessages.Skip(before).ToArray();

		var bookUnsubscribeId = sent.OfType<MarketDataMessage>().Single().TransactionId;
		var ordersUnsubscribeId = sent.OfType<OrderStatusMessage>().Single().TransactionId;

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = bookUnsubscribeId }, token);
		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = ordersUnsubscribeId }, token);
	}

	private static decimal VolumeAt(QuoteChange[] quotes, decimal price)
		=> quotes.Where(q => q.Price == price).Sum(q => q.Volume);

	// The filtered book hides exactly the volume our own order still holds at that price, and
	// tracks it through the whole life of the order. Every step re-pushes the identical raw
	// snapshot, so each assertion reads accumulated state rather than one delta.
	[TestMethod]
	public async Task OwnOrderLifecycle_HidesOnlyOurRemainingBalance()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1001, token);

		// The caller only ever sees the id it subscribed with, never the two internal ones.
		var responses = output.OfType<SubscriptionResponseMessage>().ToArray();
		responses.Length.AssertEqual(1);
		responses[0].OriginalTransactionId.AssertEqual(1001L);

		// No own orders yet: the filtered book is the raw book.
		var book = await PushBookAsync(inner, output, secId, bookId, token);
		book.IsFiltered.AssertTrue();
		book.SubscriptionId.AssertEqual(1001L);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);
		VolumeAt(book.Asks, 101m).AssertEqual(30m);

		await adapter.SendInMessageAsync(Register(secId, 10, Sides.Buy, 100m, 5m), token);

		// 20 external - 5 ours.
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		// The ack confirms the same 5 we already know about; it adds nothing.
		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Active, 5m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		// A repeat of that ack repeats the same fact.
		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Active, 5m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		// Partially filled: 3 of our 5 have left the book, 2 are still ours. 20 - 2.
		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Active, 2m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(18m);

		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Active, 2m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(18m);

		// Cancelled: none of the level is ours any more.
		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Done, 2m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);

		await PushOrderAsync(inner, OwnOrderUpdate(secId, 10, OrderStates.Done, 2m), ordersId, token);
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);

		// The other levels and the other side stayed untouched throughout.
		VolumeAt(book.Bids, 99m).AssertEqual(10m);
		VolumeAt(book.Asks, 101m).AssertEqual(30m);
		VolumeAt(book.Asks, 102m).AssertEqual(40m);
	}

	// An order snapshot from the order status feed states what the order is, not what changed:
	// receiving the identical order twice (resubscribe, reconnect replay) must not hide more
	// liquidity the second time.
	[TestMethod]
	public async Task RepeatedServerSideOrder_DoesNotHideLiquidityTwice()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1002, token);

		await PushOrderAsync(inner, ServerSideOrder(secId, 555, Sides.Buy, 100m, volume: 5m, balance: 5m), ordersId, token);

		// 20 external - 5 ours.
		var book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		await PushOrderAsync(inner, ServerSideOrder(secId, 555, Sides.Buy, 100m, volume: 5m, balance: 5m), ordersId, token);

		// Still one order of 5, so still 20 - 5.
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);
	}

	// A re-stated order carries its current balance: 3 of the 5 have been filled and left the
	// book, so 2 are ours. What we hide can never exceed the order's own volume.
	[TestMethod]
	public async Task ServerSideOrderRestatedWithSmallerBalance_ReplacesPreviousBalance()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1003, token);

		await PushOrderAsync(inner, ServerSideOrder(secId, 556, Sides.Buy, 100m, volume: 5m, balance: 5m), ordersId, token);

		var book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		await PushOrderAsync(inner, ServerSideOrder(secId, 556, Sides.Buy, 100m, volume: 5m, balance: 2m), ordersId, token);

		// 20 - 2, and under no reading of the feed less than 20 - 5.
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(18m);
	}

	// Two of our orders sit on one price level: the level hides their combined balance, and
	// each cancellation gives back exactly what that order held.
	[TestMethod]
	public async Task SeveralOwnOrdersAtOnePrice_HideTheirCombinedBalance()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1004, token);

		await adapter.SendInMessageAsync(Register(secId, 20, Sides.Buy, 100m, 5m), token);
		await adapter.SendInMessageAsync(Register(secId, 21, Sides.Buy, 100m, 3m), token);

		// 20 - (5 + 3).
		var book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(12m);

		await PushOrderAsync(inner, OwnOrderUpdate(secId, 21, OrderStates.Done, 3m), ordersId, token);

		// 20 - 5: the surviving order still hides its own volume.
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(15m);

		await PushOrderAsync(inner, OwnOrderUpdate(secId, 20, OrderStates.Done, 5m), ordersId, token);

		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);
	}

	// Own liquidity is hidden on the side the order stands on. A buy priced at the best ask
	// is still a bid, so the ask keeps its full external volume.
	[TestMethod]
	public async Task OwnOrder_HidesLiquidityOnItsOwnSideOnly()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, _) = await SubscribeAsync(inner, adapter, secId, 1005, token);

		await adapter.SendInMessageAsync(Register(secId, 30, Sides.Buy, 101m, 7m), token);

		var book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Asks, 101m).AssertEqual(30m);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);

		await adapter.SendInMessageAsync(Register(secId, 31, Sides.Sell, 101m, 12m), token);

		// 30 - 12 on the sell side; the buy at the same price still changes nothing.
		book = await PushBookAsync(inner, output, secId, bookId, token);
		VolumeAt(book.Asks, 101m).AssertEqual(18m);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);
	}

	// A level that is entirely ours is not external liquidity at all: it leaves the book
	// rather than staying behind as a zero-volume row.
	[TestMethod]
	public async Task OwnOrderCoveringWholeLevel_RemovesThatLevel()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, _) = await SubscribeAsync(inner, adapter, secId, 1006, token);

		await adapter.SendInMessageAsync(Register(secId, 40, Sides.Buy, 99m, 10m), token);

		var book = await PushBookAsync(inner, output, secId, bookId, token);

		book.Bids.Any(q => q.Price == 99m).AssertFalse();
		book.Bids.Length.AssertEqual(1);
		VolumeAt(book.Bids, 100m).AssertEqual(20m);
	}

	// Two filtered books in one adapter: an order on one security must not eat into the other
	// security's book, even at an identical price.
	[TestMethod]
	public async Task OwnOrderOnAnotherSecurity_DoesNotFilterThisBook()
	{
		var token = CancellationToken;

		var secA = Helper.CreateSecurityId();
		var secB = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookIdA, _) = await SubscribeAsync(inner, adapter, secA, 1007, token);
		var (bookIdB, _) = await SubscribeAsync(inner, adapter, secB, 1008, token);

		await adapter.SendInMessageAsync(Register(secB, 50, Sides.Buy, 100m, 5m), token);

		var bookA = await PushBookAsync(inner, output, secA, bookIdA, token);
		bookA.SubscriptionId.AssertEqual(1007L);
		VolumeAt(bookA.Bids, 100m).AssertEqual(20m);

		var bookB = await PushBookAsync(inner, output, secB, bookIdB, token);
		bookB.SubscriptionId.AssertEqual(1008L);
		VolumeAt(bookB.Bids, 100m).AssertEqual(15m);
	}

	// A subscription the caller cancelled is over. Once the unsubscribe is acknowledged, a book
	// still arriving on the inner subscription must not come back out as a filtered book on it.
	[TestMethod]
	public async Task AfterUnsubscribe_BookNoLongerReachesTheCaller()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, _) = await SubscribeAsync(inner, adapter, secId, 1009, token);

		await PushBookAsync(inner, output, secId, bookId, token);

		await UnsubscribeAsync(inner, adapter, subscribeId: 1009, unsubscribeId: 1010, token);

		// The caller was told the cancellation succeeded...
		output.OfType<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == 1010 && r.IsOk()).AssertTrue();

		output.Clear();

		await inner.SendOutMessageAsync(CreateBook(secId, bookId), token);

		// ...so the internal raw message is consumed as well, rather than leaking its private id.
		output.Count.AssertEqual(0);
	}

	// The same for the own-orders half of a cancelled subscription: an order update arriving on the
	// inner order status feed must not produce a filtered book on the subscription that is gone.
	[TestMethod]
	public async Task AfterUnsubscribe_OwnOrderNoLongerReachesTheCaller()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1011, token);

		await adapter.SendInMessageAsync(Register(secId, 60, Sides.Buy, 100m, 5m), token);
		await PushBookAsync(inner, output, secId, bookId, token);

		await UnsubscribeAsync(inner, adapter, subscribeId: 1011, unsubscribeId: 1012, token);

		output.Clear();

		await PushOrderAsync(inner, OwnOrderUpdate(secId, 60, OrderStates.Done, 5m), ordersId, token);

		output.Count.AssertEqual(0);
	}

	// Finished states that the subscription has delivered everything it will ever deliver, so a book
	// arriving on the inner subscription afterwards must not come out on the finished one.
	[TestMethod]
	public async Task AfterSubscriptionFinished_BookNoLongerReachesTheCaller()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, _) = await SubscribeAsync(inner, adapter, secId, 1014, token);

		await PushBookAsync(inner, output, secId, bookId, token);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = bookId }, token);

		// The caller was told the subscription is finished...
		output.OfType<SubscriptionFinishedMessage>().Any(f => f.OriginalTransactionId == 1014).AssertTrue();

		output.Clear();

		await inner.SendOutMessageAsync(CreateBook(secId, bookId), token);

		// ...so there is nothing left for it to receive, including the raw internal message.
		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task AfterOrderStatusFinished_BothInnerTailsAreStoppedAndSuppressed()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (bookId, ordersId) = await SubscribeAsync(inner, adapter, secId, 1016, token);
		await PushBookAsync(inner, output, secId, bookId, token);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = ordersId }, token);

		output.OfType<SubscriptionFinishedMessage>().Any(f => f.OriginalTransactionId == 1016).AssertTrue();

		var cleanup = inner.InMessages.OfType<MarketDataMessage>()
			.Single(m => !m.IsSubscribe && m.OriginalTransactionId == bookId);

		output.Clear();
		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = cleanup.TransactionId }, token);
		await inner.SendOutMessageAsync(CreateBook(secId, bookId), token);
		await PushOrderAsync(inner, OwnOrderUpdate(secId, 70, OrderStates.Active, 3m), ordersId, token);

		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task SubscribeResponse_WaitsForBothLegsAndReportsOneError()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1020,
			SecurityId = secId,
			DataType2 = DataType.FilteredMarketDepth,
		}, token);

		var bookId = inner.InMessages.OfType<MarketDataMessage>().Single(m => m.IsSubscribe).TransactionId;
		var ordersId = inner.InMessages.OfType<OrderStatusMessage>().Single(m => m.IsSubscribe).TransactionId;
		output.Clear();

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = bookId }, token);
		output.OfType<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == 1020).AssertFalse("one accepted leg does not accept the composite subscription");

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage
		{
			OriginalTransactionId = ordersId,
			Error = new InvalidOperationException("orders failed"),
		}, token);

		var responses = output.OfType<SubscriptionResponseMessage>().Where(r => r.OriginalTransactionId == 1020).ToArray();
		responses.Length.AssertEqual(1, "the parent receives one final result for its two inner requests");
		responses[0].IsOk().AssertFalse();
	}

	[TestMethod]
	public async Task UnsubscribeResponse_WaitsForBothLegsAndKeepsEitherError()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await SubscribeAsync(inner, adapter, secId, 1021, token);
		output.Clear();
		var before = inner.InMessages.Count;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 1022,
			OriginalTransactionId = 1021,
		}, token);

		var sent = inner.InMessages.Skip(before).ToArray();
		var bookRequestId = sent.OfType<MarketDataMessage>().Single().TransactionId;
		var ordersRequestId = sent.OfType<OrderStatusMessage>().Single().TransactionId;

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = bookRequestId }, token);
		output.OfType<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == 1022).AssertFalse("one stopped leg does not finish the composite cancellation");

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage
		{
			OriginalTransactionId = ordersRequestId,
			Error = new InvalidOperationException("orders unsubscribe failed"),
		}, token);

		var responses = output.OfType<SubscriptionResponseMessage>().Where(r => r.OriginalTransactionId == 1022).ToArray();
		responses.Length.AssertEqual(1);
		responses[0].IsOk().AssertFalse("an error from either leg is the result of the composite cancellation");
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task SynchronousBookTerminal_DoesNotDispatchAnOrphanOrderStatus(bool finished)
	{
		var inner = new SynchronousBookTerminalAdapter(finished);
		using var adapter = new FilteredMarketDepthAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1023,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.FilteredMarketDepth,
		}, CancellationToken);

		inner.OrdersSubscriptionReceived.AssertFalse("the second leg is not sent after the first ended synchronously");

		if (finished)
			output.OfType<SubscriptionFinishedMessage>().Single().OriginalTransactionId.AssertEqual(1023L);
		else
			output.OfType<SubscriptionResponseMessage>().Single(r => r.OriginalTransactionId == 1023).IsOk().AssertFalse();
	}

	[TestMethod]
	public async Task InactiveSubscriptionIdsRemainBounded()
	{
		const int capacity = 1_024;

		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		long firstBookId = 0;
		long lastBookId = 0;
		long firstOrdersId = 0;
		long lastOrdersId = 0;

		for (var i = 0; i <= capacity; i++)
		{
			var before = inner.InMessages.Count;

			await adapter.SendInMessageAsync(new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = 10_000 + i,
				SecurityId = secId,
				DataType2 = DataType.FilteredMarketDepth,
			}, token);

			var sent = inner.InMessages.Skip(before).ToArray();
			var bookId = sent.OfType<MarketDataMessage>().Single(m => m.IsSubscribe).TransactionId;
			var ordersId = sent.OfType<OrderStatusMessage>().Single(m => m.IsSubscribe).TransactionId;

			if (i == 0)
			{
				firstBookId = bookId;
				firstOrdersId = ordersId;
			}

			lastBookId = bookId;
			lastOrdersId = ordersId;
			await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = bookId }, token);
		}

		output.Clear();

		await inner.SendOutMessageAsync(CreateBook(secId, firstBookId), token);
		await inner.SendOutMessageAsync(CreateBook(secId, lastBookId), token);
		await PushOrderAsync(inner, ServerSideOrder(secId, 30_001, Sides.Buy, 100m, 1m, 1m), firstOrdersId, token);
		await PushOrderAsync(inner, ServerSideOrder(secId, 30_002, Sides.Buy, 100m, 1m, 1m), lastOrdersId, token);

		var remaining = output.OfType<QuoteChangeMessage>().Single();
		remaining.SubscriptionId.AssertEqual(firstBookId, "the oldest tombstone is evicted once the bounded tail is full");
		output.OfType<ExecutionMessage>().Single().SubscriptionId.AssertEqual(firstOrdersId);
	}

	[TestMethod]
	public async Task ReusedInnerSubscriptionIdOverridesItsOldTombstone()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var (oldBookId, _) = await SubscribeAsync(inner, adapter, secId, 20_001, token);
		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = oldBookId }, token);

		((IncrementalIdGenerator)inner.TransactionIdGenerator).Current = 0;

		var (newBookId, _) = await SubscribeAsync(inner, adapter, secId, 20_002, token);
		newBookId.AssertEqual(oldBookId);

		output.Clear();
		await inner.SendOutMessageAsync(CreateBook(secId, newBookId), token);

		var book = output.OfType<QuoteChangeMessage>().Single();
		book.SubscriptionId.AssertEqual(20_002L);
	}

	// A copy of a wrapper owns a copy of what it wraps - as every other wrapper's Clone does.
	// Sharing the inner adapter makes the copy a second reader of one connection, not a copy.
	[TestMethod]
	public void Clone_HasItsOwnInnerAdapter()
	{
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		using var clone = (FilteredMarketDepthAdapter)adapter.Clone();

		AreNotSame(adapter.InnerAdapter, clone.InnerAdapter);
	}

	// Work handed to the copy stays with it. Two wrappers over one inner adapter both listen to its
	// output, so the original hands its own consumer messages it was never asked for.
	[TestMethod]
	public async Task Clone_DoesNotShareTheWork()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new FilteredMarketDepthAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		using var clone = (FilteredMarketDepthAdapter)adapter.Clone();

		await clone.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1015,
			SecurityId = secId,
			DataType2 = DataType.FilteredMarketDepth,
		}, token);

		output.Count.AssertEqual(0);
	}
}
