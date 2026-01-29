namespace StockSharp.Tests;

[TestClass]
public class OrderLogMessageAdapterTests : BaseTestClass
{
	private sealed class TestDepthBuilder : IOrderLogMarketDepthBuilder
	{
		public QuoteChangeMessage Snapshot { get; set; }
		public Func<ExecutionMessage, QuoteChangeMessage> UpdateFunc { get; set; }

		public int SnapshotCalls { get; private set; }
		public int UpdateCalls { get; private set; }
		public DateTime? LastSnapshotTime { get; private set; }
		public QuoteChangeMessage LastUpdateResult { get; private set; }

		public QuoteChangeMessage GetSnapshot(DateTime serverTime)
		{
			SnapshotCalls++;
			LastSnapshotTime = serverTime;

			var snapshot = Snapshot?.TypedClone() ?? throw new InvalidOperationException("Snapshot is not set.");
			snapshot.ServerTime = serverTime;
			snapshot.LocalTime = serverTime;
			return snapshot;
		}

		public QuoteChangeMessage Update(ExecutionMessage item)
		{
			UpdateCalls++;
			LastUpdateResult = UpdateFunc?.Invoke(item);
			return LastUpdateResult;
		}
	}

	private static SubscriptionResponseMessage OkResponse(long originalTransactionId, DateTime localTime)
		=> new() { OriginalTransactionId = originalTransactionId, LocalTime = localTime };

	private static ExecutionMessage CreateOrderLog(SecurityId securityId, DateTime time, long[] subscriptionIds, decimal orderVolume = 1m)
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = securityId,
			ServerTime = time,
			LocalTime = time,
			IsSystem = true,
			TradeId = 123,
			TradePrice = 100m,
			OrderVolume = orderVolume,
		};

		msg.SetSubscriptionIds(subscriptionIds);
		return msg;
	}

	[TestMethod]
	public async Task MarketDepth_Subscribe_RewrittenToOrderLog_AndSnapshotSentOnResponse()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var builder = new TestDepthBuilder
		{
			Snapshot = new QuoteChangeMessage
			{
				SecurityId = secId,
				BuildFrom = DataType.OrderLog,
				State = QuoteChangeStates.SnapshotComplete,
				Bids = [new QuoteChange(100m, 1m)],
				Asks = [new QuoteChange(101m, 2m)],
			}
		};

		var inner = new RecordingPassThroughMessageAdapter(supportedMarketDataTypes: [DataType.OrderLog]);
		using var adapter = new OrderLogMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DepthBuilder = builder,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).DataType2.AssertEqual(DataType.OrderLog);

		output.Clear();

		var time = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		inner.SendOutMessage(OkResponse(1, time));

		builder.SnapshotCalls.AssertEqual(1);
		builder.LastSnapshotTime.AssertEqual(time);

		output.Count.AssertEqual(2);
		output[0].AssertOfType<QuoteChangeMessage>();
		((QuoteChangeMessage)output[0]).GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
		output[1].AssertOfType<SubscriptionResponseMessage>();
		((SubscriptionResponseMessage)output[1]).OriginalTransactionId.AssertEqual(1);
	}

	[TestMethod]
	public async Task Execution_OrderLog_ForMarketDepth_EmitsDepthUpdate_AndSuppressesOriginal()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var builder = new TestDepthBuilder
		{
			Snapshot = new QuoteChangeMessage
			{
				SecurityId = secId,
				BuildFrom = DataType.OrderLog,
				State = QuoteChangeStates.SnapshotComplete,
				Bids = [],
				Asks = [],
			},
			UpdateFunc = item => new QuoteChangeMessage
			{
				SecurityId = item.SecurityId,
				ServerTime = item.ServerTime,
				LocalTime = item.LocalTime,
				BuildFrom = DataType.OrderLog,
				State = QuoteChangeStates.Increment,
				Bids = [new QuoteChange(100m, 10m)],
				Asks = [],
			}
		};

		var inner = new RecordingPassThroughMessageAdapter(supportedMarketDataTypes: [DataType.OrderLog]);
		using var adapter = new OrderLogMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DepthBuilder = builder,
		}, token);

		inner.SendOutMessage(OkResponse(1, DateTime.UtcNow));

		output.Clear();

		inner.SendOutMessage(CreateOrderLog(secId, DateTime.UtcNow.AddSeconds(1), subscriptionIds: [1]));

		output.OfType<ExecutionMessage>().Any().AssertFalse();

		var depth = output.OfType<QuoteChangeMessage>().Single();
		depth.GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();
		depth.State.AssertEqual(QuoteChangeStates.Increment);
		depth.Bids.Length.AssertEqual(1);
		depth.Bids[0].Price.AssertEqual(100m);
		depth.Bids[0].Volume.AssertEqual(10m);
	}

	[TestMethod]
	public async Task Execution_OrderLog_WithUnknownSubscriptionId_IsForwardedWithRemainingIds()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var builder = new TestDepthBuilder
		{
			Snapshot = new QuoteChangeMessage
			{
				SecurityId = secId,
				BuildFrom = DataType.OrderLog,
				State = QuoteChangeStates.SnapshotComplete,
				Bids = [],
				Asks = [],
			},
			UpdateFunc = _ => new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
				State = QuoteChangeStates.Increment,
				Bids = [new QuoteChange(100m, 1m)],
				Asks = [],
			}
		};

		var inner = new RecordingPassThroughMessageAdapter(supportedMarketDataTypes: [DataType.OrderLog]);
		using var adapter = new OrderLogMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DepthBuilder = builder,
		}, token);

		inner.SendOutMessage(OkResponse(1, DateTime.UtcNow));

		output.Clear();

		inner.SendOutMessage(CreateOrderLog(secId, DateTime.UtcNow.AddSeconds(1), subscriptionIds: [1, 99]));

		output.OfType<QuoteChangeMessage>().Single().GetSubscriptionIds().SequenceEqual([1L]).AssertTrue();

		var forwarded = output.OfType<ExecutionMessage>().Single();
		forwarded.GetSubscriptionIds().SequenceEqual([99L]).AssertTrue();
	}

	[TestMethod]
	public async Task Ticks_Subscribe_RewrittenToOrderLog_AndExecutionConvertedToTick()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var inner = new RecordingPassThroughMessageAdapter(supportedMarketDataTypes: [DataType.OrderLog]);
		using var adapter = new OrderLogMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).DataType2.AssertEqual(DataType.OrderLog);

		inner.SendOutMessage(OkResponse(2, DateTime.UtcNow));

		output.Clear();

		inner.SendOutMessage(CreateOrderLog(secId, DateTime.UtcNow.AddSeconds(1), subscriptionIds: [2], orderVolume: 7m));

		output.OfType<ExecutionMessage>().Count().AssertEqual(1);

		var tick = output.OfType<ExecutionMessage>().Single();
		tick.DataType.AssertEqual(DataType.Ticks);
		tick.BuildFrom.AssertEqual(DataType.OrderLog);
		tick.TradePrice.AssertEqual(100m);
		tick.TradeVolume.AssertEqual(7m);
		tick.GetSubscriptionIds().Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Execution_OrderLog_WhenBuilderThrows_SendsErrorMessage()
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();
		var builder = new TestDepthBuilder
		{
			Snapshot = new QuoteChangeMessage
			{
				SecurityId = secId,
				BuildFrom = DataType.OrderLog,
				State = QuoteChangeStates.SnapshotComplete,
				Bids = [],
				Asks = [],
			},
			UpdateFunc = _ => throw new InvalidOperationException("broken order log"),
		};

		var inner = new RecordingPassThroughMessageAdapter(supportedMarketDataTypes: [DataType.OrderLog]);
		using var adapter = new OrderLogMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			DepthBuilder = builder,
		}, token);

		inner.SendOutMessage(OkResponse(1, DateTime.UtcNow));

		output.Clear();

		inner.SendOutMessage(CreateOrderLog(secId, DateTime.UtcNow.AddSeconds(1), subscriptionIds: [1]));

		output.OfType<ErrorMessage>().Single().Error.Message.AssertEqual("broken order log");
	}
}
