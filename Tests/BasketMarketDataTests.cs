namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// MarketData routing, broadcast aggregation, subscribe/unsubscribe tests for BasketMessageAdapter.
/// </summary>
[TestClass]
public class BasketMarketDataTests : BasketTestBase
{
	#region Specific Security → 1 adapter

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_RoutesToCorrectAdapter_BySecurityProvider()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		ClearOut();

		basket.SecurityAdapterProvider.SetAdapter(SecId1, null, adapter1.Id);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		subscriptionRouting.TryGetSubscription(transId, out _, out var adapters, out _)
			.AssertTrue("Subscription should be recorded");

		adapter1.GetMessages<MarketDataMessage>().Any().AssertTrue("Adapter1 should receive MarketDataMessage");
		adapter2.GetMessages<MarketDataMessage>().Any().AssertFalse("Adapter2 should NOT receive MarketDataMessage");

		GetOut<SubscriptionResponseMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionResponseMessage with parent transId");

		GetOut<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionOnlineMessage with parent transId");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_SpecificSecurity_StrictOutput()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		basket.SecurityAdapterProvider.SetAdapter(SecId1, null, adapter1.Id);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		adapter1.GetMessages<MarketDataMessage>().Any().AssertTrue("Adapter1 should receive MarketData");
		adapter2.GetMessages<MarketDataMessage>().Any().AssertFalse("Adapter2 should NOT receive MarketData");

		var childId = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childId });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childId });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == transId).AssertEqual(1);
		GetOut<SubscriptionResponseMessage>()
			.First(r => r.OriginalTransactionId == transId).Error.AssertNull();
		GetOut<SubscriptionOnlineMessage>()
			.Count(m => m.OriginalTransactionId == transId).AssertEqual(1);
		GetOut<SubscriptionFinishedMessage>()
			.Count(m => m.OriginalTransactionId == transId).AssertEqual(0);

		GetOut<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == childId).AssertFalse("Child Response leaked");
		GetOut<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == childId).AssertFalse("Child Online leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_SpecificSecurity_DataHasParentSubscriptionId()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		basket.SecurityAdapterProvider.SetAdapter(SecId1, null, adapter1.Id);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		var childId = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childId });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childId });

		adapter1.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = SecId1,
			TradePrice = 150m,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.SetSubscriptionIds(subscriptionId: childId));

		var ticks = GetOut<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Ticks && e.SecurityId == SecId1).ToArray();
		ticks.Length.AssertEqual(1);
		ticks[0].GetSubscriptionIds()[0].AssertEqual(transId, "Tick should have parent subscription ID");
		ticks[0].GetSubscriptionIds().Any(id => id == childId).AssertFalse("Child ID must not appear");
	}

	#endregion

	#region No Security → all adapters

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_NoSecurity_RoutesToAllAdapters_StrictOutput()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		adapter1.GetMessages<MarketDataMessage>().Any().AssertTrue("Adapter1 should receive MarketData");
		adapter2.GetMessages<MarketDataMessage>().Any().AssertTrue("Adapter2 should receive MarketData");

		var c1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		var c2 = adapter2.GetMessages<MarketDataMessage>().Last().TransactionId;
		c1.AssertNotEqual(transId);
		c2.AssertNotEqual(transId);
		c1.AssertNotEqual(c2);

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		adapter2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c1 });
		adapter2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(transId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 0, expectedOnline: 1);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_NoSecurity_DataFromBothAdapters_AllHaveParentId()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		var c1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		var c2 = adapter2.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		adapter2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c1 });
		adapter2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c2 });

		adapter1.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = SecId1,
			TradePrice = 100m,
			TradeVolume = 5,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.SetSubscriptionIds(subscriptionId: c1));

		adapter2.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = SecId2,
			TradePrice = 200m,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.SetSubscriptionIds(subscriptionId: c2));

		var ticks = GetOut<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		ticks.Length.AssertEqual(2);

		foreach (var tick in ticks)
		{
			var ids = tick.GetSubscriptionIds();
			ids.Length.AssertEqual(1);
			ids[0].AssertEqual(transId, $"Tick {tick.SecurityId} should have parent subscription ID");
		}

		ticks.Any(t => t.GetSubscriptionIds().Contains(c1)).AssertFalse("Child1 ID must not appear");
		ticks.Any(t => t.GetSubscriptionIds().Contains(c2)).AssertFalse("Child2 ID must not appear");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_NoSecurity_OneError_OtherOnline()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		var c1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		var c2 = adapter2.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Not supported"),
		});

		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == transId)
			.AssertFalse("Parent must wait for all children");

		adapter2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		adapter2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(transId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 0, expectedOnline: 1);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_NoSecurity_AllErrors()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		var c1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		var c2 = adapter2.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Adapter1 error"),
		});
		adapter2.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c2,
			Error = new InvalidOperationException("Adapter2 error"),
		});

		AssertBroadcastOutput(transId, [c1, c2],
			expectedResponse: 1, expectError: true,
			expectedFinished: 0, expectedOnline: 0);

		var error = GetOut<SubscriptionResponseMessage>()
			.First(r => r.OriginalTransactionId == transId).Error;
		error.AssertOfType<AggregateException>();
		((AggregateException)error).InnerExceptions.Count.AssertEqual(2);
	}

	#endregion

	#region Broadcast Aggregation (SecurityLookup)

	private async Task<(BasketMessageAdapter basket, TestBasketInnerAdapter a1, TestBasketInnerAdapter a2,
		long parentId, long child1Id, long child2Id)>
		SetupBroadcastLookup()
	{
		var (basket, a1, a2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		a1.AutoRespond = false;
		a2.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = SecId1,
		}, TestContext.CancellationToken);

		var c1 = a1.GetMessages<SecurityLookupMessage>().Last();
		var c2 = a2.GetMessages<SecurityLookupMessage>().Last();

		c1.TransactionId.AssertNotEqual(transId);
		c2.TransactionId.AssertNotEqual(transId);
		c1.TransactionId.AssertNotEqual(c2.TransactionId);

		return (basket, a1, a2, transId, c1.TransactionId, c2.TransactionId);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_BothSucceed_Finished()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		a1.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(parentId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 1, expectedOnline: 0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_BothSucceed_Online()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		a1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(parentId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 0, expectedOnline: 1);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_OneSubscriptionError_OtherFinished()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Securities not available"),
		});

		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == parentId)
			.AssertFalse("Parent should wait for all children");

		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		a2.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(parentId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 1, expectedOnline: 0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_OneSubscriptionError_OtherOnline()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Adapter1 failed"),
		});

		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });
		a2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = c2 });

		AssertBroadcastOutput(parentId, [c1, c2],
			expectedResponse: 1, expectError: false,
			expectedFinished: 0, expectedOnline: 1);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_AllErrors()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Adapter1 failed"),
		});

		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == parentId)
			.AssertFalse("Parent should wait for all children");

		a2.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c2,
			Error = new InvalidOperationException("Adapter2 failed"),
		});

		AssertBroadcastOutput(parentId, [c1, c2],
			expectedResponse: 1, expectError: true,
			expectedFinished: 0, expectedOnline: 0);

		var error = GetOut<SubscriptionResponseMessage>()
			.First(r => r.OriginalTransactionId == parentId).Error;
		error.AssertOfType<AggregateException>();
		((AggregateException)error).InnerExceptions.Count.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_OneConnectionError_OnlyLiveAdapterQueried()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.EmitOut(new ConnectMessage());
		adapter2.EmitOut(new ConnectMessage { Error = new Exception("Connection refused") });

		connectionState.ConnectedCount.AssertEqual(1);

		adapter1.AutoRespond = true;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = SecId1,
		}, TestContext.CancellationToken);

		adapter1.GetMessages<SecurityLookupMessage>().Any().AssertTrue("Connected adapter should receive SecurityLookup");
		adapter2.GetMessages<SecurityLookupMessage>().Any().AssertFalse("Failed adapter should NOT receive SecurityLookup");

		var childId = adapter1.GetMessages<SecurityLookupMessage>().Last().TransactionId;

		AssertBroadcastOutput(transId, [childId],
			expectedResponse: 1, expectError: false,
			expectedFinished: 1, expectedOnline: 0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_ResponseComesOnlyAfterAllChildrenRespond()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == parentId)
			.AssertEqual(0, "Parent response must wait for all children");

		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == parentId)
			.AssertEqual(1, "Exactly 1 parent response after all children responded");

		GetOut<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == c1).AssertFalse("Child1 leaked");
		GetOut<SubscriptionResponseMessage>().Any(r => r.OriginalTransactionId == c2).AssertFalse("Child2 leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_ThreeAdapters_TwoErrors_OneFinished()
	{
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			new AdapterConnectionState(),
			new AdapterConnectionManager(new AdapterConnectionState()),
			new PendingMessageState(),
			new SubscriptionRoutingState(),
			new ParentChildMap(),
			new OrderRoutingState(),
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen, candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var a1 = new TestBasketInnerAdapter(idGen);
		var a2 = new TestBasketInnerAdapter(idGen);
		var a3 = new TestBasketInnerAdapter(idGen);
		basket.InnerAdapters.Add(a1);
		basket.InnerAdapters.Add(a2);
		basket.InnerAdapters.Add(a3);
		basket.ApplyHeartbeat(a1, false);
		basket.ApplyHeartbeat(a2, false);
		basket.ApplyHeartbeat(a3, false);

		OutMessages = new ConcurrentQueue<Message>();
		basket.NewOutMessageAsync += (msg, ct) =>
		{
			OutMessages.Enqueue(msg);
			return default;
		};

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		a1.AutoRespond = false;
		a2.AutoRespond = false;
		a3.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = SecId1,
		}, TestContext.CancellationToken);

		var c1 = a1.GetMessages<SecurityLookupMessage>().Last().TransactionId;
		var c2 = a2.GetMessages<SecurityLookupMessage>().Last().TransactionId;
		var c3 = a3.GetMessages<SecurityLookupMessage>().Last().TransactionId;

		a1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Adapter1 failed"),
		});
		a2.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c2,
			Error = new InvalidOperationException("Adapter2 failed"),
		});

		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == transId)
			.AssertFalse("Parent should wait for adapter3");

		a3.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c3 });
		a3.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c3 });

		AssertBroadcastOutput(transId, [c1, c2, c3],
			expectedResponse: 1, expectError: false,
			expectedFinished: 1, expectedOnline: 0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_ThreeAdapters_AllErrors()
	{
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			new AdapterConnectionState(),
			new AdapterConnectionManager(new AdapterConnectionState()),
			new PendingMessageState(),
			new SubscriptionRoutingState(),
			new ParentChildMap(),
			new OrderRoutingState(),
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen, candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var a1 = new TestBasketInnerAdapter(idGen);
		var a2 = new TestBasketInnerAdapter(idGen);
		var a3 = new TestBasketInnerAdapter(idGen);
		basket.InnerAdapters.Add(a1);
		basket.InnerAdapters.Add(a2);
		basket.InnerAdapters.Add(a3);
		basket.ApplyHeartbeat(a1, false);
		basket.ApplyHeartbeat(a2, false);
		basket.ApplyHeartbeat(a3, false);

		OutMessages = new ConcurrentQueue<Message>();
		basket.NewOutMessageAsync += (msg, ct) =>
		{
			OutMessages.Enqueue(msg);
			return default;
		};

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		a1.AutoRespond = false;
		a2.AutoRespond = false;
		a3.AutoRespond = false;
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = SecId1,
		}, TestContext.CancellationToken);

		var c1 = a1.GetMessages<SecurityLookupMessage>().Last().TransactionId;
		var c2 = a2.GetMessages<SecurityLookupMessage>().Last().TransactionId;
		var c3 = a3.GetMessages<SecurityLookupMessage>().Last().TransactionId;

		a1.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c1,
			Error = new InvalidOperationException("Adapter1 failed"),
		});
		a2.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c2,
			Error = new InvalidOperationException("Adapter2 failed"),
		});
		a3.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = c3,
			Error = new InvalidOperationException("Adapter3 failed"),
		});

		AssertBroadcastOutput(transId, [c1, c2, c3],
			expectedResponse: 1, expectError: true,
			expectedFinished: 0, expectedOnline: 0);

		var aggEx = (AggregateException)GetOut<SubscriptionResponseMessage>()
			.First(r => r.OriginalTransactionId == transId).Error;
		aggEx.InnerExceptions.Count.AssertEqual(3);
	}

	#endregion

	#region OriginalTransactionId Remapping

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_SecurityMessage_RemapsChildToParentSubscriptionId()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		a1.EmitOut(new SecurityMessage
		{
			SecurityId = SecId1,
			OriginalTransactionId = c1,
		}.SetSubscriptionIds(subscriptionId: c1));

		a2.EmitOut(new SecurityMessage
		{
			SecurityId = SecId2,
			OriginalTransactionId = c2,
		}.SetSubscriptionIds(subscriptionId: c2));

		var securities = GetOut<SecurityMessage>().ToArray();
		securities.Length.AssertEqual(2);

		foreach (var sec in securities)
		{
			var ids = sec.GetSubscriptionIds();
			ids.Length.AssertEqual(1);
			ids[0].AssertEqual(parentId, $"SecurityMessage {sec.SecurityId} should have parent subscription ID");
			sec.OriginalTransactionId.AssertEqual(parentId, $"SecurityMessage {sec.SecurityId} OriginalTransactionId should be parent");
		}

		securities.Any(s => s.GetSubscriptionIds().Contains(c1)).AssertFalse("Child1 ID leaked");
		securities.Any(s => s.GetSubscriptionIds().Contains(c2)).AssertFalse("Child2 ID leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_ExecutionMessage_RemapsChildToParentSubscriptionId()
	{
		var (basket, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		a1.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = SecId1,
			OriginalTransactionId = c1,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			TransactionId = basket.TransactionIdGenerator.GetNextId(),
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.SetSubscriptionIds(subscriptionId: c1));

		var execs = GetOut<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Transactions).ToArray();
		execs.Length.AssertEqual(1);

		var ids = execs[0].GetSubscriptionIds();
		ids.Length.AssertEqual(1);
		ids[0].AssertEqual(parentId, "ExecutionMessage should have parent subscription ID");
		execs[0].OriginalTransactionId.AssertEqual(parentId, "ExecutionMessage OriginalTransactionId should be parent");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_PortfolioMessage_RemapsChildToParentSubscriptionId()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		a1.EmitOut(new PortfolioMessage
		{
			PortfolioName = Portfolio1,
			OriginalTransactionId = c1,
		}.SetSubscriptionIds(subscriptionId: c1));

		var portfolios = GetOut<PortfolioMessage>().ToArray();
		portfolios.Length.AssertEqual(1);

		var ids = portfolios[0].GetSubscriptionIds();
		ids.Length.AssertEqual(1);
		ids[0].AssertEqual(parentId, "PortfolioMessage should have parent subscription ID");
		portfolios[0].OriginalTransactionId.AssertEqual(parentId);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_MultipleSubscriptionIds_AllRemapped()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		a1.EmitOut(new SecurityMessage
		{
			SecurityId = SecId1,
			OriginalTransactionId = c1,
			SubscriptionIds = [c1, c2],
		});

		var securities = GetOut<SecurityMessage>().ToArray();
		securities.Length.AssertEqual(1);

		var ids = securities[0].GetSubscriptionIds();
		ids.Any(id => id == c1).AssertFalse("Child1 ID should be remapped");
		ids.Any(id => id == c2).AssertFalse("Child2 ID should be remapped");
		ids.All(id => id == parentId).AssertTrue("All IDs should be parent ID");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Broadcast_FinishedMessages_AlsoRemapped()
	{
		var (_, a1, a2, parentId, c1, c2) = await SetupBroadcastLookup();

		a1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = c2 });

		a1.EmitOut(new SecurityMessage
		{
			SecurityId = SecId1,
			OriginalTransactionId = c1,
		}.SetSubscriptionIds(subscriptionId: c1));

		a1.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c1 });
		a2.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = c2 });

		var sec = GetOut<SecurityMessage>().Single();
		sec.GetSubscriptionIds()[0].AssertEqual(parentId);

		GetOut<SubscriptionFinishedMessage>()
			.Count(m => m.OriginalTransactionId == parentId).AssertEqual(1);
		GetOut<SubscriptionFinishedMessage>()
			.Any(m => m.OriginalTransactionId == c1).AssertFalse("Child1 Finished leaked");
		GetOut<SubscriptionFinishedMessage>()
			.Any(m => m.OriginalTransactionId == c2).AssertFalse("Child2 Finished leaked");
	}

	#endregion

	#region Subscribe / Unsubscribe cycles

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Unsubscribe_SingleAdapter_ExactlyOneResponseWithUnsubscribeId()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		adapter1.AutoRespond = false;
		ClearOut();

		// Subscribe
		var subId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subId,
		}, TestContext.CancellationToken);

		var childSubId = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSubId });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSubId });
		ClearOut();

		// Unsubscribe
		var unsubId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsubId,
			OriginalTransactionId = subId,
		}, TestContext.CancellationToken);

		var childUnsubId = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => !m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childUnsubId });

		// exactly 1 response with UNSUB transaction ID
		var responses = GetOut<SubscriptionResponseMessage>().ToArray();
		responses.Count(r => r.OriginalTransactionId == unsubId).AssertEqual(1,
			"Exactly 1 response with unsubscribe transaction ID");

		// no response with subscribe ID
		responses.Any(r => r.OriginalTransactionId == subId).AssertFalse(
			"No response with subscribe ID on unsubscribe");

		// no child IDs
		responses.Any(r => r.OriginalTransactionId == childSubId).AssertFalse("Child subscribe ID leaked");
		responses.Any(r => r.OriginalTransactionId == childUnsubId).AssertFalse("Child unsubscribe ID leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SubscribeUnsubscribeSubscribe_AllIdsCorrect()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		adapter1.AutoRespond = false;
		ClearOut();

		// --- Subscribe #1 ---
		var sub1Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = sub1Id,
		}, TestContext.CancellationToken);

		var childSub1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub1 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub1 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == sub1Id).AssertEqual(1);
		GetOut<SubscriptionOnlineMessage>()
			.Count(r => r.OriginalTransactionId == sub1Id).AssertEqual(1);
		ClearOut();

		// --- Unsubscribe #1 ---
		var unsub1Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsub1Id,
			OriginalTransactionId = sub1Id,
		}, TestContext.CancellationToken);

		var childUnsub1 = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => !m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childUnsub1 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == unsub1Id).AssertEqual(1,
			"Exactly 1 response for unsubscribe #1");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == sub1Id).AssertFalse(
			"Subscribe #1 ID must not appear in unsubscribe response");
		ClearOut();

		// --- Subscribe #2 (re-subscribe) ---
		var sub2Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = sub2Id,
		}, TestContext.CancellationToken);

		var childSub2 = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub2 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub2 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == sub2Id).AssertEqual(1,
			"Exactly 1 response for subscribe #2");
		GetOut<SubscriptionOnlineMessage>()
			.Count(r => r.OriginalTransactionId == sub2Id).AssertEqual(1,
			"Exactly 1 online for subscribe #2");

		// no old IDs leak
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == sub1Id).AssertFalse("Old subscribe #1 ID leaked");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == unsub1Id).AssertFalse("Old unsubscribe #1 ID leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task DoubleSubscribe_ThenUnsubscribeBoth_EachUnsubHasOwnResponse()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		adapter1.AutoRespond = false;
		ClearOut();

		// --- Subscribe #1 (Ticks) ---
		var sub1Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = sub1Id,
		}, TestContext.CancellationToken);

		var childSub1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub1 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub1 });

		// --- Subscribe #2 (Level1) ---
		var sub2Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = sub2Id,
		}, TestContext.CancellationToken);

		var childSub2 = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub2 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub2 });
		ClearOut();

		// --- Unsubscribe #1 (Ticks) ---
		var unsub1Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsub1Id,
			OriginalTransactionId = sub1Id,
		}, TestContext.CancellationToken);

		var childUnsub1 = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => !m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childUnsub1 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == unsub1Id).AssertEqual(1,
			"Exactly 1 response for unsubscribe Ticks");

		// --- Unsubscribe #2 (Level1) ---
		var unsub2Id = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = false,
			TransactionId = unsub2Id,
			OriginalTransactionId = sub2Id,
		}, TestContext.CancellationToken);

		var childUnsub2 = adapter1.GetMessages<MarketDataMessage>()
			.Last(m => !m.IsSubscribe).TransactionId;
		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childUnsub2 });

		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == unsub2Id).AssertEqual(1,
			"Exactly 1 response for unsubscribe Level1");

		// no cross-contamination
		var allResponses = GetOut<SubscriptionResponseMessage>();
		allResponses.Any(r => r.OriginalTransactionId == sub1Id).AssertFalse("Sub1 ID leaked in unsub phase");
		allResponses.Any(r => r.OriginalTransactionId == sub2Id).AssertFalse("Sub2 ID leaked in unsub phase");
		allResponses.Any(r => r.OriginalTransactionId == childSub1).AssertFalse("Child sub1 leaked");
		allResponses.Any(r => r.OriginalTransactionId == childSub2).AssertFalse("Child sub2 leaked");
		allResponses.Any(r => r.OriginalTransactionId == childUnsub1).AssertFalse("Child unsub1 leaked");
		allResponses.Any(r => r.OriginalTransactionId == childUnsub2).AssertFalse("Child unsub2 leaked");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Unsubscribe_TwoAdapters_Broadcast_ExactlyOneUnsubResponse()
	{
		var (basket, adapter1, adapter2) = CreateBasket();
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;
		ClearOut();

		// Subscribe (broadcast — no security)
		var subId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subId,
		}, TestContext.CancellationToken);

		var childSub1 = adapter1.GetMessages<MarketDataMessage>().Last().TransactionId;
		var childSub2 = adapter2.GetMessages<MarketDataMessage>().Last().TransactionId;

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub1 });
		adapter2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = childSub2 });
		adapter1.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub1 });
		adapter2.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = childSub2 });
		ClearOut();

		// Unsubscribe (broadcast)
		var unsubId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsubId,
			OriginalTransactionId = subId,
		}, TestContext.CancellationToken);

		// both adapters should get unsub child messages
		var unsub1 = adapter1.GetMessages<MarketDataMessage>().Last(m => !m.IsSubscribe);
		var unsub2 = adapter2.GetMessages<MarketDataMessage>().Last(m => !m.IsSubscribe);

		adapter1.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = unsub1.TransactionId });
		adapter2.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = unsub2.TransactionId });

		// exactly 1 parent unsub response
		GetOut<SubscriptionResponseMessage>()
			.Count(r => r.OriginalTransactionId == unsubId).AssertEqual(1,
			"Exactly 1 parent unsubscribe response");

		// no sub ID, no child IDs
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == subId).AssertFalse("Subscribe ID leaked");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == childSub1).AssertFalse("Child sub1 leaked");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == childSub2).AssertFalse("Child sub2 leaked");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == unsub1.TransactionId).AssertFalse("Child unsub1 leaked");
		GetOut<SubscriptionResponseMessage>()
			.Any(r => r.OriginalTransactionId == unsub2.TransactionId).AssertFalse("Child unsub2 leaked");
	}

	#endregion
}
