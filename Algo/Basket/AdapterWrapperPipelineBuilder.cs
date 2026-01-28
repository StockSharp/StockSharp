namespace StockSharp.Algo.Basket;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Positions;
using StockSharp.Algo.Slippage;

/// <summary>
/// Default implementation of <see cref="IAdapterWrapperPipelineBuilder"/>.
/// </summary>
public class AdapterWrapperPipelineBuilder : IAdapterWrapperPipelineBuilder
{
	/// <inheritdoc />
	public IMessageAdapter Build(IMessageAdapter adapter, AdapterWrapperConfiguration config)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
		if (config is null)
			throw new ArgumentNullException(nameof(config));

		var first = adapter;

		IMessageAdapter ApplyOwnInner(MessageAdapterWrapper a)
		{
			a.OwnInnerAdapter = first != adapter;
			return a;
		}

		if (config.IsHeartbeatOn?.Invoke(adapter) == true)
		{
			adapter = ApplyOwnInner(new HeartbeatMessageAdapter(adapter)
			{
				SuppressReconnectingErrors = config.SuppressReconnectingErrors,
				Parent = config.Parent,
			});
		}

		if (config.SupportOffline)
			adapter = ApplyOwnInner(new OfflineMessageAdapter(adapter));

		if (config.IgnoreExtraAdapters)
			return adapter;

		if (config.UseChannels && adapter.UseChannels())
		{
			adapter = ApplyOwnInner(new ChannelMessageAdapter(adapter,
				adapter.UseInChannel ? new AsyncMessageChannel(adapter) : new PassThroughMessageChannel(),
				adapter.UseOutChannel ? new InMemoryMessageChannel(new MessageByOrderQueue(), $"{adapter} Out", ex => config.SendOutErrorAsync?.Invoke(ex, default)) : new PassThroughMessageChannel()
			));
		}

		if (config.LatencyManager != null)
		{
			adapter = ApplyOwnInner(new LatencyMessageAdapter(adapter, config.LatencyManager.Clone()));
		}

		if (config.SlippageManager != null)
		{
			adapter = ApplyOwnInner(new SlippageMessageAdapter(adapter, config.SlippageManager.Clone()));
		}

		if (adapter.IsNativeIdentifiers)
		{
			adapter = ApplyOwnInner(new SecurityNativeIdMessageAdapter(adapter, config.NativeIdStorage));
		}

		if (config.MappingProvider != null)
		{
			adapter = ApplyOwnInner(new SecurityMappingMessageAdapter(adapter, config.MappingProvider));
		}

		if (config.SupportLookupTracking)
		{
			adapter = ApplyOwnInner(new LookupTrackingMessageAdapter(adapter, new LookupTrackingManagerState()));
		}

		if (config.IsSupportTransactionLog)
		{
			adapter = ApplyOwnInner(new TransactionOrderingMessageAdapter(adapter));
		}

		if (adapter.IsPositionsEmulationRequired is bool isPosEmu)
		{
			adapter = ApplyOwnInner(new PositionMessageAdapter(adapter, new PositionManager(isPosEmu, new PositionManagerState())));
		}

		if (adapter.IsSupportSubscriptions)
		{
			adapter = ApplyOwnInner(new SubscriptionOnlineMessageAdapter(adapter));
		}

		if (config.SupportSecurityAll)
		{
			adapter = ApplyOwnInner(new SubscriptionSecurityAllMessageAdapter(adapter));
		}

		if (config.GenerateOrderBookFromLevel1 && adapter.GetSupportedMarketDataTypes().Contains(DataType.Level1) && !adapter.GetSupportedMarketDataTypes().Contains(DataType.MarketDepth))
		{
			adapter = ApplyOwnInner(new Level1DepthBuilderAdapter(adapter));
		}

		if (config.Level1Extend && !adapter.GetSupportedMarketDataTypes().Contains(DataType.Level1))
		{
			adapter = ApplyOwnInner(new Level1ExtendBuilderAdapter(adapter));
		}

		if (config.PnLManager != null && !adapter.IsSupportExecutionsPnL)
		{
			adapter = ApplyOwnInner(new PnLMessageAdapter(adapter, config.PnLManager.Clone()));
		}

		if (config.CommissionManager != null)
		{
			adapter = ApplyOwnInner(new CommissionMessageAdapter(adapter, config.CommissionManager.Clone()));
		}

		if (adapter.IsSupportSubscriptions)
		{
			adapter = ApplyOwnInner(new SubscriptionMessageAdapter(adapter)
			{
				IsRestoreSubscriptionOnErrorReconnect = config.IsRestoreSubscriptionOnErrorReconnect,
			});
		}

		if (adapter.IsFullCandlesOnly)
		{
			adapter = ApplyOwnInner(new CandleHolderMessageAdapter(adapter));
		}

		if (config.SupportStorage && config.StorageProcessor?.Settings?.StorageRegistry != null)
		{
			adapter = ApplyOwnInner(new StorageMessageAdapter(adapter, config.StorageProcessor));
		}

		if (config.SupportBuildingFromOrderLog)
		{
			adapter = ApplyOwnInner(new OrderLogMessageAdapter(adapter));
		}

		if (config.SupportBuildingFromOrderLog || adapter.IsSupportOrderBookIncrements)
		{
			adapter = ApplyOwnInner(new OrderBookIncrementMessageAdapter(adapter));
		}

		if (config.SupportOrderBookTruncate)
		{
			adapter = ApplyOwnInner(new OrderBookTruncateMessageAdapter(adapter));
		}

		if (config.SupportCandlesCompression)
		{
			adapter = ApplyOwnInner(new CandleBuilderMessageAdapter(adapter, config.StorageProcessor?.CandleBuilderProvider)
			{
				SendFinishedCandlesImmediatelly = config.SendFinishedCandlesImmediatelly,
				Buffer = config.Buffer,
			});
		}

		if (config.ExtendedInfoStorage != null && !adapter.SecurityExtendedFields.IsEmpty())
		{
			adapter = ApplyOwnInner(new ExtendedInfoStorageMessageAdapter(adapter, config.ExtendedInfoStorage));
		}

		if (config.FillGapsBehaviour is not null)
		{
			adapter = new FillGapsMessageAdapter(adapter, config.FillGapsBehaviour);
		}

		return adapter;
	}
}
