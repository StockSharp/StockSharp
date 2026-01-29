namespace StockSharp.Tests;

[TestClass]
public class FillGapsMessageAdapterTests : BaseTestClass
{
	private sealed class QueueFillGapsBehaviour(params (DateTime gapStart, DateTime gapEnd)[] gaps) : IFillGapsBehaviour
	{
		private readonly Queue<(DateTime gapStart, DateTime gapEnd)> _gaps = new(gaps);

		public ValueTask<(DateTime? gapStart, DateTime? gapEnd)> TryGetNextGapAsync(SecurityId secId, DataType dataType, DateTime from, DateTime to, FillGapsDays fillGaps, CancellationToken cancellationToken)
			=> new(_gaps.TryDequeue(out var gap) ? (gap.gapStart, gap.gapEnd) : default);
	}

	private static async Task DrainLoopbacksAsync(IMessageAdapter adapter, Queue<Message> loopbacks, CancellationToken cancellationToken)
	{
		// Wait for async gap detection to complete
		await Task.Delay(50, cancellationToken);

		while (loopbacks.TryDequeue(out var loopback))
			await adapter.SendInMessageAsync(loopback, cancellationToken);
	}

	[TestMethod]
	public async Task Subscribe_WithFillGaps_RewritesToFirstGap_AndClearsFillGaps()
	{
		var token = CancellationToken;

		var gap1Start = new DateTime(2020, 1, 2);
		var gap1End = new DateTime(2020, 1, 3);

		var behaviour = new QueueFillGapsBehaviour((gap1Start, gap1End));
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FillGapsMessageAdapter(inner, behaviour);

		var original = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = new DateTime(2020, 1, 1),
			To = new DateTime(2020, 1, 10),
			FillGaps = FillGapsDays.All,
		};

		await adapter.SendInMessageAsync(original, token);

		original.From.AssertEqual(new DateTime(2020, 1, 1));
		original.To.AssertEqual(new DateTime(2020, 1, 10));
		original.FillGaps.AssertEqual(FillGapsDays.All);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertOfType<MarketDataMessage>();
		var sent = (MarketDataMessage)inner.InMessages[0];
		sent.TransactionId.AssertEqual(1);
		sent.SecurityId.AssertEqual(original.SecurityId);
		sent.DataType2.AssertEqual(DataType.Ticks);
		sent.IsSubscribe.AssertTrue();
		sent.FillGaps.AssertNull();
		sent.From.AssertEqual(gap1Start);
		sent.To.AssertEqual(gap1End);
	}

	[TestMethod]
	public async Task SubscriptionFinished_LoopbacksNextGap_AndSuppressesDuplicateResponses()
	{
		var token = CancellationToken;

		var gap1Start = new DateTime(2020, 1, 2);
		var gap1End = new DateTime(2020, 1, 3);

		var gap2Start = new DateTime(2020, 1, 6);
		var gap2End = new DateTime(2020, 1, 7);

		var behaviour = new QueueFillGapsBehaviour((gap1Start, gap1End), (gap2Start, gap2End));
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FillGapsMessageAdapter(inner, behaviour);

		var output = new List<Message>();
		var loopbacks = new Queue<Message>();

		adapter.NewOutMessageAsync += (msg, ct) =>
		{
			output.Add(msg);
			if (msg.IsBack())
				loopbacks.Enqueue(msg);
			return default;
		};

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = new DateTime(2020, 1, 1),
			To = new DateTime(2020, 1, 10),
			FillGaps = FillGapsDays.All,
		}, token);

		inner.InMessages.Count.AssertEqual(1);
		((MarketDataMessage)inner.InMessages[0]).From.AssertEqual(gap1Start);
		((MarketDataMessage)inner.InMessages[0]).To.AssertEqual(gap1End);

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 1 }, CancellationToken);
		output.OfType<SubscriptionResponseMessage>().Count().AssertEqual(1);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, CancellationToken);
		await DrainLoopbacksAsync(adapter, loopbacks, token);

		inner.InMessages.Count.AssertEqual(2);
		((MarketDataMessage)inner.InMessages[1]).From.AssertEqual(gap2Start);
		((MarketDataMessage)inner.InMessages[1]).To.AssertEqual(gap2End);

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 1 }, CancellationToken);
		output.OfType<SubscriptionResponseMessage>().Count().AssertEqual(1);
	}

	[TestMethod]
	public async Task AfterLastGap_WhenOriginalToIsNull_LoopbacksOriginalForOnline()
	{
		var token = CancellationToken;

		var gap1Start = new DateTime(2020, 1, 2);
		var gap1End = new DateTime(2020, 1, 3);

		var gap2Start = new DateTime(2020, 1, 6);
		var gap2End = new DateTime(2020, 1, 7);

		var behaviour = new QueueFillGapsBehaviour((gap1Start, gap1End), (gap2Start, gap2End));
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new FillGapsMessageAdapter(inner, behaviour);

		var loopbacks = new Queue<Message>();

		adapter.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg.IsBack())
				loopbacks.Enqueue(msg);
			return default;
		};

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = new DateTime(2020, 1, 1),
			To = null,
			FillGaps = FillGapsDays.All,
		}, token);

		inner.InMessages.Count.AssertEqual(1);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, CancellationToken);
		await DrainLoopbacksAsync(adapter, loopbacks, token);
		inner.InMessages.Count.AssertEqual(2);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, CancellationToken);
		await DrainLoopbacksAsync(adapter, loopbacks, token);
		inner.InMessages.Count.AssertEqual(3);

		var online = (MarketDataMessage)inner.InMessages[2];
		online.TransactionId.AssertEqual(1);
		online.IsSubscribe.AssertTrue();
		(online.From is null || online.From.Value == default).AssertTrue();
		(online.To is null || online.To.Value == default).AssertTrue();
		online.FillGaps.AssertNull();
	}
}
