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

		public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
			=> _supported;
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
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public void InnerMessage_DelegatesToManager_AndRoutesMessages()
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
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(2);
		output[0].AssertSame(extra);
		output[1].AssertSame(forward);

		manager.Verify(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}
}
