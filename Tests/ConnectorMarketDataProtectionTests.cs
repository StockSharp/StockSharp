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
				msg.OriginalTransactionId = subscriptionId;

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
				msg.OriginalTransactionId = subscriptionId;

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
				msg.OriginalTransactionId = subscriptionId;

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

	#endregion

	#region Level1 — ValuesChanged

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_NoSubscription_ShouldNotTriggerValuesChanged()
	{
		var (connector, adapter) = CreateConnector();
		var secId = Helper.CreateSecurityId();

		await connector.ConnectAsync(CancellationToken);
		connector.GetSecurity(secId);

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

		connector.GetSecurity(secId);

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
		var security = connector.GetSecurity(secId);

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

		var security = connector.GetSecurity(secId);

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
		connector.GetSecurity(secId);

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
		var security = connector.GetSecurity(secId);

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

		var security = connector.GetSecurity(secId);

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
		connector.GetSecurity(secId);

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
		var security = connector.GetSecurity(secId);

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

		var security = connector.GetSecurity(secId);

		await adapter.SendOrderBook(subscriptionId, secId, DateTime.UtcNow.AddDays(-1), CancellationToken);
		await Task.Delay(500, CancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		security.BestBid.AssertNull("Security.BestBid should not be set by Count-based historical order book");
		security.BestAsk.AssertNull("Security.BestAsk should not be set by Count-based historical order book");
#pragma warning restore CS0618 // Type or member is obsolete

		runCts.Cancel();
	}

	#endregion
}
