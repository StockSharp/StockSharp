namespace StockSharp.Algo
{
	using Ecng.Configuration;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
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
		/// Securities meta info storage.
		/// </summary>
		public static ISecurityStorage SecurityStorage => ConfigManager.GetService<ISecurityStorage>();
		
		/// <summary>
		/// Associations storage.
		/// </summary>
		public static ISecurityAssociationStorage AssociationStorage => ConfigManager.GetService<ISecurityAssociationStorage>();
		
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
		/// The position provider.
		/// </summary>
		public static IPositionProvider PositionProvider => ConfigManager.GetService<IPositionProvider>();
		
		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public static ISecurityProvider SecurityProvider => ConfigManager.GetService<ISecurityProvider>();

		/// <summary>
		/// The market data provider.
		/// </summary>
		public static IMarketDataProvider MarketDataProvider => ConfigManager.GetService<IMarketDataProvider>();

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
	}
}