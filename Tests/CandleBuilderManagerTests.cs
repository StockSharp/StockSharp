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

		public TestInnerAdapter(IEnumerable<DataType> supported)
			: base(new IncrementalIdGenerator())
		{
			_supported = [.. supported];

			this.AddMarketDataSupport();
			foreach (var type in _supported)
				this.AddSupportedMarketDataType(type);
		}

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
	public async Task Subscribe_SubscriptionSecurityAll_RoutesToProcessMarketData()
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

		// Now simulate a tick coming in which creates a child
		var secId = Helper.CreateSecurityId();
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		// Should have created a child SubscriptionSecurityAllMessage
		var childMsg = extraOut.OfType<SubscriptionSecurityAllMessage>().FirstOrDefault();
		IsNotNull(childMsg);
		childMsg.SecurityId.AssertEqual(secId);
		childMsg.ParentTransactionId.AssertEqual(parentTransId);

		// Now send the child back (simulating loopback)
		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(childMsg, CancellationToken);

		// Should return response, not forward to inner
		toInner2.Length.AssertEqual(0);
		toOut2.Length.AssertEqual(1);
		toOut2[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		var response = (SubscriptionResponseMessage)toOut2[0];
		response.OriginalTransactionId.AssertEqual(childMsg.TransactionId);
		response.Error.AssertNull();
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
	public async Task AllSecurity_Tick_CreatesChildSubscription()
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

		// Send a tick for a specific security
		var secId = Helper.CreateSecurityId();
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);

		// Should create a child SubscriptionSecurityAllMessage
		var childMsg = extraOut.OfType<SubscriptionSecurityAllMessage>().FirstOrDefault();
		IsNotNull(childMsg);
		childMsg.SecurityId.AssertEqual(secId);
		childMsg.ParentTransactionId.AssertEqual(parentTransId);
		childMsg.IsSubscribe.AssertTrue();
	}

	[TestMethod]
	public async Task AllSecurity_ChildUnsubscribe_RemovesFromAllChilds()
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
		var subSent = (MarketDataMessage)subToInner[0];
		subSent.DataType2.AssertEqual(DataType.Ticks);
		subSent.SecurityId.AssertEqual(default(SecurityId)); // all securities
		subSent.IsSubscribe.AssertTrue();
		subSent.TransactionId.AssertEqual(parentTransId);
		subToOut.Length.AssertEqual(0);

		// Send a tick to create child
		var secId = Helper.CreateSecurityId();
		var tick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		tick.SetSubscriptionIds([parentTransId]);

		var (tickForward, extraOut) = await manager.ProcessOutMessageAsync(tick, CancellationToken);
		tickForward.AssertNull(); // tick is consumed, not forwarded
		var childMsg = extraOut.OfType<SubscriptionSecurityAllMessage>().First();
		IsNotNull(childMsg);
		childMsg.SecurityId.AssertEqual(secId);
		childMsg.ParentTransactionId.AssertEqual(parentTransId);
		childMsg.IsSubscribe.AssertTrue();

		// Send child back (loopback)
		var (childToInner, childToOut) = await manager.ProcessInMessageAsync(childMsg, CancellationToken);
		childToInner.Length.AssertEqual(0);
		childToOut.Length.AssertEqual(1);
		childToOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		var childResponse = (SubscriptionResponseMessage)childToOut[0];
		childResponse.OriginalTransactionId.AssertEqual(childMsg.TransactionId);
		childResponse.Error.AssertNull();

		// Now unsubscribe the child
		var unsubscribeMsg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 200,
			OriginalTransactionId = childMsg.TransactionId,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(unsubscribeMsg, CancellationToken);

		// Should return response and not forward to inner
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
		toOut[0].Type.AssertEqual(MessageTypes.SubscriptionResponse);
		var unsubResponse = (SubscriptionResponseMessage)toOut[0];
		unsubResponse.OriginalTransactionId.AssertEqual(200);
		unsubResponse.Error.AssertNull();
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
