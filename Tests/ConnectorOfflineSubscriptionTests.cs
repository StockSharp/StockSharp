namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Tests for subscription handling around connect/disconnect/offline scenarios.
/// Verifies that BasketMessageAdapter correctly pends subscriptions before connect,
/// and OfflineMessageAdapter correctly buffers during connection loss.
/// </summary>
[TestClass]
public class ConnectorOfflineSubscriptionTests : BaseTestClass
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

	private sealed class MockAdapter : MessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public ConcurrentQueue<MarketDataMessage> RecordedSubscriptions { get; } = [];
		public ConcurrentQueue<MarketDataMessage> RecordedUnsubscriptions { get; } = [];
		public Dictionary<long, MarketDataMessage> ActiveSubscriptions { get; } = [];

		public override bool UseInChannel => false;
		public override bool UseOutChannel => false;

		public MockAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.Ticks);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || dataType == DataType.Transactions;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Enqueue(message);

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
						RecordedSubscriptions.Enqueue(mdMsg.TypedClone());
						ActiveSubscriptions[mdMsg.TransactionId] = mdMsg;
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						await SendSubscriptionResultAsync(mdMsg, cancellationToken);
					}
					else
					{
						RecordedUnsubscriptions.Enqueue(mdMsg.TypedClone());
						ActiveSubscriptions.Remove(mdMsg.OriginalTransactionId);
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var slm = (SecurityLookupMessage)message;
					await SendOutMessageAsync(slm.CreateResponse(), cancellationToken);
					await SendOutMessageAsync(slm.CreateResult(), cancellationToken);
					break;
				}
				case MessageTypes.PortfolioLookup:
				{
					var plm = (PortfolioLookupMessage)message;
					await SendOutMessageAsync(plm.CreateResponse(), cancellationToken);
					await SendOutMessageAsync(plm.CreateResult(), cancellationToken);
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var osm = (OrderStatusMessage)message;
					if (osm.IsSubscribe)
					{
						await SendOutMessageAsync(osm.CreateResponse(), cancellationToken);
						await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = osm.TransactionId }, cancellationToken);
					}
					break;
				}
				case MessageTypes.Reset:
				{
					ActiveSubscriptions.Clear();
					RecordedSubscriptions.Clear();
					RecordedUnsubscriptions.Clear();
					break;
				}
			}
		}

		public async ValueTask SimulateConnectionLost(CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new ConnectionLostMessage(), cancellationToken);
		}

		public async ValueTask SimulateConnectionRestored(CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new ConnectionRestoredMessage(), cancellationToken);
		}
	}

	private sealed class BasketState
	{
		public AdapterConnectionState ConnectionState { get; } = new();
		public AdapterConnectionManager ConnectionManager { get; }
		public SubscriptionRoutingState SubscriptionRouting { get; } = new();
		public ParentChildMap ParentChildMap { get; } = new();
		public PendingMessageState PendingState { get; } = new();
		public OrderRoutingState OrderRouting { get; } = new();

		public BasketState()
		{
			ConnectionManager = new AdapterConnectionManager(ConnectionState);
		}
	}

	private static (TestConnector connector, MockAdapter adapter, BasketState state) CreateSingleAdapter(bool supportOffline = false)
	{
		var state = new BasketState();
		var idGen = new MillisecondIncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			state.ConnectionState,
			state.ConnectionManager,
			state.PendingState,
			state.SubscriptionRouting,
			state.ParentChildMap,
			state.OrderRouting,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager)
		{
			LatencyManager = null,
			SlippageManager = null,
			CommissionManager = null,
			SupportOffline = supportOffline,
		};

		var connector = new TestConnector(basket);
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		return (connector, adapter, state);
	}

	private static (TestConnector connector, MockAdapter adapter1, MockAdapter adapter2, BasketState state) CreateTwoAdapters()
	{
		var state = new BasketState();
		var idGen = new MillisecondIncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			state.ConnectionState,
			state.ConnectionManager,
			state.PendingState,
			state.SubscriptionRouting,
			state.ParentChildMap,
			state.OrderRouting,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager)
		{
			LatencyManager = null,
			SlippageManager = null,
			CommissionManager = null,
		};

		var connector = new TestConnector(basket);

		var adapter1 = new MockAdapter(connector.TransactionIdGenerator);
		var adapter2 = new MockAdapter(connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(adapter1);
		connector.Adapter.InnerAdapters.Add(adapter2);

		return (connector, adapter1, adapter2, state);
	}

	#endregion

	#region 1. Subscribe specific instrument before connect

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SubscribeBeforeConnect_SpecificInstrument_ShouldPendThenDeliver()
	{
		var (connector, adapter, state) = CreateSingleAdapter();

		var secId = Helper.CreateSecurityId();
		var sub = new Subscription(DataType.Level1, new Security { Id = secId.ToStringId() });

		// Subscribe BEFORE connect — should be pended in BasketMessageAdapter
		_ = connector.SubscribeAsync(sub, CancellationToken).AsTask();

		state.PendingState.Count.AssertGreater(0,
			"Subscription should be pended when no adapter is connected");
		adapter.RecordedSubscriptions.Count.AssertEqual(0,
			"Mock adapter should NOT receive subscription before connect");

		// Now connect
		await connector.ConnectAsync(CancellationToken);

		// After connect, pending subscriptions should be replayed to the adapter
		adapter.RecordedSubscriptions.Count.AssertGreater(0,
			"Mock adapter should receive the pending subscription after connect");

		// Verify the subscription is for our security
		adapter.RecordedSubscriptions.TryDequeue(out var received);
		received.AssertNotNull();
		received.SecurityId.AssertEqual(secId,
			"Delivered subscription should be for the originally requested security");
		received.DataType2.AssertEqual(DataType.Level1,
			"Delivered subscription should be Level1");

		state.PendingState.Count.AssertEqual(0,
			"No pending messages should remain after connect");
	}

	#endregion

	#region 2. Subscribe ALL instruments before connect with two adapters

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SubscribeBeforeConnect_AllInstruments_TwoAdapters_BothShouldReceive()
	{
		var (connector, adapter1, adapter2, state) = CreateTwoAdapters();

		var sub = new Subscription(DataType.Level1);

		// Subscribe ALL before connect
		_ = connector.SubscribeAsync(sub, CancellationToken).AsTask();

		state.PendingState.Count.AssertGreater(0,
			"Subscription should be pended when no adapters are connected");
		adapter1.RecordedSubscriptions.Count.AssertEqual(0,
			"Adapter1 should NOT receive subscription before connect");
		adapter2.RecordedSubscriptions.Count.AssertEqual(0,
			"Adapter2 should NOT receive subscription before connect");

		// Now connect — both adapters will process ConnectMessage
		await connector.ConnectAsync(CancellationToken);

		// Both adapters should receive the subscription (ALL = broadcast)
		adapter1.RecordedSubscriptions.Count.AssertGreater(0,
			"Adapter1 should receive the pending subscription after connect");
		adapter2.RecordedSubscriptions.Count.AssertGreater(0,
			"Adapter2 should receive the pending subscription after connect");

		state.PendingState.Count.AssertEqual(0,
			"No pending messages should remain after connect");
	}

	#endregion

	#region 3. Offline: subscribe during connection loss, delivered after restore

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SubscribeDuringConnectionLoss_ShouldBufferOffline_ThenDeliverOnRestore()
	{
		var (connector, adapter, state) = CreateSingleAdapter(supportOffline: true);

		// Connect first
		await connector.ConnectAsync(CancellationToken);

		adapter.RecordedSubscriptions.Clear();

		// Simulate connection loss
		await adapter.SimulateConnectionLost(CancellationToken);

		var secId = Helper.CreateSecurityId();
		var sub = new Subscription(DataType.Level1, new Security { Id = secId.ToStringId() });

		// Subscribe while disconnected — OfflineMessageAdapter should buffer
		_ = connector.SubscribeAsync(sub, CancellationToken).AsTask();

		adapter.RecordedSubscriptions.Count.AssertEqual(0,
			"Mock adapter should NOT receive subscription during connection loss");

		// Restore connection
		await adapter.SimulateConnectionRestored(CancellationToken);

		// After restoration, offline-buffered subscription should reach the adapter
		adapter.RecordedSubscriptions.Count.AssertGreater(0,
			"Mock adapter should receive the buffered subscription after connection restore");

		adapter.RecordedSubscriptions.TryDequeue(out var received);
		received.AssertNotNull();
		received.SecurityId.AssertEqual(secId,
			"Restored subscription should be for the originally requested security");
	}

	#endregion

	#region 4. Unsubscribe during connection loss

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task UnsubscribeDuringConnectionLoss_ShouldRemoveSubscription()
	{
		var (connector, adapter, state) = CreateSingleAdapter(supportOffline: true);

		// Connect and subscribe
		await connector.ConnectAsync(CancellationToken);

		var secId = Helper.CreateSecurityId();
		var sub = new Subscription(DataType.Level1, new Security { Id = secId.ToStringId() });

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };
		_ = connector.SubscribeAsync(sub, CancellationToken).AsTask();
		await started.Task.WithCancellation(CancellationToken);

		adapter.RecordedSubscriptions.Count.AssertGreater(0,
			"Adapter should have received the subscription");

		adapter.RecordedSubscriptions.Clear();
		adapter.RecordedUnsubscriptions.Clear();

		// Simulate connection loss
		await adapter.SimulateConnectionLost(CancellationToken);

		// Unsubscribe while disconnected
		connector.UnSubscribe(sub);

		// The unsubscribe should be processed locally (offline adapter removes the pending sub)
		// When connection restores, the adapter should receive an unsubscribe request
		await adapter.SimulateConnectionRestored(CancellationToken);

		// After restore we expect the subscription NOT to be re-subscribed,
		// or an explicit unsubscribe to be sent
		var reSubscribed = adapter.RecordedSubscriptions.Count;
		var unsubscribed = adapter.RecordedUnsubscriptions.Count;

		// The subscription should effectively be gone:
		// either no re-subscribe happened, or an unsubscribe was sent
		IsTrue(reSubscribed == 0 || unsubscribed > 0,
			$"After unsubscribe during offline + restore: reSubscribed={reSubscribed}, unsubscribed={unsubscribed}. " +
			"Expected either no re-subscribe or an explicit unsubscribe.");
	}

	#endregion
}
