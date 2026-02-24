namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

[TestClass]
public class CandleBuilderManagerTests : BaseTestClass
{
	private sealed class TestWrapper : MessageAdapterWrapper
	{
		public TestWrapper(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		public override IMessageAdapter Clone()
			=> new TestWrapper(InnerAdapter.TypedClone());
	}

	private sealed class TestInnerAdapter : MessageAdapter
	{
		private readonly DataType[] _supported;

		public Func<MarketDataMessage, bool> SupportCandlesUpdates { get; set; }

		public TestInnerAdapter(IEnumerable<DataType> supported)
			: base(new IncrementalIdGenerator())
		{
			_supported = [.. supported];

			this.AddMarketDataSupport();
			foreach (var type in _supported)
				this.AddSupportedMarketDataType(type);
		}

		public override bool IsSupportCandlesUpdates(MarketDataMessage subscription)
			=> SupportCandlesUpdates?.Invoke(subscription) ?? false;

		public override IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
			=> _supported.ToAsyncEnumerable();
	}

	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public async Task Subscribe_BuildFromTicks_ReturnsInnerSubscription()
	{
		var inner = new TestInnerAdapter([DataType.Ticks]);
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var wrapper = new TestWrapper(inner);
		var manager = new CandleBuilderManager(
			logReceiver,
			transactionIdGenerator,
			wrapper,
			sendFinishedCandlesImmediatelly: false,
			buffer: null,
			cloneOutCandles: true,
			provider);

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(message, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(DataType.Ticks);
		sent.TransactionId.AssertEqual(1);
		sent.IsSubscribe.AssertTrue();
	}

	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var manager = new Mock<ICandleBuilderManager>();

		var toInner = new ConnectMessage();
		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessInMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
			.Returns(new ValueTask<(Message[] toInner, Message[] toOut)>((toInner: [toInner], toOut: [toOut])));

		using var adapter = new CandleBuilderMessageAdapter(inner, provider, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task InnerMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var manager = new Mock<ICandleBuilderManager>();

		var forward = new ConnectMessage();
		var extra = new SubscriptionResponseMessage { OriginalTransactionId = 2 };

		manager
			.Setup(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
			.Returns(new ValueTask<(Message forward, Message[] extraOut)>((forward, [extra])));

		using var adapter = new CandleBuilderMessageAdapter(inner, provider, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(new DisconnectMessage(), CancellationToken);

		output.Count.AssertEqual(2);
		output[0].AssertSame(extra);
		output[1].AssertSame(forward);

		manager.Verify(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	private static CandleBuilderManager CreateManager(out TestWrapper wrapper, out IncrementalIdGenerator idGenerator)
	{
		var inner = new TestInnerAdapter([DataType.Ticks]);
		var logReceiver = new TestReceiver();
		idGenerator = new IncrementalIdGenerator();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		wrapper = new TestWrapper(inner);

		return new CandleBuilderManager(
			logReceiver,
			idGenerator,
			wrapper,
			sendFinishedCandlesImmediatelly: false,
			buffer: null,
			cloneOutCandles: true,
			provider);
	}

	[TestMethod]
	public async Task Reset_ClearsState()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// First subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(100);
		subToOut.Length.AssertEqual(0);

		// Reset
		var (toInner, toOut) = await manager.ProcessInMessageAsync(new ResetMessage(), CancellationToken);

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Subscribe_AllSecurity_InlineChildCreation()
	{
		var manager = CreateManager(out var wrapper, out var idGenerator);

		// First create a parent all-security subscription
		var parentTransId = idGenerator.GetNextId();
		var parentMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = parentTransId,
			SecurityId = default, // all securities
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (parentToInner, parentToOut) = await manager.ProcessInMessageAsync(parentMsg, CancellationToken);
		parentToInner.Length.AssertEqual(1);
		var parentSent = (MarketDataMessage)parentToInner[0];
		parentSent.DataType2.AssertEqual(DataType.Ticks);
		parentToOut.Length.AssertEqual(0);

		// Now simulate a tick coming in — inline child SeriesInfo should be created
		var secId = Helper.CreateSecurityId();
		var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = baseTime,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		// No loopback subscribe message — child is created inline
		extraOut.OfType<MarketDataMessage>().Any(m => m.IsSubscribe).AssertFalse();
		forward.AssertNull();

		// The first tick may already produce a candle (started state).
		// Close candle by sending tick in next minute
		var tick2 = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = baseTime.AddMinutes(1).AddSeconds(1),
			TradePrice = 120m,
			TradeVolume = 1m,
		};
		tick2.SetSubscriptionIds([parentTransId]);
		var (fwd2, extra2) = await manager.ProcessOutMessageAsync(tick2, CancellationToken);

		// Should produce a finished candle
		var candles = extra2.OfType<CandleMessage>().ToArray();
		IsTrue(candles.Length >= 1, $"Expected at least 1 candle, got {candles.Length}");
		candles[0].SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public async Task Unsubscribe_RemovesSeries()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subToOut.Length.AssertEqual(0);

		// Unsubscribe
		var unsubscribeMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(unsubscribeMsg, CancellationToken);

		toInner.Length.AssertEqual(1);
		var sent = (MarketDataMessage)toInner[0];
		sent.IsSubscribe.AssertFalse();
		sent.OriginalTransactionId.AssertEqual(1);
		sent.TransactionId.AssertEqual(2);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task SubscriptionResponse_Error_RemovesSeries()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(1);
		subToOut.Length.AssertEqual(0);

		// Error response
		var errorResponse = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("Test error"),
		};

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(errorResponse, CancellationToken);

		// Manager consumes the message and outputs result via extraOut
		forward.AssertNull();
		extraOut.Length.AssertEqual(1);
		var response = (SubscriptionResponseMessage)extraOut[0];
		response.OriginalTransactionId.AssertEqual(1);
		response.Error.AssertNotNull();
	}

	[TestMethod]
	public async Task SubscriptionFinished_RemovesSeries()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(1);
		subToOut.Length.AssertEqual(0);

		// Finished
		var finishedMsg = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(finishedMsg, CancellationToken);

		// Manager consumes the message and outputs result via extraOut
		forward.AssertNull();
		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.SubscriptionFinished);
		var finished = (SubscriptionFinishedMessage)extraOut[0];
		finished.OriginalTransactionId.AssertEqual(1);
	}

	[TestMethod]
	public async Task SubscriptionOnline_ForwardedCorrectly()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Subscribe
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(1);
		subToOut.Length.AssertEqual(0);

		// Online
		var onlineMsg = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(onlineMsg, CancellationToken);

		forward.AssertSame(onlineMsg);
		var online = (SubscriptionOnlineMessage)forward;
		online.OriginalTransactionId.AssertEqual(1);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Tick_WithMultipleSubscriptionIds_CandleHasAllIds()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Two subscriptions to build candles from ticks
		var sub1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};
		var (sub1Inner, _) = await manager.ProcessInMessageAsync(sub1, CancellationToken);
		sub1Inner.Length.AssertEqual(1);

		var sub2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};
		var (sub2Inner, _) = await manager.ProcessInMessageAsync(sub2, CancellationToken);

		// Send tick with both subscription IDs (as joined by SubscriptionOnlineManager)
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = now,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([1, 2]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		forward.AssertNull("Tick consumed for building");

		var candles = extraOut.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(2, "Should produce 2 candles (one per subscription)");

		var allIds = candles.SelectMany(c => c.GetSubscriptionIds()).Distinct().ToArray();
		allIds.Length.AssertEqual(2, "Candles should contain both subscription IDs");
		allIds.Count(id => id == 1).AssertEqual(1, "Candles should contain first subscription ID");
		allIds.Count(id => id == 2).AssertEqual(1, "Candles should contain second subscription ID");
	}

	[TestMethod]
	public async Task Tick_WithMixedSubscriptionIds_OnlyRelevantIdsInCandle()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// One subscription for candle building
		var sub = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};
		await manager.ProcessInMessageAsync(sub, CancellationToken);

		// Tick with subscription ID 1 (known) + 999 (unknown to this manager)
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = now,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([1, 999]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		var candles = extraOut.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(1, "Should produce exactly 1 candle");

		var ids = candles[0].GetSubscriptionIds();
		ids.Count(id => id == 1).AssertEqual(1, "Should contain known subscription ID");
	}

	[TestMethod]
	public async Task SubscriptionFinished_OneOfTwo_OtherStillBuildsCandles()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Two subscriptions for same candle type
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Finished for first subscription
		await manager.ProcessOutMessageAsync(
			new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, CancellationToken);

		// Tick with second subscription ID — should still build candle
		var tick = new ExecutionMessage
		{
			SecurityId = secId, DataTypeEx = DataType.Ticks,
			ServerTime = now, TradePrice = 100m, TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([2]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		var candles = extraOut.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(1, "Remaining subscription should still build candles");
		candles[0].GetSubscriptionIds().Count(id => id == 2).AssertEqual(1, "Candle should have remaining subscription ID");
	}

	[TestMethod]
	public async Task SubscriptionError_OneOfTwo_OtherStillBuildsCandles()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Two subscriptions
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Error for first subscription
		await manager.ProcessOutMessageAsync(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1, Error = new InvalidOperationException("fail"),
		}, CancellationToken);

		// Tick with second subscription ID
		var tick = new ExecutionMessage
		{
			SecurityId = secId, DataTypeEx = DataType.Ticks,
			ServerTime = now, TradePrice = 100m, TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([2]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		var candles = extraOut.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(1, "Remaining subscription should still build candles after error on other");
	}

	[TestMethod]
	public async Task SubscriptionFinished_Both_NoMoreCandles()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Two subscriptions
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Finished for both
		await manager.ProcessOutMessageAsync(
			new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, CancellationToken);
		await manager.ProcessOutMessageAsync(
			new SubscriptionFinishedMessage { OriginalTransactionId = 2 }, CancellationToken);

		// Tick arrives — no subscriptions left
		var tick = new ExecutionMessage
		{
			SecurityId = secId, DataTypeEx = DataType.Ticks,
			ServerTime = now, TradePrice = 100m, TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([1, 2]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		var candles = extraOut.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(0, "No candles should be built after all subscriptions finished");
	}

	[TestMethod]
	public async Task AllSecurity_Tick_CreatesInlineChild()
	{
		var manager = CreateManager(out var wrapper, out var idGenerator);

		// Subscribe to all securities
		var parentTransId = idGenerator.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = parentTransId,
			SecurityId = default, // all securities
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(default(SecurityId)); // all securities
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(parentTransId);
		subToOut.Length.AssertEqual(0);

		// Send a tick for a specific security — inline child should be created (no loopback)
		var secId = Helper.CreateSecurityId();
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		// No loopback message — child is created inline
		extraOut.OfType<MarketDataMessage>().Any().AssertFalse();
	}

	[TestMethod]
	public async Task AllSecurity_Unsubscribe_ForwardsToInner()
	{
		var manager = CreateManager(out var wrapper, out var idGenerator);

		// Subscribe to all securities
		var parentTransId = idGenerator.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = parentTransId,
			SecurityId = default,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);

		// Send a tick to create inline child
		var secId = Helper.CreateSecurityId();
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		await manager.ProcessOutMessageAsync(tick, CancellationToken);

		// Now unsubscribe the parent
		var unsubscribeMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 200,
			OriginalTransactionId = parentTransId,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(unsubscribeMsg, CancellationToken);

		// Should forward to inner (unsubscribe from tick source)
		toInner.Length.AssertEqual(1);
		var sent = (MarketDataMessage)toInner[0];
		sent.IsSubscribe.AssertFalse();
		sent.OriginalTransactionId.AssertEqual(parentTransId);
		sent.TransactionId.AssertEqual(200);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task NonMarketDataMessage_PassesThrough()
	{
		var manager = CreateManager(out _, out _);

		var connectMsg = new ConnectMessage();

		var (toInner, toOut) = await manager.ProcessInMessageAsync(connectMsg, CancellationToken);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(connectMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ProcessOutMessage_NonSubscriptionMessage_PassesThrough()
	{
		var manager = CreateManager(out _, out _);

		var disconnectMsg = new DisconnectMessage();

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(disconnectMsg, CancellationToken);

		forward.AssertSame(disconnectMsg);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Subscribe_RegularTimeFrame_Supported_ForwardsDirectly()
	{
		// Create manager with TF candles support
		var inner = new TestInnerAdapter([TimeSpan.FromMinutes(1).TimeFrame()]);
		var logReceiver = new TestReceiver();
		var idGenerator = new IncrementalIdGenerator();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var wrapper = new TestWrapper(inner);

		var manager = new CandleBuilderManager(
			logReceiver,
			idGenerator,
			wrapper,
			sendFinishedCandlesImmediatelly: false,
			buffer: null,
			cloneOutCandles: true,
			provider);

		var secId = Helper.CreateSecurityId();
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribeMsg);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Subscribe_LoadOnly_NotSupported_ReturnsNotSupported()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Load, // Load only, no build fallback
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);

		// Should return not supported error
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
		toOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);

		var response = (SubscriptionResponseMessage)toOut[0];
		response.OriginalTransactionId.AssertEqual(1);
		response.IsNotSupported().AssertTrue();
	}

	#region Status Message Handling — Multiple Series

	[TestMethod]
	public async Task StatusMessage_Response_TwoSeries_EachEmittedViaExtraOut()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Two subscriptions building from ticks
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Response OK for series1 → forward=null, extraOut has response
		var resp1 = new SubscriptionResponseMessage { OriginalTransactionId = 1 };
		var (forward1, extra1) = await manager.ProcessOutMessageAsync(resp1, CancellationToken);

		forward1.AssertNull("CandleBuilderManager always suppresses forward for Response");
		extra1.Length.AssertEqual(1);
		extra1[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		((SubscriptionResponseMessage)extra1[0]).OriginalTransactionId.AssertEqual(1);

		// Response OK for series2 → forward=null, extraOut has response
		var resp2 = new SubscriptionResponseMessage { OriginalTransactionId = 2 };
		var (forward2, extra2) = await manager.ProcessOutMessageAsync(resp2, CancellationToken);

		forward2.AssertNull("CandleBuilderManager always suppresses forward for Response");
		extra2.Length.AssertEqual(1);
		((SubscriptionResponseMessage)extra2[0]).OriginalTransactionId.AssertEqual(2);
	}

	[TestMethod]
	public async Task StatusMessage_Online_TwoSeries_EachForwardedWithCorrectId()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Two subscriptions building from ticks
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Online for series1 → forwarded
		var online1 = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };
		var (forward1, extra1) = await manager.ProcessOutMessageAsync(online1, CancellationToken);

		forward1.AssertNotNull("Online should be forwarded");
		forward1.AssertSame(online1);
		((SubscriptionOnlineMessage)forward1).OriginalTransactionId.AssertEqual(1);
		extra1.Length.AssertEqual(0);

		// Online for series2 → forwarded
		var online2 = new SubscriptionOnlineMessage { OriginalTransactionId = 2 };
		var (forward2, extra2) = await manager.ProcessOutMessageAsync(online2, CancellationToken);

		forward2.AssertNotNull("Online for series2 should be forwarded");
		((SubscriptionOnlineMessage)forward2).OriginalTransactionId.AssertEqual(2);
		extra2.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task StatusMessage_Finished_OneOfTwo_OtherStillGetsOnline()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Finished for series1 → forward=null, extraOut has finished
		var finished1 = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (forward1, extra1) = await manager.ProcessOutMessageAsync(finished1, CancellationToken);

		forward1.AssertNull("CandleBuilderManager suppresses forward for Finished");
		var finishedOut = extra1.OfType<SubscriptionFinishedMessage>().ToArray();
		finishedOut.Length.AssertEqual(1, "Should emit finished via extraOut");
		finishedOut[0].OriginalTransactionId.AssertEqual(1);

		// Series2 should still work — Online arrives and is forwarded
		var online2 = new SubscriptionOnlineMessage { OriginalTransactionId = 2 };
		var (forward2, extra2) = await manager.ProcessOutMessageAsync(online2, CancellationToken);

		forward2.AssertNotNull("Online for series2 should still be forwarded after series1 finished");
		((SubscriptionOnlineMessage)forward2).OriginalTransactionId.AssertEqual(2);
		extra2.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task StatusMessage_Error_OneOfTwo_OtherStillGetsResponse()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Two subscriptions
		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 1, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true, TransactionId = 2, SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build, BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Error for series1 → forward=null, extraOut has error
		var errorResp = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("fail"),
		};
		var (forward1, extra1) = await manager.ProcessOutMessageAsync(errorResp, CancellationToken);

		forward1.AssertNull("CandleBuilderManager suppresses forward for Response");
		extra1.Length.AssertEqual(1, "Should emit error via extraOut");

		// Series2 should still work — Response OK
		var resp2 = new SubscriptionResponseMessage { OriginalTransactionId = 2 };
		var (forward2, extra2) = await manager.ProcessOutMessageAsync(resp2, CancellationToken);

		forward2.AssertNull("Forward suppressed for Response");
		extra2.Length.AssertEqual(1);
		((SubscriptionResponseMessage)extra2[0]).OriginalTransactionId.AssertEqual(2);
		((SubscriptionResponseMessage)extra2[0]).IsOk().AssertTrue("Series2 response should be OK");
	}

	#endregion

	private static CandleBuilderManager CreateManagerWith(
		DataType[] supported,
		Func<MarketDataMessage, bool> supportCandlesUpdates,
		out TestWrapper wrapper,
		out IncrementalIdGenerator idGenerator,
		out TestReceiver logReceiver)
	{
		var inner = new TestInnerAdapter(supported) { SupportCandlesUpdates = supportCandlesUpdates };
		logReceiver = new TestReceiver();
		idGenerator = new IncrementalIdGenerator();
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		wrapper = new TestWrapper(inner);

		return new CandleBuilderManager(
			logReceiver,
			idGenerator,
			wrapper,
			sendFinishedCandlesImmediatelly: false,
			buffer: null,
			cloneOutCandles: true,
			provider);
	}

	#region IsSupportCandlesUpdates / IsFinishedOnly / BuildFrom Tests

	[TestMethod]
	public async Task LiveCandle_NoUpdatesSupport_WithTicks_CapsToAndTransitionsToCompress()
	{
		// Adapter supports TF candles AND ticks, but NOT IsSupportCandlesUpdates
		// Subscription: To=null, BuildMode=LoadAndBuild, IsFinishedOnly=false
		// Expected: To gets capped to CurrentTime, then after history finishes, transitions to build from ticks
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			// To = null => hist+live
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(tf); // still candle subscription
		sent.To.AssertNotNull(); // To was capped
		sent.To.AssertEqual(logReceiver.Time);
		sent.TransactionId.AssertEqual(1);

		// Now simulate history finishes — should transition to Compress (build from ticks)
		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (fwd, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		fwd.AssertNull();

		// Should emit a loopback MarketDataMessage for ticks subscription
		var tickSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(tickSub, "Should create a build-from-ticks subscription after history finishes");
		tickSub.DataType2.AssertEqual(DataType.Ticks);
		tickSub.IsSubscribe.AssertTrue();
	}

	[TestMethod]
	public async Task LiveCandle_WithUpdatesSupport_DoesNotCapTo()
	{
		// Adapter supports TF candles AND IsSupportCandlesUpdates
		// Expected: To stays null, subscription passes through as-is
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks],
			supportCandlesUpdates: _ => true,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			// To = null => hist+live
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(tf);
		sent.To.AssertNull(); // NOT capped — adapter supports live candle updates
	}

	[TestMethod]
	public async Task LiveCandle_IsFinishedOnly_DoesNotCapTo()
	{
		// Adapter does NOT support IsSupportCandlesUpdates, but IsFinishedOnly=true
		// Expected: To stays null, subscription passes through
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			IsFinishedOnly = true,
			// To = null => hist+live
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(tf);
		sent.To.AssertNull(); // NOT capped — IsFinishedOnly skips the redirect logic
	}

	[TestMethod]
	public async Task LiveCandle_NoBuildSource_DoesNotCapTo()
	{
		// Adapter supports TF candles but does NOT support ticks/level1/orderbook/depth
		// Expected: To stays null, no build source available
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf], // only candles, no build source
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			// To = null => hist+live
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.DataType2.AssertEqual(tf);
		sent.To.AssertNull(); // NOT capped — no build source available
	}

	[TestMethod]
	public async Task LiveCandle_BuildFromLevel1_CapsToAndTransitions()
	{
		// Adapter supports TF candles AND Level1, but NOT ticks and NOT IsSupportCandlesUpdates
		// Expected: To capped, then after history finishes, builds from Level1
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Level1],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.To.AssertNotNull();
		sent.To.AssertEqual(logReceiver.Time);

		// History finishes — should transition to build from Level1
		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (fwd, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		fwd.AssertNull();
		var buildSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(buildSub, "Should create a build-from-level1 subscription");
		buildSub.DataType2.AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public async Task LiveCandle_BuildFromDepth_CapsToAndTransitions()
	{
		// Adapter supports TF candles AND MarketDepth only
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.MarketDepth],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toInner.Length.AssertEqual(1);
		((MarketDataMessage)toInner[0]).To.AssertEqual(logReceiver.Time);

		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (fwd, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		fwd.AssertNull();
		var buildSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(buildSub, "Should create a build-from-depth subscription");
		buildSub.DataType2.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public async Task LiveCandle_BuildFromOrderLog_CapsToAndTransitions()
	{
		// Adapter supports TF candles AND OrderLog only
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.OrderLog],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		toInner.Length.AssertEqual(1);
		((MarketDataMessage)toInner[0]).To.AssertEqual(logReceiver.Time);

		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (fwd, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		fwd.AssertNull();
		var buildSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(buildSub, "Should create a build-from-orderlog subscription");
		buildSub.DataType2.AssertEqual(DataType.OrderLog);
	}

	[TestMethod]
	public async Task LiveCandle_BuildSourcePriority_TicksOverLevel1()
	{
		// Adapter supports ticks AND level1 — ticks should be chosen (higher priority)
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks, DataType.Level1],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
		};

		await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (_, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		var buildSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(buildSub);
		buildSub.DataType2.AssertEqual(DataType.Ticks); // ticks has priority over level1
	}

	[TestMethod]
	public async Task LiveCandle_ExplicitBuildFrom_UsedInsteadOfAutoDetect()
	{
		// Adapter supports ticks AND level1, but user explicitly requests BuildFrom=Level1
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks, DataType.Level1],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			BuildFrom = DataType.Level1,
		};

		await manager.ProcessInMessageAsync(mdMsg, CancellationToken);

		var finished = new SubscriptionFinishedMessage { OriginalTransactionId = 1 };
		var (_, extraOut) = await manager.ProcessOutMessageAsync(finished, CancellationToken);

		var buildSub = extraOut.OfType<MarketDataMessage>().SingleOrDefault(m => m.IsSubscribe);
		IsNotNull(buildSub);
		buildSub.DataType2.AssertEqual(DataType.Level1); // user's explicit choice
	}

	[TestMethod]
	public async Task LiveCandle_WithUpdatesSupport_OnlineForwardedDirectly()
	{
		// When adapter supports candle updates, subscription stays live (no To capping)
		// Online message should be forwarded directly to caller
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks],
			supportCandlesUpdates: _ => true,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
		};

		var (toInner, _) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);
		toInner.Length.AssertEqual(1);
		((MarketDataMessage)toInner[0]).To.AssertNull();

		// Online arrives — should pass through (no upgrade needed)
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };
		var (fwd, extraOut) = await manager.ProcessOutMessageAsync(online, CancellationToken);

		fwd.AssertSame(online);
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task LiveCandle_ToAlreadySet_DoesNotOverwrite()
	{
		// If To is already set by caller (history-only), it should NOT be changed
		var tf = TimeSpan.FromMinutes(5).TimeFrame();
		var manager = CreateManagerWith(
			[tf, DataType.Ticks],
			supportCandlesUpdates: _ => false,
			out _, out _, out var logReceiver);

		logReceiver.Time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

		var originalTo = new DateTime(2025, 6, 1, 11, 0, 0, DateTimeKind.Utc);
		var secId = Helper.CreateSecurityId();
		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = tf,
			From = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc),
			To = originalTo,
		};

		var (toInner, _) = await manager.ProcessInMessageAsync(mdMsg, CancellationToken);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.To.AssertEqual(originalTo); // original To preserved, not overwritten
	}

	#endregion

	[TestMethod]
	public async Task Tick_WithSubscription_BuildsCandle()
	{
		var manager = CreateManager(out _, out _);

		var secId = Helper.CreateSecurityId();

		// Subscribe to build from ticks
		var subscribeMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		};

		var (subToInner, subToOut) = await manager.ProcessInMessageAsync(subscribeMsg, CancellationToken);
		subToInner.Length.AssertEqual(1);
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(secId);
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(1);
		subToOut.Length.AssertEqual(0);

		// Use a specific time that aligns to minute boundary for predictable candle creation
		var now = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Send first tick
		var tick1 = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = now,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick1.SetSubscriptionIds([1]);

		var (forward1, extraOut1) = await manager.ProcessOutMessageAsync(tick1, CancellationToken);

		// Tick is consumed for building, not forwarded
		forward1.AssertNull();
		// First tick creates initial candle state
		extraOut1.Length.AssertEqual(1);
		var candle1 = extraOut1[0] as CandleMessage;
		IsNotNull(candle1);
		candle1.OpenPrice.AssertEqual(100m);
		candle1.HighPrice.AssertEqual(100m);
		candle1.LowPrice.AssertEqual(100m);
		candle1.ClosePrice.AssertEqual(100m);
		candle1.TotalVolume.AssertEqual(10m);
		candle1.SecurityId.AssertEqual(secId);
		candle1.State.AssertEqual(CandleStates.Active);

		// Send second tick within same candle
		var tick2 = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = now.AddSeconds(10),
			TradePrice = 105m,
			TradeVolume = 20m,
		};
		tick2.SetSubscriptionIds([1]);

		var (forward2, extraOut2) = await manager.ProcessOutMessageAsync(tick2, CancellationToken);

		// Second tick updates the candle
		forward2.AssertNull();
		extraOut2.Length.AssertEqual(1);
		var candle2 = extraOut2[0] as CandleMessage;
		IsNotNull(candle2);
		candle2.OpenPrice.AssertEqual(100m);
		candle2.HighPrice.AssertEqual(105m);
		candle2.LowPrice.AssertEqual(100m);
		candle2.ClosePrice.AssertEqual(105m);
		candle2.TotalVolume.AssertEqual(30m);
		candle2.SecurityId.AssertEqual(secId);
		candle2.State.AssertEqual(CandleStates.Active);
	}
}
