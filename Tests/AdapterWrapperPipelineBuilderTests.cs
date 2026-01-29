namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Positions;
using StockSharp.Algo.Slippage;

/// <summary>
/// Tests for <see cref="AdapterWrapperPipelineBuilder"/>.
/// </summary>
[TestClass]
public class AdapterWrapperPipelineBuilderTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestPipelineAdapter : MessageAdapter
	{
		public TestPipelineAdapter(IdGenerator idGen = null)
			: base(idGen ?? new IncrementalIdGenerator())
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		public bool NativeIdentifiers { get; set; }
		public override bool IsNativeIdentifiers => NativeIdentifiers;

		public bool? PositionsEmulation { get; set; }
		public override bool? IsPositionsEmulationRequired => PositionsEmulation;

		public bool SupportSubscriptions { get; set; } = true;
		public override bool IsSupportSubscriptions => SupportSubscriptions;

		public bool FullCandlesOnly { get; set; }
		public override bool IsFullCandlesOnly => FullCandlesOnly;

		public bool SupportOrderBookIncrements { get; set; }
		public override bool IsSupportOrderBookIncrements => SupportOrderBookIncrements;

		public bool SupportExecutionsPnL { get; set; }
		public override bool IsSupportExecutionsPnL => SupportExecutionsPnL;

		public IEnumerable<(string, Type)> ExtendedFields { get; set; } = [];
		public override IEnumerable<(string, Type)> SecurityExtendedFields => ExtendedFields;

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
			=> default;

		public override IMessageAdapter Clone()
			=> new TestPipelineAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helpers

	private static AdapterWrapperPipelineBuilder CreateBuilder()
		=> new();

	private static AdapterWrapperConfiguration CreateDefaultConfig()
		=> new()
		{
			IsHeartbeatOn = _ => true,
		};

	private static IEnumerable<Type> GetWrapperTypes(IMessageAdapter adapter)
	{
		var current = adapter;
		while (current is IMessageAdapterWrapper wrapper)
		{
			yield return wrapper.GetType();
			current = wrapper.InnerAdapter;
		}
	}

	private static bool HasWrapper<T>(IMessageAdapter adapter) where T : IMessageAdapterWrapper
		=> GetWrapperTypes(adapter).Any(t => t == typeof(T));

	private static int CountWrappers(IMessageAdapter adapter)
		=> GetWrapperTypes(adapter).Count();

	#endregion

	#region Basic Pipeline Building

	[TestMethod]
	public async Task Build_WithIgnoreExtraAdapters_ReturnsOnlyHeartbeatAndOffline()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			IgnoreExtraAdapters = true,
			SupportOffline = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<HeartbeatMessageAdapter>(result));
		IsTrue(HasWrapper<OfflineMessageAdapter>(result));
		// Should only have 2 wrappers: Heartbeat and Offline
		AreEqual(2, CountWrappers(result));
	}

	[TestMethod]
	public async Task Build_WithIgnoreExtraAdapters_NoOffline_ReturnsOnlyHeartbeat()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			IgnoreExtraAdapters = true,
			SupportOffline = false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<HeartbeatMessageAdapter>(result));
		IsFalse(HasWrapper<OfflineMessageAdapter>(result));
		AreEqual(1, CountWrappers(result));
	}

	[TestMethod]
	public async Task Build_WithSupportOffline_IncludesOfflineAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<OfflineMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithoutSupportOffline_NoOfflineAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<OfflineMessageAdapter>(result));
	}

	#endregion

	#region Manager Adapters

	[TestMethod]
	public async Task Build_WithLatencyManager_IncludesLatencyAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			LatencyManager = new LatencyManager(new LatencyManagerState()),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<LatencyMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithoutLatencyManager_NoLatencyAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			LatencyManager = null,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<LatencyMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSlippageManager_IncludesSlippageAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SlippageManager = new SlippageManager(new SlippageManagerState()),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<SlippageMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithPnLManager_IncludesPnLAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportExecutionsPnL = false };
		var config = CreateDefaultConfig() with
		{
			PnLManager = new PnLManager(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<PnLMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithPnLManager_AdapterSupportsPnL_NoPnLAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportExecutionsPnL = true };
		var config = CreateDefaultConfig() with
		{
			PnLManager = new PnLManager(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<PnLMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithCommissionManager_IncludesCommissionAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			CommissionManager = new CommissionManager(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<CommissionMessageAdapter>(result));
	}

	#endregion

	#region Native/Mapping Adapters

	[TestMethod]
	public async Task Build_WithNativeIdentifiers_IncludesSecurityNativeIdAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { NativeIdentifiers = true };
		var config = CreateDefaultConfig() with
		{
			NativeIdStorage = new InMemoryNativeIdStorageProvider(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<SecurityNativeIdMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithoutNativeIdentifiers_NoSecurityNativeIdAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { NativeIdentifiers = false };
		var config = CreateDefaultConfig() with
		{
			NativeIdStorage = new InMemoryNativeIdStorageProvider(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<SecurityNativeIdMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithMappingProvider_IncludesSecurityMappingAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var mapping = new Mock<ISecurityMappingStorageProvider>();
		var config = CreateDefaultConfig() with
		{
			MappingProvider = mapping.Object,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<SecurityMappingMessageAdapter>(result));
	}

	#endregion

	#region Subscription Adapters

	[TestMethod]
	public async Task Build_WithSubscriptionSupport_IncludesSubscriptionAdapters()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportSubscriptions = true };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<SubscriptionOnlineMessageAdapter>(result));
		IsTrue(HasWrapper<SubscriptionMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithoutSubscriptionSupport_NoSubscriptionAdapters()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportSubscriptions = false };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<SubscriptionOnlineMessageAdapter>(result));
		IsFalse(HasWrapper<SubscriptionMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSupportSecurityAll_IncludesSubscriptionSecurityAllAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportSecurityAll = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<SubscriptionSecurityAllMessageAdapter>(result));
	}

	#endregion

	#region Candle Adapters

	[TestMethod]
	public async Task Build_WithCandlesCompression_IncludesCandleBuilderAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var storageProcessor = new StorageProcessor(new StorageCoreSettings(), new CandleBuilderProvider(new InMemoryExchangeInfoProvider()));
		var config = CreateDefaultConfig() with
		{
			SupportCandlesCompression = true,
			StorageProcessor = storageProcessor,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<CandleBuilderMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithFullCandlesOnly_IncludesCandleHolderAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { FullCandlesOnly = true };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<CandleHolderMessageAdapter>(result));
	}

	#endregion

	#region Order Book Adapters

	[TestMethod]
	public async Task Build_WithSupportBuildingFromOrderLog_IncludesOrderLogAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportBuildingFromOrderLog = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<OrderLogMessageAdapter>(result));
		IsTrue(HasWrapper<OrderBookIncrementMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSupportOrderBookIncrements_IncludesOrderBookIncrementAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportOrderBookIncrements = true };
		var config = CreateDefaultConfig() with
		{
			SupportBuildingFromOrderLog = false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<OrderBookIncrementMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSupportOrderBookTruncate_IncludesOrderBookTruncateAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOrderBookTruncate = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<OrderBookTruncateMessageAdapter>(result));
	}

	#endregion

	#region Other Adapters

	[TestMethod]
	public async Task Build_WithSupportLookupTracking_IncludesLookupTrackingAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportLookupTracking = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<LookupTrackingMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithIsSupportTransactionLog_IncludesTransactionOrderingAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			IsSupportTransactionLog = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<TransactionOrderingMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithPositionsEmulationRequired_IncludesPositionAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { PositionsEmulation = true };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<PositionMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithFillGapsBehaviour_IncludesFillGapsAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var fillGaps = new Mock<IFillGapsBehaviour>();
		var config = CreateDefaultConfig() with
		{
			FillGapsBehaviour = fillGaps.Object,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<FillGapsMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithExtendedInfoStorage_IncludesExtendedInfoStorageAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { ExtendedFields = [("Field1", typeof(string))] };
		var extStorage = new Mock<IExtendedInfoStorage>();
		var config = CreateDefaultConfig() with
		{
			ExtendedInfoStorage = extStorage.Object,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<ExtendedInfoStorageMessageAdapter>(result));
	}

	#endregion

	#region OwnInnerAdapter Flag

	[TestMethod]
	public async Task Build_OwnInnerAdapterFlagSetCorrectly()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = true,
			LatencyManager = new LatencyManager(new LatencyManagerState()),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		// The outermost wrapper should have OwnInnerAdapter = true (it owns the next wrapper)
		// The first wrapper (directly wrapping the inner) should have OwnInnerAdapter = false
		IsNotNull(result);

		IMessageAdapter current = result;
		var wrapperCount = 0;
		while (current is MessageAdapterWrapper wrapper)
		{
			wrapperCount++;
			if (wrapper.InnerAdapter is MessageAdapterWrapper)
			{
				// This wrapper owns another wrapper, so OwnInnerAdapter should be true
				IsTrue(wrapper.OwnInnerAdapter, $"Wrapper {wrapper.GetType().Name} should own inner adapter");
			}
			else
			{
				// This is the innermost wrapper wrapping the original adapter
				IsFalse(wrapper.OwnInnerAdapter, $"Innermost wrapper {wrapper.GetType().Name} should not own inner adapter");
			}
			current = wrapper.InnerAdapter;
		}

		IsTrue(wrapperCount > 1, "Should have multiple wrappers for this test");
	}

	#endregion

	#region Wrapper Order

	[TestMethod]
	public async Task Build_CorrectWrapperOrder_HeartbeatFirst()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		// First wrapper from outside should be FillGaps (if enabled), then other wrappers,
		// and Heartbeat should be closest to the inner adapter (first added)
		var wrapperTypes = GetWrapperTypes(result).ToList();

		// The HeartbeatMessageAdapter should be at the end of the list (closest to inner adapter)
		AreEqual(typeof(HeartbeatMessageAdapter), wrapperTypes.Last());
	}

	[TestMethod]
	public async Task Build_CorrectWrapperOrder_OfflineAfterHeartbeat()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = true,
			IgnoreExtraAdapters = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		var wrapperTypes = GetWrapperTypes(result).ToList();

		// With IgnoreExtraAdapters, we should only have Heartbeat and Offline
		AreEqual(2, wrapperTypes.Count);

		// Offline should be first (outermost), Heartbeat should be last (innermost)
		AreEqual(typeof(OfflineMessageAdapter), wrapperTypes[0]);
		AreEqual(typeof(HeartbeatMessageAdapter), wrapperTypes[1]);
	}

	#endregion

	#region Null/Empty Configuration

	[TestMethod]
	public async Task Build_NullAdapter_ThrowsArgumentNullException()
	{
		var builder = CreateBuilder();
		var config = CreateDefaultConfig();

		await ThrowsExactlyAsync<ArgumentNullException>(async () => await builder.BuildAsync(null, config, CancellationToken));
	}

	[TestMethod]
	public async Task Build_NullConfig_ThrowsArgumentNullException()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();

		await ThrowsExactlyAsync<ArgumentNullException>(async () => await builder.BuildAsync(inner, null, CancellationToken));
	}

	[TestMethod]
	public async Task Build_HeartbeatOff_NoHeartbeatAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = new AdapterWrapperConfiguration
		{
			IsHeartbeatOn = _ => false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<HeartbeatMessageAdapter>(result));
	}

	#endregion
}
