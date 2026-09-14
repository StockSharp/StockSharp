namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

using StockSharp.Algo.Candles.Compression;

[TestClass]
public class StorageMessageAdapterTests : BaseTestClass
{
	private sealed class TestInnerAdapter : PassThroughMessageAdapter
	{
		public TestInnerAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public override async IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
		{
			yield return DataType.Level1;
		}
	}

	private sealed class TestStorageProcessor(StorageCoreSettings settings) : IStorageProcessor
	{
		public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

		public CandleBuilderProvider CandleBuilderProvider { get; } = new(new InMemoryExchangeInfoProvider());

		public int ResetCalls { get; private set; }

		public Func<MarketDataMessage, CancellationToken, IAsyncEnumerable<Message>> ProcessMarketDataImpl { get; set; }

		public void Reset() => ResetCalls++;

		public IAsyncEnumerable<Message> ProcessMarketData(MarketDataMessage message, CancellationToken cancellationToken)
		{
			return ProcessMarketDataImpl?.Invoke(message, cancellationToken) ?? Return(message, cancellationToken);

			static async IAsyncEnumerable<Message> Return(MarketDataMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return message;
			}
		}
	}

	private static async Task TouchDataAsync(LocalMarketDataDrive drive, SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, CancellationToken cancellationToken)
	{
		var storageDrive = drive.GetStorageDrive(securityId, dataType, format);

		using var stream = new MemoryStream();
		stream.Position = 0;
		await storageDrive.SaveStreamAsync(date, stream, cancellationToken);
	}

	[TestMethod]
	public async Task SendInMessageAsync_Reset_CallsProcessorReset_AndPassesToInner()
	{
		var token = CancellationToken;

		var settings = new StorageCoreSettings
		{
			StorageRegistry = new StorageRegistry(),
			Format = StorageFormats.Binary,
		};

		var processor = new TestStorageProcessor(settings);
		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new ResetMessage(), token);

		processor.ResetCalls.AssertEqual(1);
		output.Count.AssertEqual(1);
		output[0].Type.AssertEqual(MessageTypes.Reset);
	}

	[TestMethod]
	public async Task SendInMessageAsync_MarketData_UsesProcessorCallback_AndPassesToInner()
	{
		var token = CancellationToken;

		var settings = new StorageCoreSettings
		{
			StorageRegistry = new StorageRegistry(),
			Format = StorageFormats.Binary,
		};

		var processor = new TestStorageProcessor(settings)
		{
			ProcessMarketDataImpl = static (md, ct) => Process(md, ct)
		};

		static async IAsyncEnumerable<Message> Process(MarketDataMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return new SubscriptionResponseMessage { OriginalTransactionId = message.TransactionId };
			yield return message;
		}

		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			DataType2 = DataType.Ticks,
		};

		await adapter.SendInMessageAsync(mdMsg, token);

		output.Count.AssertEqual(2);
		output[0].AssertOfType<SubscriptionResponseMessage>();
		((SubscriptionResponseMessage)output[0]).OriginalTransactionId.AssertEqual(100);

		output[1].AssertOfType<MarketDataMessage>();
		((MarketDataMessage)output[1]).TransactionId.AssertEqual(100);
	}

	[TestMethod]
	public async Task SendInMessageAsync_MarketData_WhenProcessorReturnsNull_DoesNotPassToInner()
	{
		var token = CancellationToken;

		var settings = new StorageCoreSettings
		{
			StorageRegistry = new StorageRegistry(),
			Format = StorageFormats.Binary,
		};

		var processor = new TestStorageProcessor(settings)
		{
			ProcessMarketDataImpl = static (_, _) => AsyncEnumerable.Empty<Message>(),
		};

		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			DataType2 = DataType.Ticks,
		};

		await adapter.SendInMessageAsync(mdMsg, token);

		output.Count.AssertEqual(0);
	}

	private static (StorageCoreSettings settings, StorageProcessor processor, SecurityId secId) CreateRealEnv()
	{
		var fs = Helper.MemorySystem;
		var registry = fs.GetStorage(fs.GetSubTemp());

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry,
			Drive = registry.DefaultDrive,
			Format = StorageFormats.Binary,
			Mode = StorageModes.Incremental,
		};

		var processor = new StorageProcessor(settings, new CandleBuilderProvider(registry.ExchangeInfoProvider));

		return (settings, processor, new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test });
	}

	private static ExecutionMessage CreateTick(SecurityId secId, DateTime serverTime, long tradeId, decimal price) => new()
	{
		SecurityId = secId,
		DataTypeEx = DataType.Ticks,
		ServerTime = serverTime,
		TradeId = tradeId,
		TradePrice = price,
		TradeVolume = 1m,
	};

	// Storage covers the whole requested range: the caller gets the history plus
	// SubscriptionFinished, and nothing is asked of the inner adapter.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_HistoryCoversRange_FinishesAndDoesNotForward()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(2),
		}, token);

		var ticks = output.OfType<ExecutionMessage>().ToArray();
		ticks.Length.AssertEqual(3);
		ticks.Select(t => t.TradeId).AssertEqual(new long?[] { 1, 2, 3 });
		ticks.All(t => t.OriginalTransactionId == 100 && t.SubscriptionId == 100).AssertTrue();

		output.OfType<SubscriptionResponseMessage>().Count().AssertEqual(1);
		output.OfType<SubscriptionResponseMessage>().First().OriginalTransactionId.AssertEqual(100L);

		var finished = output.OfType<SubscriptionFinishedMessage>().ToArray();
		finished.Length.AssertEqual(1);
		finished[0].OriginalTransactionId.AssertEqual(100L);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);
	}

	// Storage covers only part of the request: the remainder is forwarded online
	// resuming at the last stored time with only the still missing Count.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_HistoryCoversPart_ForwardsRemainder()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 2, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var to = date.AddMinutes(10);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 200,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = to,
			Count = 5,
		}, token);

		output.OfType<ExecutionMessage>().Select(t => t.TradeId).AssertEqual(new long?[] { 1, 2, 3 });
		output.OfType<SubscriptionFinishedMessage>().Count().AssertEqual(0);

		var forwarded = inner.InMessages.OfType<MarketDataMessage>().ToArray();
		forwarded.Length.AssertEqual(1);

		var remainder = forwarded[0];
		remainder.IsSubscribe.AssertTrue();
		remainder.TransactionId.AssertEqual(200L);
		remainder.SecurityId.AssertEqual(secId);
		remainder.DataType2.AssertEqual(DataType.Ticks);
		remainder.From.AssertEqual(date.AddMinutes(2));
		remainder.To.AssertEqual(to);
		remainder.Count.AssertEqual(2L);
	}

	// A plain unsubscribe (no From/Count) for a subscription served entirely from
	// storage must be answered locally, not pushed at an inner adapter that never saw it.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_UnsubscribeAfterHistoryOnly_AnsweredLocally()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 300,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(1),
		}, token);

		output.OfType<SubscriptionFinishedMessage>().Count().AssertEqual(1);
		inner.InMessages.Clear();
		output.Clear();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 301,
			OriginalTransactionId = 300,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);

		var responses = output.OfType<SubscriptionResponseMessage>().ToArray();
		responses.Length.AssertEqual(1);
		responses[0].OriginalTransactionId.AssertEqual(301L);
		responses[0].Error.AssertNull();
	}

	// The same unsubscribe cloned from the original subscription (so it still carries
	// From/To) must be answered the same way — the bounds it drags along change nothing.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_ClonedUnsubscribeAfterHistoryOnly_AnsweredLocally()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 4, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 400,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(1),
		};

		await adapter.SendInMessageAsync(subscribe, token);

		output.OfType<SubscriptionFinishedMessage>().Count().AssertEqual(1);
		inner.InMessages.Clear();
		output.Clear();

		var unsubscribe = subscribe.TypedClone();
		unsubscribe.IsSubscribe = false;
		unsubscribe.OriginalTransactionId = 400;
		unsubscribe.TransactionId = 401;

		await adapter.SendInMessageAsync(unsubscribe, token);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);

		var responses = output.OfType<SubscriptionResponseMessage>().ToArray();
		responses.Length.AssertEqual(1);
		responses[0].OriginalTransactionId.AssertEqual(401L);
		responses[0].Error.AssertNull();
	}

	// A Count-only request is "the last Count records" and storage served every one of them:
	// the request is finished, and nothing is forwarded — least of all a request for zero
	// more records, which is what a remainder of an exhausted Count amounts to.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_LastCountWithoutTo_ServedInFull_FinishesAndDoesNotForward()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
			CreateTick(secId, date.AddMinutes(3), tradeId: 4, price: 103),
			CreateTick(secId, date.AddMinutes(4), tradeId: 5, price: 104),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 500,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			Count = 3,
		}, token);

		// The last three of the five stored, oldest first.
		output.OfType<ExecutionMessage>().Select(t => t.TradeId).AssertEqual(new long?[] { 3, 4, 5 });

		var finished = output.OfType<SubscriptionFinishedMessage>().ToArray();
		finished.Length.AssertEqual(1);
		finished[0].OriginalTransactionId.AssertEqual(500L);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);
	}

	// Count runs out while To is still far ahead: the caller asked for three records and has
	// three, so the request is complete on its own terms and nothing is forwarded.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_CountExhaustedBeforeTo_FinishesAndDoesNotForward()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
			CreateTick(secId, date.AddMinutes(3), tradeId: 4, price: 103),
			CreateTick(secId, date.AddMinutes(4), tradeId: 5, price: 104),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 550,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddDays(1),
			Count = 3,
		}, token);

		output.OfType<ExecutionMessage>().Select(t => t.TradeId).AssertEqual(new long?[] { 1, 2, 3 });

		var finished = output.OfType<SubscriptionFinishedMessage>().ToArray();
		finished.Length.AssertEqual(1);
		finished[0].OriginalTransactionId.AssertEqual(550L);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);
	}

	// Two trades share the last stored timestamp. Both belong to the history leg and each is
	// delivered exactly once, and the request handed on for the online leg resumes at that
	// same timestamp rather than at either trade individually.
	[TestMethod]
	public async Task SendInMessageAsync_MarketData_TiedTimestampsAtBoundary_DeliveredOnce_AndRemainderResumesThere()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 7, 1, 10, 0, 0, DateTimeKind.Utc);
		var boundary = date.AddMinutes(2);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, boundary, tradeId: 3, price: 102),
			CreateTick(secId, boundary, tradeId: 4, price: 103),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var to = date.AddMinutes(10);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 600,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = to,
		}, token);

		var ticks = output.OfType<ExecutionMessage>().ToArray();
		ticks.Select(t => t.TradeId).AssertEqual(new long?[] { 1, 2, 3, 4 });
		ticks.Select(t => t.TradeId).Distinct().Count().AssertEqual(4);
		ticks[3].ServerTime.AssertEqual(boundary);

		output.OfType<SubscriptionFinishedMessage>().Count().AssertEqual(0);

		var forwarded = inner.InMessages.OfType<MarketDataMessage>().ToArray();
		forwarded.Length.AssertEqual(1);
		forwarded[0].TransactionId.AssertEqual(600L);
		forwarded[0].From.AssertEqual(boundary);
		forwarded[0].To.AssertEqual(to);
		forwarded[0].Count.AssertNull();
	}

	// The chain a Connector builds: the meta-info wrapper sits above the per-adapter pipeline, and both
	// it and the storage wrapper inside that pipeline are handed the very same storage processor.
	private static StorageMetaInfoMessageAdapter CreateSharedProcessorChain(IStorageProcessor processor, RecordingPassThroughMessageAdapter inner)
		=> new(new StorageMessageAdapter(inner, processor),
			new InMemorySecurityStorage(), new InMemoryPositionStorage(),
			new InMemoryExchangeInfoProvider(), processor);

	/// <summary>
	/// A subscriber asked for a range of history once. Whichever wrappers a connector happens to stack
	/// between it and the storage is none of its business: a trade that is in the storage once has to
	/// arrive once, or every consumer that counts volume, builds candles or fills a blotter double-counts it.
	/// </summary>
	[TestMethod]
	public async Task HistoryIsDeliveredOnceWhenTwoStorageAdaptersShareOneProcessor()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 8, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = CreateSharedProcessorChain(processor, inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 700,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(10),
		}, token);

		output.OfType<ExecutionMessage>().Select(t => t.TradeId).AssertEqual(new long?[] { 1, 2, 3 });
	}

	/// <summary>
	/// One subscription is answered once. A second response on the same identifier tells the subscriber
	/// its request was accepted twice, and a subscriber that treats the response as the moment the
	/// subscription starts either restarts it or rejects the duplicate as a protocol error.
	/// </summary>
	[TestMethod]
	public async Task OneSubscriptionIsAnsweredOnceWhenTwoStorageAdaptersShareOneProcessor()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();

		var date = new DateTime(2025, 9, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = CreateSharedProcessorChain(processor, inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 800,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(10),
		}, token);

		output.OfType<SubscriptionResponseMessage>().Count(r => r.OriginalTransactionId == 800).AssertEqual(1);
	}

	[TestMethod]
	public async Task UpstreamFinished_ReleasesServedSubscription()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();
		var date = new DateTime(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date, tradeId: 1, price: 100),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = CreateSharedProcessorChain(processor, inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 900,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(1),
		};

		await adapter.SendInMessageAsync(subscription, token);
		output.OfType<ExecutionMessage>().Count().AssertEqual(1);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = 900 }, token);

		output.Clear();
		inner.InMessages.Clear();

		await adapter.SendInMessageAsync(subscription.TypedClone(), token);

		output.OfType<ExecutionMessage>().Count().AssertEqual(1,
			"A completed subscription id must not remain marked as already served.");
	}

	[TestMethod]
	public async Task UpstreamError_ReleasesServedSubscription()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();
		var date = new DateTime(2025, 11, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date, tradeId: 1, price: 100),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = CreateSharedProcessorChain(processor, inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 901,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(1),
		};

		await adapter.SendInMessageAsync(subscription, token);
		output.OfType<ExecutionMessage>().Count().AssertEqual(1);

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage
		{
			OriginalTransactionId = 901,
			Error = new InvalidOperationException("Rejected."),
		}, token);

		output.Clear();
		inner.InMessages.Clear();

		await adapter.SendInMessageAsync(subscription.TypedClone(), token);

		output.OfType<ExecutionMessage>().Count().AssertEqual(1,
			"A rejected subscription id must not remain marked as already served.");
	}

	[TestMethod]
	public async Task CompletedStorageSubscriptions_KeepOnlyRecentUnsubscribeIds()
	{
		var token = CancellationToken;
		var (settings, processor, secId) = CreateRealEnv();
		var date = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date, tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
		], token);

		var inner = new RecordingPassThroughMessageAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		for (var id = 1L; id <= 1_001; id++)
		{
			await adapter.SendInMessageAsync(new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = id,
				SecurityId = secId,
				DataType2 = DataType.Ticks,
				From = date,
				To = date.AddMinutes(1),
			}, token);

			output.Clear();
		}

		inner.InMessages.Count.AssertEqual(0);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2_001,
			OriginalTransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		inner.InMessages.OfType<MarketDataMessage>().Count(m => m.TransactionId == 2_001).AssertEqual(1,
			"The oldest completed id must be evicted instead of retained forever.");

		inner.InMessages.Clear();
		output.Clear();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2_002,
			OriginalTransactionId = 1_001,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		inner.InMessages.OfType<MarketDataMessage>().Count().AssertEqual(0);
		output.OfType<SubscriptionResponseMessage>()
			.Count(m => m.OriginalTransactionId == 2_002 && m.IsOk()).AssertEqual(1,
				"A recent completed subscription must still answer unsubscribe locally.");
	}

	[TestMethod]
	public async Task GetSupportedMarketDataTypes_IncludesDriveDataTypes()
	{
		var token = CancellationToken;

		var fs = Helper.MemorySystem;
		using var drive = new LocalMarketDataDrive(fs, fs.GetSubTemp());
		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = DateTime.UtcNow.Date;

		await TouchDataAsync(drive, secId, DataType.Ticks, StorageFormats.Binary, date, token);

		var settings = new StorageCoreSettings
		{
			StorageRegistry = new StorageRegistry(),
			Drive = drive,
			Format = StorageFormats.Binary,
		};

		var processor = new TestStorageProcessor(settings);
		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var supported = adapter.GetSupportedMarketDataTypesAsync(default, null, null);

		(await supported.ContainsAsync(DataType.Level1, cancellationToken: CancellationToken)).AssertTrue();
		(await supported.ContainsAsync(DataType.Ticks, cancellationToken: CancellationToken)).AssertTrue();
	}
}
