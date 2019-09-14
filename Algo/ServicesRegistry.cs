namespace StockSharp.Algo
{
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Interop;

	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Services registry.
	/// </summary>
	public static class ServicesRegistry
	{
		private static readonly InMemoryExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
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
		/// The storage of market data.
		/// </summary>
		public static IStorageRegistry StorageRegistry => ConfigManager.GetService<IStorageRegistry>();
		
		/// <summary>
		/// Connector.
		/// </summary>
		public static Connector Connector => ConfigManager.GetService<Connector>();
		
		/// <summary>
		/// Connector.
		/// </summary>
		public static IConnector IConnector => ConfigManager.GetService<IConnector>();
		
		/// <summary>
		/// Log manager.
		/// </summary>
		public static LogManager LogManager => ConfigManager.TryGetService<LogManager>();

		/// <summary>
		/// The storage of trade objects.
		/// </summary>
		public static IEntityRegistry EntityRegistry => ConfigManager.GetService<IEntityRegistry>();
		
		/// <summary>
		/// Security native identifier storage.
		/// </summary>
		public static INativeIdStorage NativeIdStorage => ConfigManager.GetService<INativeIdStorage>();
		
		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public static IExtendedInfoStorage ExtendedInfoStorage => ConfigManager.GetService<IExtendedInfoStorage>();

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public static IExtendedInfoStorage TryExtendedInfoStorage => ConfigManager.TryGetService<IExtendedInfoStorage>();

		/// <summary>
		/// The client for access to the StockSharp notification service.
		/// </summary>
		public static INotificationClient NotificationClient => ConfigManager.GetService<INotificationClient>();

		/// <summary>
		/// The client for access to the StockSharp notification service.
		/// </summary>
		public static INotificationClient TryNotificationClient => ConfigManager.GetService<INotificationClient>();

		/// <summary>
		/// The client for access to the service of work with files and documents.
		/// </summary>
		public static IFileClient FileClient => ConfigManager.GetService<IFileClient>();

		/// <summary>
		/// The client for access to the registration service.
		/// </summary>
		public static IProfileClient ProfileClient => ConfigManager.GetService<IProfileClient>();

		/// <summary>
		/// The client for access to <see cref="IStrategyService"/>.
		/// </summary>
		public static IStrategyClient StrategyClient => ConfigManager.GetService<IStrategyClient>();

		/// <summary>
		/// The client for access to the StockSharp authentication service.
		/// </summary>
		public static IAuthenticationClient AuthenticationClient => ConfigManager.GetService<IAuthenticationClient>();

		/// <summary>
		/// The client for access to the StockSharp authentication service.
		/// </summary>
		public static IAuthenticationClient TryAuthenticationClient => ConfigManager.TryGetService<IAuthenticationClient>();

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
		/// Compiler service.
		/// </summary>
		public static ICompilerService CompilerService => ConfigManager.GetService<ICompilerService>();

		/// <summary>
		/// Compiler service.
		/// </summary>
		public static ICompilerService TryCompilerService => ConfigManager.TryGetService<ICompilerService>();

		/// <summary>
		/// Excel provider.
		/// </summary>
		public static IExcelWorkerProvider ExcelProvider => ConfigManager.TryGetService<IExcelWorkerProvider>();

		/// <summary>
		/// Snapshot storage registry.
		/// </summary>
		public static SnapshotRegistry SnapshotRegistry => ConfigManager.GetService<SnapshotRegistry>();
		
		/// <summary>
		/// News provider.
		/// </summary>
		public static INewsProvider NewsProvider => ConfigManager.GetService<INewsProvider>();

		/// <summary>
		/// The risks control manager.
		/// </summary>
		public static IRiskManager RiskManager => ConfigManager.GetService<IRiskManager>();
	}
}