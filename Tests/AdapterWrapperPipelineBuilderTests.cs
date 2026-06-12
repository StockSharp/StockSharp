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
	public async Task Build_WithSuppressOrderBookIncrements_NoOrderBookIncrementAdapter()
	{
		var builder = CreateBuilder();
		// Even though the inner adapter reports IsSupportOrderBookIncrements, SuppressOrderBookIncrements
		// must veto the OrderBookIncrementMessageAdapter (builder line:
		// "!config.SuppressOrderBookIncrements && (SupportBuildingFromOrderLog || IsSupportOrderBookIncrements)").
		var inner = new TestPipelineAdapter { SupportOrderBookIncrements = true };
		var config = CreateDefaultConfig() with
		{
			SuppressOrderBookIncrements = true,
			SupportBuildingFromOrderLog = false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<OrderBookIncrementMessageAdapter>(result));
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
	public async Task Build_WithPositionsEmulationFalse_IncludesPositionAdapter()
	{
		var builder = CreateBuilder();
		// The builder branch is "IsPositionsEmulationRequired is bool isPosEmu": a non-null
		// false value still matches, so the PositionMessageAdapter must be added (it is then
		// configured with emulation disabled). This distinguishes false (add) from null (skip).
		var inner = new TestPipelineAdapter { PositionsEmulation = false };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<PositionMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithPositionsEmulationNull_NoPositionAdapter()
	{
		var builder = CreateBuilder();
		// null means "not applicable": the pattern "is bool" fails, so no PositionMessageAdapter.
		var inner = new TestPipelineAdapter { PositionsEmulation = null };
		var config = CreateDefaultConfig();

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<PositionMessageAdapter>(result));
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

		// FillGaps is added last, so it is the outermost wrapper of the whole pipeline.
		// Here it wraps the Heartbeat wrapper (IsHeartbeatOn => true), i.e. it owns another
		// MessageAdapterWrapper. Per the OwnInnerAdapter contract (IMessageAdapterWrapper:
		// "if (OwnInnerAdapter) InnerAdapter.Dispose()"), the consumer disposes only the
		// outermost wrapper and relies on the cascade. Therefore the FillGaps wrapper MUST
		// own its inner wrapper, otherwise the inner pipeline (Heartbeat timers, channels, ...)
		// leaks on Dispose. Assert the correct/contracted behavior.
		var fillGapsWrapper = (FillGapsMessageAdapter)result;
		IsTrue(fillGapsWrapper.InnerAdapter is MessageAdapterWrapper, "FillGaps should wrap another wrapper in this configuration");
		IsTrue(fillGapsWrapper.OwnInnerAdapter, "FillGaps wrapper must own its inner wrapper so Dispose cascades through the pipeline");
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

	#region Previously Uncovered Branches

	[TestMethod]
	public async Task Build_WithUseChannels_IncludesChannelAdapter()
	{
		var builder = CreateBuilder();
		// TestPipelineAdapter (MessageAdapter) reports UseInChannel/UseOutChannel == true by default,
		// so adapter.UseChannels() is true; together with config.UseChannels the ChannelMessageAdapter
		// must be added. Construction only wires channel event handlers, it does not start any loop.
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			UseChannels = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<ChannelMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithoutUseChannels_NoChannelAdapter()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			UseChannels = false,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<ChannelMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithGenerateOrderBookFromLevel1_IncludesLevel1DepthBuilderAdapter()
	{
		var builder = CreateBuilder();
		// Branch requires Level1 supported AND MarketDepth NOT supported. The default test adapter
		// supports both, so remove MarketDepth to satisfy the condition.
		var inner = new TestPipelineAdapter();
		inner.RemoveSupportedMarketDataType(DataType.MarketDepth);
		var config = CreateDefaultConfig() with
		{
			GenerateOrderBookFromLevel1 = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<Level1DepthBuilderAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithGenerateOrderBookFromLevel1_MarketDepthSupported_NoLevel1DepthBuilderAdapter()
	{
		var builder = CreateBuilder();
		// MarketDepth is supported (default), so the builder must NOT add Level1DepthBuilderAdapter
		// even though GenerateOrderBookFromLevel1 is requested.
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			GenerateOrderBookFromLevel1 = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<Level1DepthBuilderAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithLevel1Extend_IncludesLevel1ExtendBuilderAdapter()
	{
		var builder = CreateBuilder();
		// Branch requires Level1 NOT supported. The default test adapter supports Level1, so remove it.
		var inner = new TestPipelineAdapter();
		inner.RemoveSupportedMarketDataType(DataType.Level1);
		var config = CreateDefaultConfig() with
		{
			Level1Extend = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<Level1ExtendBuilderAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithLevel1Extend_Level1Supported_NoLevel1ExtendBuilderAdapter()
	{
		var builder = CreateBuilder();
		// Level1 is supported (default), so Level1ExtendBuilderAdapter must NOT be added.
		var inner = new TestPipelineAdapter();
		var config = CreateDefaultConfig() with
		{
			Level1Extend = true,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<Level1ExtendBuilderAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSupportStorage_IncludesStorageAdapter()
	{
		var builder = CreateBuilder();
		// StorageMessageAdapter is added only when SupportStorage is set AND the processor has a
		// non-null StorageRegistry. Provide a real in-memory registry to satisfy both.
		var inner = new TestPipelineAdapter();
		var registry = new StorageRegistry();
		var storageProcessor = new StorageProcessor(new StorageCoreSettings { StorageRegistry = registry }, new CandleBuilderProvider(new InMemoryExchangeInfoProvider()));
		var config = CreateDefaultConfig() with
		{
			SupportStorage = true,
			StorageProcessor = storageProcessor,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsTrue(HasWrapper<StorageMessageAdapter>(result));
	}

	[TestMethod]
	public async Task Build_WithSupportStorage_NoStorageRegistry_NoStorageAdapter()
	{
		var builder = CreateBuilder();
		// SupportStorage requested but the processor reports no StorageRegistry: the adapter must
		// NOT be added (guards against a NullReferenceException-prone configuration).
		var inner = new TestPipelineAdapter();
		var storageProcessor = new StorageProcessor(new StorageCoreSettings(), new CandleBuilderProvider(new InMemoryExchangeInfoProvider()));
		var config = CreateDefaultConfig() with
		{
			SupportStorage = true,
			StorageProcessor = storageProcessor,
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		IsNotNull(result);
		IsFalse(HasWrapper<StorageMessageAdapter>(result));
	}

	#endregion

	#region OwnInnerAdapter Flag

	[TestMethod]
	public async Task Build_OwnInnerAdapterFlagSetCorrectly()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter();
		var fillGaps = new Mock<IFillGapsBehaviour>();
		var config = CreateDefaultConfig() with
		{
			SupportOffline = true,
			LatencyManager = new LatencyManager(new LatencyManagerState()),
			// FillGaps is the only builder branch that does NOT call ApplyOwnInner, so it
			// exercises the outermost wrapper whose OwnInnerAdapter flag must be set. Including
			// it here ensures the contract is verified for every branch, not just the ones that
			// already call ApplyOwnInner.
			FillGapsBehaviour = fillGaps.Object,
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

		// Heartbeat is added first by the builder, so it must be closest to the inner adapter
		// (last in the outer->inner list returned by GetWrapperTypes). No FillGaps in this config.
		var wrapperTypes = GetWrapperTypes(result).ToList();

		// The HeartbeatMessageAdapter should be at the end of the list (closest to inner adapter)
		AreEqual(typeof(HeartbeatMessageAdapter), wrapperTypes.Last());
	}

	[TestMethod]
	public async Task Build_RelativeWrapperOrder_IsStable()
	{
		var builder = CreateBuilder();
		var inner = new TestPipelineAdapter { SupportSubscriptions = true, SupportExecutionsPnL = false };
		var config = CreateDefaultConfig() with
		{
			PnLManager = new PnLManager(),
			CommissionManager = new CommissionManager(),
		};

		var result = await builder.BuildAsync(inner, config, CancellationToken);

		// GetWrapperTypes returns the chain from the outermost wrapper to the innermost.
		// Wrappers are added inner-to-outer in builder source order, so a wrapper added later
		// sits more to the outside (smaller index in this list).
		var wrapperTypes = GetWrapperTypes(result).ToList();

		int IndexOf<T>()
		{
			var idx = wrapperTypes.IndexOf(typeof(T));
			IsTrue(idx >= 0, $"{typeof(T).Name} expected in the pipeline");
			return idx;
		}

		// SubscriptionMessageAdapter is added after SubscriptionOnlineMessageAdapter, so it must be
		// the more outer (closer to the consumer) of the two subscription wrappers.
		IsTrue(IndexOf<SubscriptionMessageAdapter>() < IndexOf<SubscriptionOnlineMessageAdapter>(),
			"SubscriptionMessageAdapter must wrap (be outside) SubscriptionOnlineMessageAdapter");

		// CommissionMessageAdapter is added after PnLMessageAdapter, so Commission is more outer than PnL.
		IsTrue(IndexOf<CommissionMessageAdapter>() < IndexOf<PnLMessageAdapter>(),
			"CommissionMessageAdapter must wrap (be outside) PnLMessageAdapter");

		// Heartbeat is added first, so it stays innermost (last in the outer->inner list).
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
