namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Tests that verify ValuesChanged and UpdateSecurity* protection logic in Connector
/// for Level1, Ticks, and OrderBook.
/// These tests demonstrate bugs where ValuesChanged fires or Security properties
/// are modified when they should not be (no subscription, historical data, Count-based subscription).
/// </summary>
[TestClass]
public class ConnectorMarketDataProtectionTests : BaseTestClass
{
	#region Infrastructure

	private sealed class TestConnector : Connector
	{
		public TestConnector(BasketMessageAdapter adapter)
			: base(new InMemorySecurityStorage(), new InMemoryPositionStorage(), new InMemoryExchangeInfoProvider(), initAdapter: false, initChannels: false)
		{
			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();
			Adapter = adapter;
		}
	}

	private sealed class MockMarketDataAdapter : MessageAdapter
	{
		public long LastSubscribedId { get; private set; }

		public MockMarketDataAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || dataType == DataType.Transactions;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					await SendOutMessageAsync(new ConnectMessage(), cancellationToken);
					break;
				case MessageTypes.Disconnect:
					await SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
					break;
				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					if (mdMsg.IsSubscribe)
					{
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						LastSubscribedId = mdMsg.TransactionId;

						// A live (non-historical) subscription reaches Online; Count/To based ones stay Active.
						if (!mdMsg.IsHistoryOnly())
							await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = mdMsg.TransactionId }, cancellationToken);
					}
					else
					{
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}
					break;
				}
				case MessageTypes.Reset:
					break;
			}
		}

		public async ValueTask SendLevel1(long subscriptionId, SecurityId secId, DateTime serverTime, CancellationToken cancellationToken)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = serverTime,
				LocalTime = serverTime,
			};
			msg.Add(Level1Fields.LastTradePrice, 42m);
			msg.Add(Level1Fields.BestBidPrice, 41m);
			msg.Add(Level1Fields.BestAskPrice, 43m);

			if (subscriptionId != 0)
				msg.SetSubscriptionIds(subscriptionId: subscriptionId);

			await SendOutMessageAsync(msg, cancellationToken);
		}

		public async ValueTask SendLevel1(long subscriptionId, SecurityId secId, DateTime serverTime, IEnumerable<KeyValuePair<Level1Fields, object>> fields, CancellationToken cancellationToken)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = serverTime,
				LocalTime = serverTime,
			};

			foreach (var pair in fields)
				msg.Add(pair.Key, pair.Value);

			if (subscriptionId != 0)
				msg.SetSubscriptionIds(subscriptionId: subscriptionId);

			await SendOutMessageAsync(msg, cancellationToken);
		}

		public async ValueTask SendTick(long subscriptionId, SecurityId secId, DateTime serverTime, CancellationToken cancellationToken)
		{
			var msg = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				ServerTime = serverTime,
				LocalTime = serverTime,
				TradePrice = 42m,
				TradeVolume = 1m,
				TradeId = serverTime.Ticks,
			};

			if (subscriptionId != 0)
				msg.SetSubscriptionIds(subscriptionId: subscriptionId);

			await SendOutMessageAsync(msg, cancellationToken);
		}

		public async ValueTask SendOrderBook(long subscriptionId, SecurityId secId, DateTime serverTime, CancellationToken cancellationToken)
		{
			var msg = new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = serverTime,
				LocalTime = serverTime,
				Bids = [new QuoteChange(99m, 10m)],
				Asks = [new QuoteChange(101m, 10m)],
			};

			if (subscriptionId != 0)
				msg.SetSubscriptionIds(subscriptionId: subscriptionId);

			await SendOutMessageAsync(msg, cancellationToken);
		}
	}

	private static (TestConnector connector, MockMarketDataAdapter adapter) CreateConnector()
	{
		var state = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(state);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var idGen = new MillisecondIncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			state,
			connectionManager,
			pendingState,
			subscriptionRouting,
			parentChildMap,
			orderRouting,
			a => a, candleBuilderProvider, () => false, idGen);

        var basket = new BasketMessageAdapter(
            idGen,
            candleBuilderProvider,
            new InMemorySecurityMessageAdapterProvider(),
            new InMemoryPortfolioMessageAdapterProvider(),
            null, null, routingManager)
        {
            IgnoreExtraAdapters = true,
            LatencyManager = null,
            SlippageManager = null,
            CommissionManager = null
        };

        var connector = new TestConnector(basket);
		var adapter = new MockMarketDataAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		return (connector, adapter);
	}

	private static async Task<long> SubscribeAndWait(Connector connector, MockMarketDataAdapter adapter, DataType dataType, SecurityId secId, long? count = null, CancellationToken cancellationToken = default)
	{
		var sub = new Subscription(dataType, new Security { Id = secId.ToStringId() });

		if (count != null)
			sub.Count = count;

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		_ = connector.SubscribeAsync(sub, cancellationToken).AsTask();
		await started.Task.WithCancellation(cancellationToken);

		return adapter.LastSubscribedId;
	}

	private static async Task<long> SubscribeOnlineAndWait(Connector connector, MockMarketDataAdapter adapter, DataType dataType, SecurityId secId, CancellationToken cancellationToken)
	{
		var (_, id) = await SubscribeOnlineAndWaitSubscription(connector, adapter, dataType, secId, cancellationToken);
		return id;
	}

	private static async Task<(Subscription subscription, long id)> SubscribeOnlineAndWaitSubscription(
		Connector connector, MockMarketDataAdapter adapter, DataType dataType, SecurityId secId, CancellationToken cancellationToken)
	{
		var subscription = new Subscription(dataType, new Security { Id = secId.ToStringId() });

		var online = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionOnline += s => { if (ReferenceEquals(s, subscription)) online.TrySetResult(true); };

		_ = connector.SubscribeAsync(subscription, cancellationToken).AsTask();
		await online.Task.WithCancellation(cancellationToken);

		return (subscription, adapter.LastSubscribedId);
	}

	private static async Task UnsubscribeAndWait(Connector connector, Subscription subscription, CancellationToken cancellationToken)
	{
		var stopped = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStopped += (s, _) =>
		{
			if (ReferenceEquals(s, subscription))
				stopped.TrySetResult(true);
		};

		connector.UnSubscribe(subscription);
		await stopped.Task.WithCancellation(cancellationToken);
	}

	#endregion

	#region Level1 — ValuesChanged

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_NoSubscription_ShouldNotTriggerValuesChanged()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);
		await connector.GetSecurityAsync(secId, CancellationToken);

		var valuesChangedCount = 0;
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			Interlocked.Increment(ref valuesChangedCount);
		};

		await adapter.SendLevel1(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

		valuesChangedCount.AssertEqual(0, "ValuesChanged should not fire for unsubscribed Level1 data");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_CountSubscription_ShouldNotTriggerValuesChanged()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);

		using var runCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var subscriptionId = await SubscribeAndWait(connector, adapter, DataType.Level1, secId, count: 10, cancellationToken: runCts.Token);

		await connector.GetSecurityAsync(secId, CancellationToken);

		var valuesChangedCount = 0;
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			Interlocked.Increment(ref valuesChangedCount);
		};

		await adapter.SendLevel1(subscriptionId, secId, DateTime.UtcNow.AddDays(-1), CancellationToken);
		await Task.Delay(500, CancellationToken);

		valuesChangedCount.AssertEqual(0, "ValuesChanged should not fire for Count-based historical Level1 subscription");

		runCts.Cancel();
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_AfterTicks_ValuesChangedCarriesFilteredChanges()
	{
		// Once ticks own the last-trade fields, the connector drops them from the Level1 snapshot.
		// ValuesChanged must report that same filtered set, not the raw message.
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);

		using var runCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var tickSubId = await SubscribeOnlineAndWait(connector, adapter, DataType.Ticks, secId, runCts.Token);
		var level1SubId = await SubscribeOnlineAndWait(connector, adapter, DataType.Level1, secId, runCts.Token);

		await connector.GetSecurityAsync(secId, CancellationToken);

		var tickRaised = AsyncHelper.CreateTaskCompletionSource<bool>();
		var level1Raised = AsyncHelper.CreateTaskCompletionSource<IDictionary<Level1Fields, object>>();

		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			var dict = changes.ToDictionary(p => p.Key, p => p.Value);

			// OpenInterest is sent by the Level1 message only, so it tells the two sources apart.
			if (dict.ContainsKey(Level1Fields.OpenInterest))
				level1Raised.TrySetResult(dict);
			else
				tickRaised.TrySetResult(true);
		};

		await adapter.SendTick(tickSubId, secId, DateTime.UtcNow, CancellationToken);
		await tickRaised.Task.WithCancellation(CancellationToken);

		var fields = new Dictionary<Level1Fields, object>
		{
			{ Level1Fields.LastTradePrice, 50m },
			{ Level1Fields.OpenInterest, 7m },
		};

		await adapter.SendLevel1(level1SubId, secId, DateTime.UtcNow, fields, CancellationToken);

		var raised = await level1Raised.Task.WithCancellation(CancellationToken);

		raised.ContainsKey(Level1Fields.OpenInterest).AssertTrue("neutral Level1 field must reach the subscriber");
		raised.ContainsKey(Level1Fields.LastTradePrice).AssertFalse("last-trade field is owned by the tick stream and must be filtered out of ValuesChanged");

		runCts.Cancel();
	}

	#endregion

	#region Level1 — UpdateSecurityByLevel1

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_NoSubscription_ShouldNotUpdateSecurityProperties()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityByLevel1 = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);
		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendLevel1(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.LastTick.AssertNull("Security.LastTick should not be set by unsubscribed Level1");
		security.BestBid.AssertNull("Security.BestBid should not be set by unsubscribed Level1");
		security.BestAsk.AssertNull("Security.BestAsk should not be set by unsubscribed Level1");
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_CountSubscription_ShouldNotUpdateSecurityProperties()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityByLevel1 = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);

		using var runCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var subscriptionId = await SubscribeAndWait(connector, adapter, DataType.Level1, secId, count: 10, cancellationToken: runCts.Token);

		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendLevel1(subscriptionId, secId, DateTime.UtcNow.AddDays(-1), CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.BestBid.AssertNull("Security.BestBid should not be set by Count-based historical Level1");
		security.BestAsk.AssertNull("Security.BestAsk should not be set by Count-based historical Level1");
#pragma warning restore CS0618 // Type or member is obsolete

		runCts.Cancel();
	}

	#endregion

	#region Ticks — ValuesChanged

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Tick_NoSubscription_ShouldNotTriggerValuesChanged()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);
		await connector.GetSecurityAsync(secId, CancellationToken);

		var valuesChangedCount = 0;
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			Interlocked.Increment(ref valuesChangedCount);
		};

		await adapter.SendTick(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

		valuesChangedCount.AssertEqual(0, "ValuesChanged should not fire for unsubscribed tick data");
	}

	#endregion

	#region Ticks — UpdateSecurityLastQuotes

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Tick_NoSubscription_ShouldNotUpdateSecurityLastTick()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityLastQuotes = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);
		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendTick(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.LastTick.AssertNull("Security.LastTick should not be set by unsubscribed tick");
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Tick_CountSubscription_ShouldNotUpdateSecurityLastTick()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityLastQuotes = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);

		using var runCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var subscriptionId = await SubscribeAndWait(connector, adapter, DataType.Ticks, secId, count: 10, cancellationToken: runCts.Token);

		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendTick(subscriptionId, secId, DateTime.UtcNow.AddDays(-1), CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.LastTick.AssertNull("Security.LastTick should not be set by Count-based historical tick");
#pragma warning restore CS0618 // Type or member is obsolete

		runCts.Cancel();
	}

	#endregion

	#region OrderBook — ValuesChanged

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderBook_NoSubscription_ShouldNotTriggerValuesChanged()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);
		await connector.GetSecurityAsync(secId, CancellationToken);

		var valuesChangedCount = 0;
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			Interlocked.Increment(ref valuesChangedCount);
		};

		await adapter.SendOrderBook(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

		valuesChangedCount.AssertEqual(0, "ValuesChanged should not fire for unsubscribed order book data");
	}

	#endregion

	#region OrderBook — UpdateSecurityLastQuotes

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderBook_NoSubscription_ShouldNotUpdateSecurityBestQuotes()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityLastQuotes = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);
		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendOrderBook(0, secId, DateTime.UtcNow, CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
        security.BestBid.AssertNull("Security.BestBid should not be set by unsubscribed order book");
        security.BestAsk.AssertNull("Security.BestAsk should not be set by unsubscribed order book");
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderBook_CountSubscription_ShouldNotUpdateSecurityBestQuotes()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

#pragma warning disable CS0618
		connector.UpdateSecurityLastQuotes = true;
#pragma warning restore CS0618

		await connector.ConnectAsync(CancellationToken);

		using var runCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var subscriptionId = await SubscribeAndWait(connector, adapter, DataType.MarketDepth, secId, count: 10, cancellationToken: runCts.Token);

		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		await adapter.SendOrderBook(subscriptionId, secId, DateTime.UtcNow.AddDays(-1), CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.BestBid.AssertNull("Security.BestBid should not be set by Count-based historical order book");
		security.BestAsk.AssertNull("Security.BestAsk should not be set by Count-based historical order book");
#pragma warning restore CS0618 // Type or member is obsolete

		runCts.Cancel();
	}

	#endregion

	#region Protection ends with the stream that claimed the fields

	private static async Task VerifyLevel1OwnershipIsHeldUntilLastSubscriptionEnds(DataType ownerType, CancellationToken cancellationToken)
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(cancellationToken);

		using var level1Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		var level1SubId = await SubscribeOnlineAndWait(connector, adapter, DataType.Level1, secId, level1Cts.Token);
		var security = await connector.GetSecurityAsync(secId, cancellationToken);

		var (first, _) = await SubscribeOnlineAndWaitSubscription(connector, adapter, ownerType, secId, cancellationToken);
		var (second, ownerId) = await SubscribeOnlineAndWaitSubscription(connector, adapter, ownerType, secId, cancellationToken);

		var ownerApplied = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstLevel1Applied = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondLevel1Applied = AsyncHelper.CreateTaskCompletionSource<bool>();

		connector.ValuesChanged += (_, changes, _, _) =>
		{
			var openInterest = changes.FirstOrDefault(p => p.Key == Level1Fields.OpenInterest).Value;

			if (Equals(openInterest, 7m))
				firstLevel1Applied.TrySetResult(true);
			else if (Equals(openInterest, 8m))
				secondLevel1Applied.TrySetResult(true);
			else
				ownerApplied.TrySetResult(true);
		};

		var ownerTime = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

		if (ownerType == DataType.MarketDepth)
			await adapter.SendOrderBook(ownerId, secId, ownerTime, cancellationToken);
		else
			await adapter.SendTick(ownerId, secId, ownerTime, cancellationToken);

		await ownerApplied.Task.WithCancellation(cancellationToken);
		await UnsubscribeAndWait(connector, first, cancellationToken);

		var protectedFields = ownerType == DataType.MarketDepth
			? new Dictionary<Level1Fields, object>
			{
				{ Level1Fields.BestBidPrice, 51m },
				{ Level1Fields.BestAskPrice, 53m },
				{ Level1Fields.OpenInterest, 7m },
			}
			: new Dictionary<Level1Fields, object>
			{
				{ Level1Fields.LastTradePrice, 51m },
				{ Level1Fields.OpenInterest, 7m },
			};

		await adapter.SendLevel1(level1SubId, secId, ownerTime.AddSeconds(1), protectedFields, cancellationToken);
		await firstLevel1Applied.Task.WithCancellation(cancellationToken);

		if (ownerType == DataType.MarketDepth)
		{
			connector.GetSecurityValue(security, Level1Fields.BestBidPrice).AssertEqual((object)99m,
				"closing one of two books must not release the remaining book's best bid");
			connector.GetSecurityValue(security, Level1Fields.BestAskPrice).AssertEqual((object)101m,
				"closing one of two books must not release the remaining book's best ask");
		}
		else
		{
			connector.GetSecurityValue(security, Level1Fields.LastTradePrice).AssertEqual((object)42m,
				"closing one of two tick streams must not release the remaining stream's last trade");
		}

		await UnsubscribeAndWait(connector, second, cancellationToken);
		protectedFields[Level1Fields.OpenInterest] = 8m;
		await adapter.SendLevel1(level1SubId, secId, ownerTime.AddSeconds(2), protectedFields, cancellationToken);
		await secondLevel1Applied.Task.WithCancellation(cancellationToken);

		if (ownerType == DataType.MarketDepth)
		{
			connector.GetSecurityValue(security, Level1Fields.BestBidPrice).AssertEqual((object)51m);
			connector.GetSecurityValue(security, Level1Fields.BestAskPrice).AssertEqual((object)53m);
		}
		else
			connector.GetSecurityValue(security, Level1Fields.LastTradePrice).AssertEqual((object)51m);

		level1Cts.Cancel();
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public Task Level1BestQuotesStayProtectedUntilTheLastOrderBookEnds()
		=> VerifyLevel1OwnershipIsHeldUntilLastSubscriptionEnds(DataType.MarketDepth, CancellationToken);

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public Task Level1LastTradeStaysProtectedUntilTheLastTickStreamEnds()
		=> VerifyLevel1OwnershipIsHeldUntilLastSubscriptionEnds(DataType.Ticks, CancellationToken);

	/// <summary>
	/// A live order book is the better source of the best bid and ask, so while one is running the
	/// connector lets it own those fields and ignores what Level1 says about them. That deal ends
	/// with the book: once the user unsubscribes, Level1 is the only source left, and a best bid
	/// frozen at the last price the book ever showed is not a stale number the user can spot - it
	/// is presented as the current market, and orders are priced off it.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1BestQuotesAreAcceptedAgainAfterTheOrderBookSubscriptionEnds()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);

		using var level1Cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var level1SubId = await SubscribeOnlineAndWait(connector, adapter, DataType.Level1, secId, level1Cts.Token);

		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		var bookTime = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
		var level1Time = bookTime.AddSeconds(1);

		var bookApplied = AsyncHelper.CreateTaskCompletionSource<bool>();
		var level1Applied = AsyncHelper.CreateTaskCompletionSource<bool>();

		// Level1 values are only kept while somebody listens to this event. OpenInterest is sent by
		// the Level1 message only, so it tells the two sources apart.
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			if (changes.Any(p => p.Key == Level1Fields.OpenInterest))
				level1Applied.TrySetResult(true);
			else
				bookApplied.TrySetResult(true);
		};

		var bookSub = new Subscription(DataType.MarketDepth, new Security { Id = secId.ToStringId() });

		var bookOnline = AsyncHelper.CreateTaskCompletionSource<bool>();
		var bookStopped = AsyncHelper.CreateTaskCompletionSource<bool>();

		connector.SubscriptionOnline += s => { if (ReferenceEquals(s, bookSub)) bookOnline.TrySetResult(true); };
		connector.SubscriptionStopped += (s, e) => { if (ReferenceEquals(s, bookSub)) bookStopped.TrySetResult(true); };

		connector.Subscribe(bookSub);
		await bookOnline.Task.WithCancellation(CancellationToken);

		await adapter.SendOrderBook(adapter.LastSubscribedId, secId, bookTime, CancellationToken);
		await bookApplied.Task.WithCancellation(CancellationToken);

		connector.GetSecurityValue(security, Level1Fields.BestBidPrice).AssertEqual((object)99m, "while the book is live it owns the best bid");

		connector.UnSubscribe(bookSub);
		await bookStopped.Task.WithCancellation(CancellationToken);

		// OpenInterest belongs to neither stream, so this update is delivered whatever is done to
		// the best quotes, and the assertions below report the quotes rather than time out.
		await adapter.SendLevel1(level1SubId, secId, level1Time, new Dictionary<Level1Fields, object>
		{
			{ Level1Fields.BestBidPrice, 41m },
			{ Level1Fields.BestAskPrice, 43m },
			{ Level1Fields.OpenInterest, 7m },
		}, CancellationToken);

		await level1Applied.Task.WithCancellation(CancellationToken);

		connector.GetSecurityValue(security, Level1Fields.BestBidPrice).AssertEqual((object)41m, "the book stream has ended, so the best bid must follow Level1 again");
		connector.GetSecurityValue(security, Level1Fields.BestAskPrice).AssertEqual((object)43m, "the book stream has ended, so the best ask must follow Level1 again");

		level1Cts.Cancel();
	}

	/// <summary>
	/// The same deal for the last trade: while a tick stream is running it is the authority on the
	/// last price, so the connector ignores the last-trade fields of Level1. When the user stops
	/// the ticks, Level1 is again the only thing reporting the last price, and a last price stuck
	/// at the final tick of a stream that ended hours ago is shown as the current one.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1LastTradeIsAcceptedAgainAfterTheTickSubscriptionEnds()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);

		using var level1Cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var level1SubId = await SubscribeOnlineAndWait(connector, adapter, DataType.Level1, secId, level1Cts.Token);

		var security = await connector.GetSecurityAsync(secId, CancellationToken);

		var tickTime = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
		var level1Time = tickTime.AddSeconds(1);

		var tickApplied = AsyncHelper.CreateTaskCompletionSource<bool>();
		var level1Applied = AsyncHelper.CreateTaskCompletionSource<bool>();

		// OpenInterest is sent by the Level1 message only, so it tells the two sources apart.
		connector.ValuesChanged += (sec, changes, serverTime, localTime) =>
		{
			if (changes.Any(p => p.Key == Level1Fields.OpenInterest))
				level1Applied.TrySetResult(true);
			else
				tickApplied.TrySetResult(true);
		};

		var tickSub = new Subscription(DataType.Ticks, new Security { Id = secId.ToStringId() });

		var tickOnline = AsyncHelper.CreateTaskCompletionSource<bool>();
		var tickStopped = AsyncHelper.CreateTaskCompletionSource<bool>();

		connector.SubscriptionOnline += s => { if (ReferenceEquals(s, tickSub)) tickOnline.TrySetResult(true); };
		connector.SubscriptionStopped += (s, e) => { if (ReferenceEquals(s, tickSub)) tickStopped.TrySetResult(true); };

		connector.Subscribe(tickSub);
		await tickOnline.Task.WithCancellation(CancellationToken);

		await adapter.SendTick(adapter.LastSubscribedId, secId, tickTime, CancellationToken);
		await tickApplied.Task.WithCancellation(CancellationToken);

		connector.GetSecurityValue(security, Level1Fields.LastTradePrice).AssertEqual((object)42m, "while the ticks are live they own the last price");

		connector.UnSubscribe(tickSub);
		await tickStopped.Task.WithCancellation(CancellationToken);

		await adapter.SendLevel1(level1SubId, secId, level1Time, new Dictionary<Level1Fields, object>
		{
			{ Level1Fields.LastTradePrice, 50m },
			{ Level1Fields.OpenInterest, 7m },
		}, CancellationToken);

		await level1Applied.Task.WithCancellation(CancellationToken);

		connector.GetSecurityValue(security, Level1Fields.LastTradePrice).AssertEqual((object)50m, "the tick stream has ended, so the last price must follow Level1 again");

		level1Cts.Cancel();
	}

	#endregion
}
