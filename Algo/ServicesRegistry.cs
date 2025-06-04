namespace StockSharp.Algo;

using Ecng.Compilation;
using Ecng.Configuration;
using Ecng.Interop;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Candles.Patterns;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Strategies.Reporting;

/// <summary>
/// Services registry.
/// </summary>
public static class ServicesRegistry
{
	private static readonly InMemoryExchangeInfoProvider _exchangeInfoProvider = new();

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	/// <returns>Exchanges and trading boards provider.</returns>
	public static IExchangeInfoProvider EnsureGetExchangeInfoProvider() => ConfigManager.TryGetService<IExchangeInfoProvider>() ?? _exchangeInfoProvider;

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	public static IExchangeInfoProvider ExchangeInfoProvider => ConfigManager.GetService<IExchangeInfoProvider>();

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	public static IExchangeInfoProvider TryExchangeInfoProvider => ConfigManager.TryGetService<IExchangeInfoProvider>();

	/// <summary>
	/// Securities meta info storage.
	/// </summary>
	public static ISecurityStorage SecurityStorage => ConfigManager.GetService<ISecurityStorage>();

	/// <summary>
	/// Security identifier mappings storage.
	/// </summary>
	public static ISecurityMappingStorage MappingStorage => ConfigManager.GetService<ISecurityMappingStorage>();

	/// <summary>
	/// Position storage.
	/// </summary>
	public static IPositionStorage PositionStorage => ConfigManager.GetService<IPositionStorage>();

	/// <summary>
	/// The provider of information about portfolios.
	/// </summary>
	public static IPortfolioProvider PortfolioProvider => ConfigManager.GetService<IPortfolioProvider>();

	/// <summary>
	/// The provider of information about portfolios.
	/// </summary>
	public static IPortfolioProvider TryPortfolioProvider => ConfigManager.TryGetService<IPortfolioProvider>();

	/// <summary>
	/// The position provider.
	/// </summary>
	public static IPositionProvider PositionProvider => ConfigManager.GetService<IPositionProvider>();

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public static ISecurityProvider SecurityProvider => ConfigManager.GetService<ISecurityProvider>();

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public static ISecurityProvider TrySecurityProvider => ConfigManager.TryGetService<ISecurityProvider>();

	/// <summary>
	/// The market data provider.
	/// </summary>
	public static IMarketDataProvider MarketDataProvider => ConfigManager.GetService<IMarketDataProvider>();

	/// <summary>
	/// The market data provider.
	/// </summary>
	public static IMarketDataProvider TryMarketDataProvider => ConfigManager.TryGetService<IMarketDataProvider>();

	/// <summary>
	/// Subscription provider.
	/// </summary>
	public static ISubscriptionProvider SubscriptionProvider => ConfigManager.GetService<ISubscriptionProvider>();

	/// <summary>
	/// Subscription provider.
	/// </summary>
	public static ISubscriptionProvider TrySubscriptionProvider => ConfigManager.TryGetService<ISubscriptionProvider>();

	/// <summary>
	/// The storage of market data.
	/// </summary>
	public static IStorageRegistry StorageRegistry => ConfigManager.GetService<IStorageRegistry>();

	/// <summary>
	/// The storage of market data.
	/// </summary>
	public static IStorageRegistry TryStorageRegistry => ConfigManager.TryGetService<IStorageRegistry>();

	/// <summary>
	/// Connector.
	/// </summary>
	public static Connector Connector => ConfigManager.GetService<Connector>();

	/// <summary>
	/// Connector.
	/// </summary>
	[Obsolete("Use Connector property.")]
	// ReSharper disable InconsistentNaming
	public static IConnector IConnector => ConfigManager.GetService<IConnector>();
	// ReSharper restore InconsistentNaming

	/// <summary>
	/// Log manager.
	/// </summary>
	public static LogManager LogManager => LogManager.Instance;

	/// <summary>
	/// Log manager.
	/// </summary>
	public static LogManager EnsureLogManager => LogManager ?? throw new InvalidOperationException();

	/// <summary>
	/// The storage of trade objects.
	/// </summary>
	public static IEntityRegistry EntityRegistry => ConfigManager.GetService<IEntityRegistry>();

	/// <summary>
	/// The storage of trade objects.
	/// </summary>
	public static IEntityRegistry TryEntityRegistry => ConfigManager.TryGetService<IEntityRegistry>();

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public static INativeIdStorage TryNativeIdStorage => ConfigManager.TryGetService<INativeIdStorage>();

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public static INativeIdStorage NativeIdStorage => ConfigManager.GetService<INativeIdStorage>();

	/// <summary>
	/// Extended info storage.
	/// </summary>
	public static IExtendedInfoStorage ExtendedInfoStorage => ConfigManager.GetService<IExtendedInfoStorage>();

	/// <summary>
	/// Extended info storage.
	/// </summary>
	public static IExtendedInfoStorage TryExtendedInfoStorage => ConfigManager.TryGetService<IExtendedInfoStorage>();

	/// <summary>
	/// The message adapter's provider.
	/// </summary>
	public static IMessageAdapterProvider AdapterProvider => ConfigManager.GetService<IMessageAdapterProvider>();

	/// <summary>
	/// The message adapter's provider.
	/// </summary>
	public static IMessageAdapterProvider TryAdapterProvider => ConfigManager.TryGetService<IMessageAdapterProvider>();

	/// <summary>
	/// The portfolio based message adapter's provider.
	/// </summary>
	public static IPortfolioMessageAdapterProvider PortfolioAdapterProvider => ConfigManager.GetService<IPortfolioMessageAdapterProvider>();

	/// <summary>
	/// The security based message adapter's provider.
	/// </summary>
	public static ISecurityMessageAdapterProvider SecurityAdapterProvider => ConfigManager.GetService<ISecurityMessageAdapterProvider>();

	/// <summary>
	/// <see cref="IMarketDataDrive"/> cache.
	/// </summary>
	public static DriveCache DriveCache => ConfigManager.GetService<DriveCache>();

	/// <summary>
	/// <see cref="IMarketDataDrive"/> cache.
	/// </summary>
	public static DriveCache TryDriveCache => ConfigManager.TryGetService<DriveCache>();

	/// <summary>
	/// <see cref="ICompiler"/>.
	/// </summary>
	public static ICompiler Compiler => ConfigManager.GetService<ICompiler>();

	/// <summary>
	/// <see cref="ICompiler"/>.
	/// </summary>
	public static ICompiler TryCompiler => ConfigManager.TryGetService<ICompiler>();

	/// <summary>
	/// <see cref="CompilerProvider"/>.
	/// </summary>
	public static CompilerProvider CompilerProvider => ConfigManager.GetService<CompilerProvider>();

	/// <summary>
	/// <see cref="CompilerProvider"/>.
	/// </summary>
	public static CompilerProvider TryCompilerProvider => ConfigManager.TryGetService<CompilerProvider>();

	/// <summary>
	/// Excel provider.
	/// </summary>
	public static IExcelWorkerProvider ExcelProvider => ConfigManager.GetService<IExcelWorkerProvider>();

	/// <summary>
	/// Excel provider.
	/// </summary>
	public static IExcelWorkerProvider TryExcelProvider => ConfigManager.TryGetService<IExcelWorkerProvider>();

	/// <summary>
	/// Snapshot storage registry.
	/// </summary>
	public static SnapshotRegistry SnapshotRegistry => ConfigManager.GetService<SnapshotRegistry>();

	/// <summary>
	/// The risks control manager.
	/// </summary>
	public static IRiskManager RiskManager => ConfigManager.GetService<IRiskManager>();

	/// <summary>
	/// The risks control manager.
	/// </summary>
	public static IRiskManager TryRiskManager => ConfigManager.TryGetService<IRiskManager>();

	/// <summary>
	/// <see cref="IReportGeneratorProvider"/>.
	/// </summary>
	public static IReportGeneratorProvider ReportGeneratorProvider => ConfigManager.GetService<IReportGeneratorProvider>();

	/// <summary>
	/// <see cref="ICandlePatternProvider"/>
	/// </summary>
	public static ICandlePatternProvider CandlePatternProvider => ConfigManager.TryGetService<ICandlePatternProvider>();

	/// <summary>
	/// <see cref="ICandlePatternProvider"/>
	/// </summary>
	public static ICandlePatternProvider TryCandlePatternProvider => ConfigManager.TryGetService<ICandlePatternProvider>();

	/// <summary>
	/// <see cref="CandleBuilderProvider"/>
	/// </summary>
	public static CandleBuilderProvider CandleBuilderProvider => ConfigManager.GetService<CandleBuilderProvider>();

	/// <summary>
	/// <see cref="ICompilerCache"/>
	/// </summary>
	public static ICompilerCache TryCompilerCache => ConfigManager.TryGetService<ICompilerCache>();

	/// <summary>
	/// <see cref="ICompilerCache"/>
	/// </summary>
	public static ICompilerCache CompilerCache => ConfigManager.GetService<ICompilerCache>();

	/// <summary>
	/// <see cref="IRiskRuleProvider"/>
	/// </summary>
	public static IRiskRuleProvider TryRiskRuleProvider => ConfigManager.TryGetService<IRiskRuleProvider>();

	/// <summary>
	/// <see cref="IRiskRuleProvider"/>
	/// </summary>
	public static IRiskRuleProvider RiskRuleProvider => ConfigManager.GetService<IRiskRuleProvider>();

	/// <summary>
	/// <see cref="ICommissionRuleProvider"/>
	/// </summary>
	public static ICommissionRuleProvider TryCommissionRuleProvider => ConfigManager.TryGetService<ICommissionRuleProvider>();

	/// <summary>
	/// <see cref="ICommissionRuleProvider"/>
	/// </summary>
	public static ICommissionRuleProvider CommissionRuleProvider => ConfigManager.GetService<ICommissionRuleProvider>();

	/// <summary>
	/// <see cref="ICustomTypeDescriptorProvider"/>
	/// </summary>
	public static ICustomTypeDescriptorProvider TryCustomTypeDescriptorProvider => ConfigManager.TryGetService<ICustomTypeDescriptorProvider>();

	/// <summary>
	/// <see cref="ICustomTypeDescriptorProvider"/>
	/// </summary>
	public static ICustomTypeDescriptorProvider CustomTypeDescriptorProvider => ConfigManager.GetService<ICustomTypeDescriptorProvider>();
}
