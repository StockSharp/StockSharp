namespace StockSharp.Algo.Basket;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Slippage;
using StockSharp.Algo.Storages;

/// <summary>
/// Configuration for building adapter wrapper pipeline.
/// </summary>
public record AdapterWrapperConfiguration
{
	/// <summary>
	/// Use <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	public bool SupportOffline { get; init; }

	/// <summary>
	/// Do not add extra adapters.
	/// </summary>
	public bool IgnoreExtraAdapters { get; init; }

	/// <summary>
	/// Use CandleBuilderMessageAdapter.
	/// </summary>
	public bool SupportCandlesCompression { get; init; }

	/// <summary>
	/// Use <see cref="OrderLogMessageAdapter"/>.
	/// </summary>
	public bool SupportBuildingFromOrderLog { get; init; }

	/// <summary>
	/// Use <see cref="OrderBookTruncateMessageAdapter"/>.
	/// </summary>
	public bool SupportOrderBookTruncate { get; init; }

	/// <summary>
	/// Use <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	public bool SupportLookupTracking { get; init; }

	/// <summary>
	/// Use <see cref="TransactionOrderingMessageAdapter"/>.
	/// </summary>
	public bool IsSupportTransactionLog { get; init; }

	/// <summary>
	/// Use <see cref="SubscriptionSecurityAllMessageAdapter"/>.
	/// </summary>
	public bool SupportSecurityAll { get; init; }

	/// <summary>
	/// Use <see cref="StorageMessageAdapter"/>.
	/// </summary>
	public bool SupportStorage { get; init; }

	/// <summary>
	/// Generate order book from Level1 data.
	/// </summary>
	public bool GenerateOrderBookFromLevel1 { get; init; }

	/// <summary>
	/// Use <see cref="Level1ExtendBuilderAdapter"/>.
	/// </summary>
	public bool Level1Extend { get; init; }

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	public bool IsRestoreSubscriptionOnErrorReconnect { get; init; }

	/// <summary>
	/// Suppress reconnecting errors.
	/// </summary>
	public bool SuppressReconnectingErrors { get; init; }

	/// <summary>
	/// CandleBuilderMessageAdapter.SendFinishedCandlesImmediatelly.
	/// </summary>
	public bool SendFinishedCandlesImmediatelly { get; init; }

	/// <summary>
	/// Use channels for message passing.
	/// </summary>
	public bool UseChannels { get; init; }

	/// <summary>
	/// Orders registration delay calculation manager.
	/// </summary>
	public ILatencyManager LatencyManager { get; init; }

	/// <summary>
	/// Slippage manager.
	/// </summary>
	public ISlippageManager SlippageManager { get; init; }

	/// <summary>
	/// The profit-loss manager.
	/// </summary>
	public IPnLManager PnLManager { get; init; }

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public ICommissionManager CommissionManager { get; init; }

	/// <summary>
	/// Security native identifier storage provider.
	/// </summary>
	public INativeIdStorageProvider NativeIdStorage { get; init; }

	/// <summary>
	/// Security identifier mappings storage provider.
	/// </summary>
	public ISecurityMappingStorageProvider MappingProvider { get; init; }

	/// <summary>
	/// Extended info storage.
	/// </summary>
	public IExtendedInfoStorage ExtendedInfoStorage { get; init; }

	/// <summary>
	/// Storage processor.
	/// </summary>
	public IStorageProcessor StorageProcessor { get; init; }

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public IStorageBuffer Buffer { get; init; }

	/// <summary>
	/// <see cref="IFillGapsBehaviour"/>
	/// </summary>
	public IFillGapsBehaviour FillGapsBehaviour { get; init; }

	/// <summary>
	/// Function to check if heartbeat is enabled for an adapter.
	/// </summary>
	public Func<IMessageAdapter, bool> IsHeartbeatOn { get; init; }

	/// <summary>
	/// Function to send error messages out.
	/// </summary>
	public Func<Exception, CancellationToken, ValueTask> SendOutErrorAsync { get; init; }

	/// <summary>
	/// Parent log receiver.
	/// </summary>
	public ILogReceiver Parent { get; init; }
}
