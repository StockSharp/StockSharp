using StockSharp.Algo.Candles.Compression;

namespace StockSharp.Tests;

[TestClass]
public class StorageMessageAdapterTests : BaseTestClass
{
	private sealed class TestInnerAdapter : PassThroughMessageAdapter
	{
		public TestInnerAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
			=> [DataType.Level1];
	}

	private sealed class TestStorageProcessor(StorageCoreSettings settings) : IStorageProcessor
	{
		public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

		public CandleBuilderProvider CandleBuilderProvider { get; } = new(new InMemoryExchangeInfoProvider());

		public int ResetCalls { get; private set; }

		public Func<MarketDataMessage, Action<Message>, MarketDataMessage> ProcessMarketDataImpl { get; set; }

		public void Reset() => ResetCalls++;

		public MarketDataMessage ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage)
		{
			if (ProcessMarketDataImpl != null)
				return ProcessMarketDataImpl(message, newOutMessage);

			return message;
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
		adapter.NewOutMessage += output.Add;

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
			ProcessMarketDataImpl = (md, newOut) =>
			{
				newOut(new SubscriptionResponseMessage { OriginalTransactionId = md.TransactionId });
				return md;
			}
		};

		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

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
			ProcessMarketDataImpl = (_, _) => null,
		};

		var inner = new TestInnerAdapter();
		var adapter = new StorageMessageAdapter(inner, processor);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

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

	[TestMethod]
	public async Task GetSupportedMarketDataTypes_IncludesDriveDataTypes()
	{
		var token = CancellationToken;

		using var drive = new LocalMarketDataDrive(Helper.GetSubTemp());
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

		var supported = adapter.GetSupportedMarketDataTypes(default, null, null).ToArray();

		supported.Contains(DataType.Level1).AssertTrue();
		supported.Contains(DataType.Ticks).AssertTrue();
	}
}
