namespace StockSharp.Tests;

/// <summary>
/// Integration tests for SecurityAll subscription flow through the full Connector adapter chain.
/// </summary>
[TestClass]
public class SecurityAllSubscriptionTests : BaseTestClass
{
	#region Infrastructure

	private static readonly SecurityId AaplId = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId GoogId = "GOOG@NASDAQ".ToSecurityId();
	private static readonly SecurityId MsftId = "MSFT@NASDAQ".ToSecurityId();

	private sealed class TestConnector : Connector
	{
		public TestConnector()
			: base(new InMemorySecurityStorage(), new InMemoryPositionStorage(),
				   new InMemoryExchangeInfoProvider(), initChannels: false)
		{
			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();
		}
	}

	private sealed class SecurityAllTestAdapter : MessageAdapter
	{
		private readonly bool _canFilterBySecurity;
		private readonly Dictionary<long, MarketDataMessage> _activeSubscriptions = [];

		public SecurityAllTestAdapter(IdGenerator transactionIdGenerator, bool canFilterBySecurity = true, bool supportCandles = false)
			: base(transactionIdGenerator)
		{
			_canFilterBySecurity = canFilterBySecurity;

			this.AddMarketDataSupport();
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.Level1);

			if (supportCandles)
				this.AddSupportedMarketDataType(TimeSpan.FromMinutes(1).TimeFrame());
		}

		public Dictionary<long, MarketDataMessage> ActiveSubscriptions => _activeSubscriptions;

		public override bool IsSecurityRequired(DataType dataType)
			=> _canFilterBySecurity && base.IsSecurityRequired(dataType);

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities;

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
						_activeSubscriptions[mdMsg.TransactionId] = mdMsg.TypedClone();
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						if (mdMsg.To == null)
							await SendSubscriptionResultAsync(mdMsg, cancellationToken);
					}
					else
					{
						_activeSubscriptions.Remove(mdMsg.OriginalTransactionId);
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;
					await SendOutMessageAsync(sl.CreateResponse(), cancellationToken);
					await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId }, cancellationToken);
					break;
				}
				case MessageTypes.Reset:
					_activeSubscriptions.Clear();
					break;
			}
		}

		public async ValueTask EmitTick(long subscriptionId, SecurityId secId, decimal price, decimal volume, DateTime serverTime, CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradePrice = price,
				TradeVolume = volume,
				ServerTime = serverTime,
				OriginalTransactionId = subscriptionId,
			}, cancellationToken);
		}

		public async ValueTask EmitLevel1(long subscriptionId, SecurityId secId, decimal lastPrice, DateTime serverTime, CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = serverTime,
				OriginalTransactionId = subscriptionId,
			}
			.TryAdd(Level1Fields.LastTradePrice, lastPrice), cancellationToken);
		}
	}

	private static (TestConnector connector, SecurityAllTestAdapter adapter) CreateConnector(bool canFilterBySecurity = true, bool supportCandles = false)
	{
		var connector = new TestConnector();

		connector.Adapter.LatencyManager = null;
		connector.Adapter.SlippageManager = null;
		connector.Adapter.CommissionManager = null;
		connector.Adapter.SendFinishedCandlesImmediatelly = true;

		var adapter = new SecurityAllTestAdapter(connector.TransactionIdGenerator, canFilterBySecurity, supportCandles);
		connector.Adapter.InnerAdapters.Add(adapter);

		return (connector, adapter);
	}

	private static async Task<long> WaitForSubscription(SecurityAllTestAdapter adapter, CancellationToken ct, int timeoutMs = 5000)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeoutMs);

		while (adapter.ActiveSubscriptions.Count == 0)
			await Task.Delay(10, cts.Token);

		await Task.Delay(50, ct);
		return adapter.ActiveSubscriptions.Keys.First();
	}

	/// <summary>
	/// Emit a "warm-up" message for a security to trigger SecurityAll child loopback,
	/// then wait for child to go online.
	/// </summary>
	private static async Task WarmUpSecurity(SecurityAllTestAdapter adapter, long subId, SecurityId secId,
		DataType dataType, DateTime serverTime, CancellationToken ct)
	{
		if (dataType == DataType.Ticks)
			await adapter.EmitTick(subId, secId, 1m, 1, serverTime, ct);
		else if (dataType == DataType.Level1)
			await adapter.EmitLevel1(subId, secId, 1m, serverTime, ct);

		// wait for loopback child subscription to complete
		await Task.Delay(200, ct);
	}

	#endregion

	#region Test 1: Subscribe all ticks, receive multiple securities

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityAll_SubscribeAllTicks_ReceivesMultipleSecurities()
	{
		var (connector, adapter) = CreateConnector(canFilterBySecurity: true);
		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Ticks);
		var receivedTicks = new List<(Subscription sub, ITickTradeMessage tick)>();
		connector.TickTradeReceived += (s, t) => receivedTicks.Add((s, t));

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();
		await started.Task.WithCancellation(CancellationToken);

		var subId = await WaitForSubscription(adapter, CancellationToken);
		var now = DateTime.UtcNow;

		// warm up children for both securities (triggers loopback)
		await WarmUpSecurity(adapter, subId, AaplId, DataType.Ticks, now, CancellationToken);
		await WarmUpSecurity(adapter, subId, GoogId, DataType.Ticks, now.AddMilliseconds(1), CancellationToken);

		receivedTicks.Clear();

		// now emit real data — children are online
		await adapter.EmitTick(subId, AaplId, 150m, 10, now.AddSeconds(1), CancellationToken);
		await adapter.EmitTick(subId, GoogId, 2800m, 5, now.AddSeconds(2), CancellationToken);
		await Task.Delay(200, CancellationToken);

		IsTrue(receivedTicks.Count >= 2, $"Expected at least 2 ticks, got {receivedTicks.Count}");
		IsTrue(receivedTicks.Any(t => ((ISecurityIdMessage)t.tick).SecurityId == AaplId), "Expected AAPL tick");
		IsTrue(receivedTicks.Any(t => ((ISecurityIdMessage)t.tick).SecurityId == GoogId), "Expected GOOG tick");

		runCts.Cancel();
		await run.WithCancellation(CancellationToken);
	}

	#endregion

	#region Test 2: Specific security, adapter can't filter, receives only requested

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityAll_SpecificSecurity_AdapterCantFilter_ReceivesOnlyRequested()
	{
		var (connector, adapter) = CreateConnector(canFilterBySecurity: false);
		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Ticks, new Security { Id = AaplId.ToStringId() });
		var receivedTicks = new List<(Subscription sub, ITickTradeMessage tick)>();
		connector.TickTradeReceived += (s, t) => receivedTicks.Add((s, t));

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();
		await started.Task.WithCancellation(CancellationToken);

		var subId = await WaitForSubscription(adapter, CancellationToken);
		var now = DateTime.UtcNow;

		// warm up all 3 securities
		await WarmUpSecurity(adapter, subId, AaplId, DataType.Ticks, now, CancellationToken);
		await WarmUpSecurity(adapter, subId, GoogId, DataType.Ticks, now.AddMilliseconds(1), CancellationToken);
		await WarmUpSecurity(adapter, subId, MsftId, DataType.Ticks, now.AddMilliseconds(2), CancellationToken);

		receivedTicks.Clear();

		// emit ticks for all 3, but only AAPL should pass through NonAlls filtering
		await adapter.EmitTick(subId, AaplId, 150m, 10, now.AddSeconds(1), CancellationToken);
		await adapter.EmitTick(subId, GoogId, 2800m, 5, now.AddSeconds(2), CancellationToken);
		await adapter.EmitTick(subId, MsftId, 300m, 7, now.AddSeconds(3), CancellationToken);
		await Task.Delay(200, CancellationToken);

		var aaplCount = receivedTicks.Count(t => ((ISecurityIdMessage)t.tick).SecurityId == AaplId);
		var googCount = receivedTicks.Count(t => ((ISecurityIdMessage)t.tick).SecurityId == GoogId);
		var msftCount = receivedTicks.Count(t => ((ISecurityIdMessage)t.tick).SecurityId == MsftId);

		IsTrue(aaplCount >= 1, $"Expected AAPL tick, got {aaplCount}");
		AreEqual(0, googCount, "GOOG ticks should be filtered out");
		AreEqual(0, msftCount, "MSFT ticks should be filtered out");

		runCts.Cancel();
		await run.WithCancellation(CancellationToken);
	}

	#endregion

	#region Test 3: Subscribe all Level1, data flows correctly

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityAll_SubscribeAllLevel1_DataFlowsCorrectly()
	{
		var (connector, adapter) = CreateConnector(canFilterBySecurity: true);
		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1);
		var receivedL1 = new List<(Subscription sub, Level1ChangeMessage msg)>();
		connector.Level1Received += (s, m) => receivedL1.Add((s, m));

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();
		await started.Task.WithCancellation(CancellationToken);

		var subId = await WaitForSubscription(adapter, CancellationToken);
		var now = DateTime.UtcNow;

		// warm up children
		await WarmUpSecurity(adapter, subId, AaplId, DataType.Level1, now, CancellationToken);
		await WarmUpSecurity(adapter, subId, GoogId, DataType.Level1, now.AddMilliseconds(1), CancellationToken);

		receivedL1.Clear();

		await adapter.EmitLevel1(subId, AaplId, 150m, now.AddSeconds(1), CancellationToken);
		await adapter.EmitLevel1(subId, GoogId, 2800m, now.AddSeconds(2), CancellationToken);
		await Task.Delay(200, CancellationToken);

		IsTrue(receivedL1.Count >= 2, $"Expected at least 2 Level1 messages, got {receivedL1.Count}");
		IsTrue(receivedL1.Any(l => l.msg.SecurityId == AaplId), "Expected AAPL Level1");
		IsTrue(receivedL1.Any(l => l.msg.SecurityId == GoogId), "Expected GOOG Level1");

		runCts.Cancel();
		await run.WithCancellation(CancellationToken);
	}

	#endregion

	#region Test 4: Candles from ticks, multiple securities

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task SecurityAll_CandlesFromTicks_MultipleSecurities()
	{
		var (connector, adapter) = CreateConnector(canFilterBySecurity: true, supportCandles: false);
		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame());
		var receivedCandles = new List<(Subscription sub, ICandleMessage candle)>();
		connector.CandleReceived += (s, c) => receivedCandles.Add((s, c));

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();
		await started.Task.WithCancellation(CancellationToken);

		var subId = await WaitForSubscription(adapter, CancellationToken);
		var activeSub = adapter.ActiveSubscriptions.Values.First();
		AreEqual(DataType.Ticks, activeSub.DataType2, "CandleBuilder should convert to tick subscription");

		var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Phase 1: warm up SecurityAll children
		await WarmUpSecurity(adapter, subId, AaplId, DataType.Ticks, baseTime, CancellationToken);
		await WarmUpSecurity(adapter, subId, GoogId, DataType.Ticks, baseTime.AddMilliseconds(1), CancellationToken);

		// Phase 2: warm up CandleBuilder children (second loopback layer)
		await adapter.EmitTick(subId, AaplId, 100m, 1, baseTime.AddSeconds(1), CancellationToken);
		await adapter.EmitTick(subId, GoogId, 2700m, 1, baseTime.AddSeconds(1), CancellationToken);
		await Task.Delay(300, CancellationToken);

		// Phase 3: one more tick so CandleBuilder children are fully active
		await adapter.EmitTick(subId, AaplId, 101m, 1, baseTime.AddSeconds(2), CancellationToken);
		await adapter.EmitTick(subId, GoogId, 2701m, 1, baseTime.AddSeconds(2), CancellationToken);
		await Task.Delay(200, CancellationToken);

		receivedCandles.Clear();

		// emit ticks within a single 1-min candle window
		for (var i = 0; i < 5; i++)
		{
			await adapter.EmitTick(subId, AaplId, 150m + i, 10, baseTime.AddSeconds(10 + i * 5), CancellationToken);
			await adapter.EmitTick(subId, GoogId, 2800m + i, 5, baseTime.AddSeconds(10 + i * 5), CancellationToken);
		}

		// emit ticks in the next minute to close the current candle
		await adapter.EmitTick(subId, AaplId, 160m, 10, baseTime.AddMinutes(1).AddSeconds(1), CancellationToken);
		await adapter.EmitTick(subId, GoogId, 2810m, 5, baseTime.AddMinutes(1).AddSeconds(1), CancellationToken);

		await Task.Delay(500, CancellationToken);

		IsTrue(receivedCandles.Count >= 2, $"Expected at least 2 candles, got {receivedCandles.Count}");
		IsTrue(receivedCandles.Any(c => ((ISecurityIdMessage)c.candle).SecurityId == AaplId), "Expected AAPL candle");
		IsTrue(receivedCandles.Any(c => ((ISecurityIdMessage)c.candle).SecurityId == GoogId), "Expected GOOG candle");

		runCts.Cancel();
		await run.WithCancellation(CancellationToken);
	}

	#endregion
}
