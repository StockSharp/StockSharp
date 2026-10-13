namespace StockSharp.Tests;

[TestClass]
public class FillGapsMessageAdapterTests : BaseTestClass
{
	private sealed class QueueFillGapsBehaviour(params (DateTime gapStart, DateTime gapEnd)[] gaps) : IFillGapsBehaviour
	{
		private readonly Queue<(DateTime gapStart, DateTime gapEnd)> _gaps = new(gaps);

		public ValueTask<(DateTime? gapStart, DateTime? gapEnd)> TryGetNextGapAsync(SecurityId secId, DataType dataType, DateTime from, DateTime to, FillGapsDays fillGaps, CancellationToken cancellationToken)
			=> new(_gaps.TryDequeue(out var gap) ? ((DateTime?)gap.gapStart, (DateTime?)gap.gapEnd) : (null, null));
	}

	private sealed class DatesStorageDrive(IMarketDataDrive drive, DateTime[] dates) : IMarketDataStorageDrive
	{
		IMarketDataDrive IMarketDataStorageDrive.Drive => drive;

		IAsyncEnumerable<DateTime> IMarketDataStorageDrive.GetDatesAsync() => dates.ToAsyncEnumerable();

		ValueTask IMarketDataStorageDrive.ClearDatesCacheAsync(CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		ValueTask IMarketDataStorageDrive.DeleteAsync(DateTime date, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		ValueTask IMarketDataStorageDrive.SaveStreamAsync(DateTime date, Stream stream, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		ValueTask<Stream> IMarketDataStorageDrive.LoadStreamAsync(DateTime date, bool readOnly, CancellationToken cancellationToken)
			=> throw new NotSupportedException();
	}

	private sealed class DatesMarketDataDrive : BaseMarketDataDrive
	{
		private readonly IMarketDataStorageDrive _storageDrive;

		public DatesMarketDataDrive(params DateTime[] dates)
		{
			_storageDrive = new DatesStorageDrive(this, dates);
		}

		public override string Path { get; set; } = string.Empty;

		public override IAsyncEnumerable<SecurityId> GetAvailableSecuritiesAsync()
			=> AsyncEnumerable.Empty<SecurityId>();

		public override IAsyncEnumerable<DataType> GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format)
			=> AsyncEnumerable.Empty<DataType>();

		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
			=> _storageDrive;

		public override ValueTask VerifyAsync(CancellationToken cancellationToken)
			=> default;

		public override IAsyncEnumerable<SecurityMessage> LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider)
			=> AsyncEnumerable.Empty<SecurityMessage>();
	}

	private static async Task<(DateTime? gapStart, DateTime? gapEnd)> GetNextGapAsync(DateTime[] storageDates, DateTime from, DateTime to, FillGapsDays days, CancellationToken cancellationToken)
	{
		using var drive = new DatesMarketDataDrive(storageDates);

		IFillGapsBehaviour behaviour = new StorageFillGapsBehaviour(drive, StorageFormats.Binary);

		return await behaviour.TryGetNextGapAsync(Helper.CreateSecurityId(), DataType.Ticks, from, to, days, cancellationToken);
	}

	private static async Task DrainLoopbacksAsync(IMessageAdapter adapter, Queue<Message> loopbacks, CancellationToken cancellationToken)
	{
		// Wait for async gap detection to complete
		await Helper.WaitUntilAsync(() => loopbacks.Count > 0, cancellationToken);

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
		online.From.AssertNull();
		online.To.AssertNull();
		online.FillGaps.AssertNull();
	}

	// Storage holds data only outside the requested range, so the whole range is one gap.
	// Dates outside [from, to] must not be counted as existing data for that range.
	[TestMethod]
	public async Task StorageBehaviour_NoDatesInRange_ReturnsWholeRange()
	{
		var from = new DateTime(2020, 1, 6, 10, 0, 0);
		var to = new DateTime(2020, 1, 8, 15, 0, 0);

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2019, 6, 3), new DateTime(2019, 6, 4)], from, to, FillGapsDays.All, CancellationToken);

		gapStart.AssertEqual(from, "gap must start exactly at the requested From when the range holds no data");
		gapEnd.AssertEqual(to, "gap must end exactly at the requested To when the range holds no data");
	}

	// Same setup with weekday filling: the single gap must still span the whole range
	// and must not be cut at the first Friday, since no day of the range has data.
	[TestMethod]
	public async Task StorageBehaviour_NoDatesInRange_NotCutAtFirstWeekend()
	{
		var from = new DateTime(2020, 1, 6);
		var to = new DateTime(2020, 1, 17);

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2019, 6, 3)], from, to, FillGapsDays.Weekdays, CancellationToken);

		gapStart.AssertEqual(from);
		gapEnd.AssertEqual(to, "the gap covers the whole range, so it must not stop at the Friday of its first week");
	}

	// A day present in the storage ends the gap: the window stops at the end of the day before it.
	[TestMethod]
	public async Task StorageBehaviour_StopsBeforeStoredDate()
	{
		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2019, 6, 3), new DateTime(2020, 1, 8)], new DateTime(2020, 1, 6), new DateTime(2020, 1, 12), FillGapsDays.All, CancellationToken);

		gapStart.AssertEqual(new DateTime(2020, 1, 6));
		gapEnd.AssertEqual(new DateTime(2020, 1, 7).EndOfDay());
	}

	// A partial first day that is already present in storage must not be requested again.
	[TestMethod]
	public async Task StorageBehaviour_FromAtEndOfStoredDay_DoesNotRerequestThatDay()
	{
		var from = new DateTime(2020, 1, 6).EndOfDay().AddDays(1);

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2020, 1, 7), new DateTime(2020, 1, 10)], from, new DateTime(2020, 1, 12), FillGapsDays.All, CancellationToken);

		gapStart.AssertEqual(new DateTime(2020, 1, 8), "07th has data, so the next gap starts at 08th");
		gapEnd.AssertEqual(new DateTime(2020, 1, 9).EndOfDay(), "10th has data, so the gap ends at the end of 09th");
	}

	[TestMethod]
	public async Task StorageBehaviour_WeekdayGapStopsAtFridayWhenLaterDataExists()
	{
		var from = new DateTime(2020, 1, 6);
		var to = new DateTime(2020, 1, 13).EndOfDay();

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2020, 1, 13)], from, to, FillGapsDays.Weekdays, CancellationToken);

		gapStart.AssertEqual(from);
		gapEnd.AssertEqual(new DateTime(2020, 1, 10).EndOfDay());
	}

	[TestMethod]
	public async Task StorageBehaviour_FirstMissingDayKeepsExactFromTime()
	{
		var from = new DateTime(2020, 1, 6, 10, 30, 0);
		var to = new DateTime(2020, 1, 10);

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2020, 1, 8)], from, to, FillGapsDays.All, CancellationToken);

		gapStart.AssertEqual(from);
		gapEnd.AssertEqual(new DateTime(2020, 1, 7).EndOfDay());
	}

	[TestMethod]
	public async Task StorageBehaviour_TrailingGapKeepsExactToTime()
	{
		var from = new DateTime(2020, 1, 6, 10, 0, 0);
		var to = new DateTime(2020, 1, 8, 15, 30, 0);

		var (gapStart, gapEnd) = await GetNextGapAsync([new DateTime(2020, 1, 6)], from, to, FillGapsDays.All, CancellationToken);

		gapStart.AssertEqual(new DateTime(2020, 1, 7));
		gapEnd.AssertEqual(to);
	}
}
