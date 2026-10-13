namespace StockSharp.Tests;

/// <summary>
/// The promises the base <see cref="MessageAdapter"/> makes to everyone who sends it a message,
/// whatever the trading system behind it turns out to be: a request it cannot serve comes back as a
/// refusal rather than as silence, a request it can serve reaches the implementation untouched, and
/// every message carries a timestamp by the time anyone downstream sees it.
/// </summary>
[TestClass]
public class MessageAdapterTests : BaseTestClass
{
	// An adapter with no trading system behind it: it writes down what it was asked to do, so a test
	// can tell "the adapter refused" from "the adapter went and did it".
	private sealed class TestAdapter : MessageAdapter
	{
		public TestAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public List<Message> Handled { get; } = [];

		/// <summary>Boards this adapter claims to serve, if any.</summary>
		public string[] Boards { get; set; } = [];

		/// <summary>Whether the adapter answers transactional unsubscriptions by itself.</summary>
		public bool AutoReply { get; set; }

		/// <summary>The clock the adapter runs on, when a test pins it.</summary>
		public DateTime? Clock { get; set; }

		/// <summary>What handling a message fails with, when a test makes it fail.</summary>
		public Exception Fault { get; set; }

		public override string[] AssociatedBoards => Boards;

		public override bool IsAutoReplyOnTransactonalUnsubscription => AutoReply;

		public override DateTime CurrentTime => Clock ?? base.CurrentTime;

		/// <summary>Pretends the adapter was built for the named bitness.</summary>
		public void RunOn(Platforms platform) => Platform = platform;

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			if (Fault is not null)
				throw Fault;

			Handled.Add(message);
			return default;
		}

		public override ValueTask<IMessageAdapter> CloneAsync(CancellationToken cancellationToken)
			=> new(new TestAdapter());
	}

	private static (TestAdapter adapter, List<Message> output) CreateSut()
	{
		var adapter = new TestAdapter();

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, _) => { output.Add(m); return default; };

		return (adapter, output);
	}

	private static MarketDataMessage CreateSubscribe(long transactionId, SecurityId securityId) => new()
	{
		IsSubscribe = true,
		TransactionId = transactionId,
		SecurityId = securityId,
		DataType2 = DataType.Ticks,
	};

	/// <summary>
	/// An adapter that names the boards it serves cannot serve a security that trades somewhere else.
	/// The caller is entitled to be told so, by a refusal naming its own request, instead of having
	/// the subscription accepted and then never answered.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_ForeignBoard_IsRefused()
	{
		var (adapter, output) = CreateSut();

		adapter.Boards = [BoardCodes.Test];

		await adapter.SendInMessageAsync(CreateSubscribe(1, new SecurityId
		{
			SecurityCode = "AAA",
			BoardCode = "ELSEWHERE",
		}), CancellationToken);

		var response = output.OfType<SubscriptionResponseMessage>().Single();
		response.OriginalTransactionId.AssertEqual(1);
		IsNotNull(response.Error, "the caller is told why the subscription was refused");
		IsInstanceOfType<NotSupportedException>(response.Error);

		IsEmpty(adapter.Handled, "a security the adapter does not serve never reaches the trading system");
	}

	/// <summary>
	/// The other half of the same rule: a security that does trade on one of the adapter's own boards
	/// is passed through untouched, so naming the boards costs nothing to those who respect them.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_OnAnAssociatedBoard_ReachesTheTradingSystem()
	{
		var (adapter, output) = CreateSut();

		adapter.Boards = [BoardCodes.Test];

		var subscribe = CreateSubscribe(2, Helper.CreateSecurityId());

		await adapter.SendInMessageAsync(subscribe, CancellationToken);

		adapter.Handled.Single().AssertSame(subscribe);
		output.OfType<SubscriptionResponseMessage>().Count().AssertEqual(0, "nothing was refused");
	}

	/// <summary>
	/// News is not traded on a board, so it cannot be judged by one. Whether the adapter serves news
	/// at all is what decides, and an adapter that does serve it must not refuse its own news.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_ToNews_IsJudgedByWhetherTheAdapterServesNews()
	{
		static MarketDataMessage newsSubscribe(long transactionId) => new()
		{
			IsSubscribe = true,
			TransactionId = transactionId,
			SecurityId = SecurityId.News,
			DataType2 = DataType.News,
		};

		var (withoutNews, refusedOutput) = CreateSut();

		withoutNews.Boards = [BoardCodes.Test];

		await withoutNews.SendInMessageAsync(newsSubscribe(3), CancellationToken);

		refusedOutput.OfType<SubscriptionResponseMessage>().Single().OriginalTransactionId.AssertEqual(3);
		IsEmpty(withoutNews.Handled, "an adapter that has no news does not take a news subscription");

		var (withNews, servedOutput) = CreateSut();

		withNews.Boards = [BoardCodes.Test];
		withNews.AddSupportedMarketDataType(DataType.News);

		var subscribe = newsSubscribe(4);

		await withNews.SendInMessageAsync(subscribe, CancellationToken);

		withNews.Handled.Single().AssertSame(subscribe, "an adapter that does serve news takes the subscription");
		servedOutput.OfType<SubscriptionResponseMessage>().Count().AssertEqual(0);
	}

	/// <summary>
	/// An adapter built for the other bitness cannot load its native library, so the connection is
	/// refused with an error the user can read rather than with a crash somewhere deeper.
	/// </summary>
	[TestMethod]
	public async Task Connect_OnTheWrongBitness_IsRefusedWithAnError()
	{
		var (adapter, output) = CreateSut();

		adapter.RunOn(Environment.Is64BitProcess ? Platforms.x86 : Platforms.x64);

		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		var connect = output.OfType<ConnectMessage>().Single();
		IsNotNull(connect.Error, "the user is told the adapter cannot run here");
		IsInstanceOfType<InvalidOperationException>(connect.Error);

		IsEmpty(adapter.Handled, "no attempt is made to connect");
	}

	/// <summary>
	/// Dropping an order subscription has to be confirmed, or whoever dropped it waits for a
	/// confirmation that never comes. An adapter that cannot confirm it itself gets the confirmation
	/// made for it.
	/// </summary>
	[TestMethod]
	public async Task Unsubscribe_FromOrders_IsConfirmedForAnAdapterThatCannotConfirmItItself()
	{
		var (adapter, output) = CreateSut();

		adapter.AutoReply = true;

		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = false,
			TransactionId = 5,
		}, CancellationToken);

		var response = output.OfType<SubscriptionResponseMessage>().Single();
		response.OriginalTransactionId.AssertEqual(5);
		IsNull(response.Error, "dropping a subscription succeeded");
	}

	/// <summary>
	/// An adapter that confirms unsubscriptions itself must not have a second confirmation made for
	/// it, or the caller sees one request answered twice.
	/// </summary>
	[TestMethod]
	public async Task Unsubscribe_FromOrders_IsLeftToAnAdapterThatConfirmsItItself()
	{
		var (adapter, output) = CreateSut();

		adapter.AutoReply = false;

		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = false,
			TransactionId = 6,
		}, CancellationToken);

		output.OfType<SubscriptionResponseMessage>().Count().AssertEqual(0, "the confirmation is the adapter's own to send");
	}

	/// <summary>
	/// Everything that goes through an adapter is stamped with the time it went through, taken from
	/// that adapter's clock. In a backtest that clock is the simulated one, and a message stamped
	/// with anything else would be out of order against the data around it.
	/// </summary>
	[TestMethod]
	public async Task AMessageWithoutTimestamps_IsStampedFromTheAdapterClock()
	{
		var (adapter, _) = CreateSut();

		var now = new DateTime(2020, 03, 04, 09, 30, 00, DateTimeKind.Utc);
		adapter.Clock = now;

		var order = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 7,
			SecurityId = Helper.CreateSecurityId(),
			OrderState = OrderStates.Active,
			Side = Sides.Buy,
			OrderPrice = 100,
			OrderVolume = 1,
			PortfolioName = "PF",
		};

		await adapter.SendInMessageAsync(order, CancellationToken);

		order.LocalTime.AssertEqual(now, "the message is stamped with when the adapter saw it");
		order.ServerTime.AssertEqual(now, "a transaction with no time of its own is dated by the same clock");
	}

	/// <summary>
	/// The list of message types an adapter supports is a set: naming one type twice is a mistake in
	/// the adapter's own configuration, and it is refused where it is made rather than turning into
	/// doubled work later on.
	/// </summary>
	[TestMethod]
	public void ARepeatedMessageType_IsRefusedWhereTheListIsSet()
	{
		var (adapter, _) = CreateSut();

		Throws<ArgumentException>(() => adapter.SupportedInMessages = [MessageTypes.Connect, MessageTypes.MarketData, MessageTypes.Connect]);

		adapter.SupportedInMessages = [MessageTypes.Connect, MessageTypes.MarketData];
		adapter.SupportedInMessages.Count().AssertEqual(2, "a list without repeats is taken as it is");
	}

	/// <summary>
	/// When handling a message fails, the caller is told about that one request instead of being left
	/// waiting: the failure comes back as a refusal naming the request that failed.
	/// </summary>
	[TestMethod]
	public async Task AFailureHandlingASubscription_ComesBackAsARefusalOfThatSubscription()
	{
		var (adapter, output) = CreateSut();

		adapter.Fault = new InvalidOperationException("the trading system said no");

		await adapter.SendInMessageAsync(CreateSubscribe(8, Helper.CreateSecurityId()), CancellationToken);

		var response = output.OfType<SubscriptionResponseMessage>().Single();
		response.OriginalTransactionId.AssertEqual(8);
		response.Error.AssertSame(adapter.Fault, "the caller is told what went wrong");
	}

	/// <summary>
	/// Copying an adapter is how a connection the user configured gets reused - a second session, a
	/// backtest run, a connector saved and opened again. The copy has to arrive configured: an adapter
	/// whose heartbeat, parallelism, retry delay or list of supported messages is dropped along the way
	/// talks to the trading system on terms the user never chose, and nothing about the copy says so.
	/// Settings that survive being written to storage and read back are exactly the settings a copy
	/// has to carry, because the copy is made by writing them down and reading them into a new adapter.
	/// </summary>
	[TestMethod]
	public async Task ACopyOfAnAdapterArrivesWithTheSettingsItWasCopiedFrom()
	{
		var original = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			Id = Guid.NewGuid(),
			Name = "configured adapter",
			LogLevel = LogLevels.Debug,
			HeartbeatInterval = TimeSpan.FromSeconds(7),
			EnqueueSubscriptions = true,
			IterationInterval = TimeSpan.FromMilliseconds(250),
			MaxParallelMessages = 11,
			FaultDelay = TimeSpan.FromSeconds(3),
			SupportedInMessages = [MessageTypes.Connect, MessageTypes.Disconnect, MessageTypes.MarketData],
		};

		var clone = (MessageAdapter)await original.CloneAsync(CancellationToken);

		AreNotSame(original, clone, "a copy is a second adapter, not the same one handed back");
		IsInstanceOfType<PassThroughMessageAdapter>(clone, "a copy of an adapter is an adapter of the same kind");

		clone.Id.AssertEqual(original.Id, "the copy stands for the same configured connection");
		clone.Name.AssertEqual(original.Name, "the name the user gave the connection");
		clone.LogLevel.AssertEqual(original.LogLevel, "the copy logs as much as the user asked for");
		clone.HeartbeatInterval.AssertEqual(original.HeartbeatInterval, "a copy with no heartbeat is dropped by a venue that expects one");
		clone.EnqueueSubscriptions.AssertEqual(original.EnqueueSubscriptions, "how subscriptions are paced is a venue requirement, not a preference");
		clone.IterationInterval.AssertEqual(original.IterationInterval, "how often the copy wakes up");
		clone.MaxParallelMessages.AssertEqual(original.MaxParallelMessages, "a venue that allows so many requests at once still allows only so many");
		clone.FaultDelay.AssertEqual(original.FaultDelay, "how long the copy waits before trying again after a failure");

		AreEquivalent(original.SupportedInMessages.ToArray(), clone.SupportedInMessages.ToArray(),
			"a copy that forgets what it supports refuses requests the original served");
	}
}
