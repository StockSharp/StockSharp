namespace StockSharp.Messages;

using System.Security;

/// <summary>
/// Base message adapter interface which convert messages <see cref="Message"/> to native commands and back.
/// </summary>
public interface IMessageAdapter : IMessageChannel, IPersistable, ILogReceiver
{
	/// <summary>
	/// Transaction id generator.
	/// </summary>
	IdGenerator TransactionIdGenerator { get; }

	/// <summary>
	/// Possible supported by adapter message types.
	/// </summary>
	IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; }

	/// <summary>
	/// Supported by adapter message types.
	/// </summary>
	IEnumerable<MessageTypes> SupportedInMessages { get; set; }

	/// <summary>
	/// Not supported by adapter result message types.
	/// </summary>
	IEnumerable<MessageTypes> NotSupportedResultMessages { get; }

	/// <summary>
	/// Get supported by adapter message types.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <param name="from">Start date for request. If <see langword="null"/>, then all available messages will be returned.</param>
	/// <param name="to">End date for request. If <see langword="null"/>, then all available messages will be returned.</param>
	/// <returns>Supported by adapter market data types.</returns>
	IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId = default, DateTimeOffset? from = default, DateTimeOffset? to = default);

	/// <summary>
	/// Possible options for candles building.
	/// </summary>
	IEnumerable<Level1Fields> CandlesBuildFrom { get; }

	/// <summary>
	/// Check possible time-frame by request.
	/// </summary>
	bool CheckTimeFrameByRequest { get; }

	/// <summary>
	/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
	/// </summary>
	ReConnectionSettings ReConnectionSettings { get; }

	/// <summary>
	///  Server check interval for track the connection alive. The value is <see cref="TimeSpan.Zero"/> turned off tracking.
	/// </summary>
	TimeSpan HeartbeatInterval { get; set; }

	/// <summary>
	/// The storage name, associated with the adapter.
	/// </summary>
	string StorageName { get; }

	/// <summary>
	/// Native identifier can be stored.
	/// </summary>
	bool IsNativeIdentifiersPersistable { get; }

	/// <summary>
	/// Identify security in messages by native identifier <see cref="SecurityId.Native"/>.
	/// </summary>
	bool IsNativeIdentifiers { get; }

	/// <summary>
	/// Translates <see cref="CandleMessage"/> as only fully filled.
	/// </summary>
	bool IsFullCandlesOnly { get; }

	/// <summary>
	/// Support any subscriptions (ticks, order books etc.).
	/// </summary>
	bool IsSupportSubscriptions { get; }

	/// <summary>
	/// Support candles subscription and live updates.
	/// </summary>
	/// <param name="subscription"><see cref="MarketDataMessage"/></param>
	/// <returns>Check result.</returns>
	bool IsSupportCandlesUpdates(MarketDataMessage subscription);

	/// <summary>
	/// Support candles <see cref="CandleMessage.PriceLevels"/>.
	/// </summary>
	/// <param name="subscription"><see cref="MarketDataMessage"/></param>
	/// <returns>Check result.</returns>
	bool IsSupportCandlesPriceLevels(MarketDataMessage subscription);

	/// <summary>
	/// Support partial downloading.
	/// </summary>
	bool IsSupportPartialDownloading { get; }

	/// <summary>
	/// Message adapter categories.
	/// </summary>
	MessageAdapterCategories Categories { get; }

	/// <summary>
	/// Names of extended security fields in <see cref="SecurityMessage"/>.
	/// </summary>
	IEnumerable<Tuple<string, Type>> SecurityExtendedFields { get; }

	/// <summary>
	/// Available options for <see cref="MarketDataMessage.MaxDepth"/>.
	/// </summary>
	IEnumerable<int> SupportedOrderBookDepths { get; }

	/// <summary>
	/// Adapter translates incremental order books.
	/// </summary>
	bool IsSupportOrderBookIncrements { get; }

	/// <summary>
	/// Adapter fills <see cref="ExecutionMessage.PnL"/>.
	/// </summary>
	bool IsSupportExecutionsPnL { get; }

	/// <summary>
	/// Adapter provides news related with specified security.
	/// </summary>
	bool IsSecurityNewsOnly { get; }

	/// <summary>
	/// Enqueue subscriptions.
	/// </summary>
	/// <remarks>
	/// Do not send new request before received confirmation for previous.
	/// </remarks>
	bool EnqueueSubscriptions { get; set; }

	/// <summary>
	/// Type of <see cref="OrderCondition"/>.
	/// </summary>
	/// <remarks>
	/// If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.
	/// </remarks>
	Type OrderConditionType { get; }

	/// <summary>
	/// Start sending <see cref="TimeMessage"/> before connection established.
	/// </summary>
	bool HeartbeatBeforConnect { get; }

	/// <summary>
	/// Icon.
	/// </summary>
	Uri Icon { get; }

	/// <summary>
	/// Send auto response for <see cref="OrderStatusMessage"/> and <see cref="PortfolioLookupMessage"/> unsubscribes.
	/// </summary>
	bool IsAutoReplyOnTransactonalUnsubscription { get; }

	/// <summary>
	/// Adapter translates orders changes on reply of <see cref="OrderStatusMessage"/>.
	/// </summary>
	bool IsSupportTransactionLog { get; }

	/// <summary>
	/// Adapter required emulation <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <remarks><see langword="null"/> means no emulatior, <see langword="true"/> by order balance, <see langword="false"/> by trades.</remarks>
	bool? IsPositionsEmulationRequired { get; }

	/// <summary>
	/// Is the <see cref="OrderReplaceMessage"/> command edit a current order.
	/// </summary>
	bool IsReplaceCommandEditCurrent { get; }

	/// <summary>
	/// The adapter can process subscription only with instruments associated with the specified board.
	/// </summary>
	string[] AssociatedBoards { get; }

	/// <summary>
	/// The adapter requires extra setup.
	/// </summary>
	bool ExtraSetup { get; }

	/// <summary>
	/// Create market depth builder.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Order log to market depth builder.</returns>
	IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId);

	/// <summary>
	/// Get maximum size step allowed for historical download.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="iterationInterval">Interval between iterations.</param>
	/// <returns>Step.</returns>
	TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval);

	/// <summary>
	/// Get maximum possible items count per single subscription request.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Max items count.</returns>
	int? GetMaxCount(DataType dataType);

	/// <summary>
	/// Is for the specified <paramref name="dataType"/> all securities downloading enabled.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Check result.</returns>
	bool IsAllDownloadingSupported(DataType dataType);

	/// <summary>
	/// Support filtering subscriptions (subscribe/unsubscribe for specified security).
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Check result.</returns>
	bool IsSecurityRequired(DataType dataType);

	/// <summary>
	/// Use channels for in and out messages.
	/// </summary>
	bool UseInChannel { get; }

	/// <summary>
	/// Use channels for in and out messages.
	/// </summary>
	bool UseOutChannel { get; }

	/// <summary>
	/// Feature name.
	/// </summary>
	string FeatureName { get; }

	/// <summary>
	/// Interval between iterations.
	/// </summary>
	TimeSpan IterationInterval { get; }

	/// <summary>
	/// Lookup timeout.
	/// </summary>
	TimeSpan? LookupTimeout { get; }
}

/// <summary>
/// Message adapter, provided <see cref="Key"/> and <see cref="Secret"/> properties.
/// </summary>
public interface IKeySecretAdapter
{
	/// <summary>
	/// Key.
	/// </summary>
	SecureString Key { get; set; }

	/// <summary>
	/// Secret.
	/// </summary>
	SecureString Secret { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="Login"/> and <see cref="Password"/> properties.
/// </summary>
public interface ILoginPasswordAdapter
{
	/// <summary>
	/// Login.
	/// </summary>
	string Login { get; set; }

	/// <summary>
	/// Password.
	/// </summary>
	SecureString Password { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="Token"/> property.
/// </summary>
public interface ITokenAdapter
{
	/// <summary>
	/// Token.
	/// </summary>
	SecureString Token { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="Passphrase"/> property.
/// </summary>
public interface IPassphraseAdapter
{
	/// <summary>
	/// Passphrase.
	/// </summary>
	SecureString Passphrase { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="IsDemo"/> property.
/// </summary>
public interface IDemoAdapter
{
	/// <summary>
	/// Connect to demo trading instead of real trading server.
	/// </summary>
	bool IsDemo { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="Address"/> property.
/// </summary>
/// <typeparam name="TAddress">Address type.</typeparam>
public interface IAddressAdapter<TAddress>
{
	/// <summary>
	/// Server address.
	/// </summary>
	TAddress Address { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="SenderCompId"/> and <see cref="TargetCompId"/> properties.
/// </summary>
public interface ISenderTargetAdapter
{
	/// <summary>
	/// Sender ID.
	/// </summary>
	string SenderCompId { get; set; }

	/// <summary>
	/// Target ID.
	/// </summary>
	string TargetCompId { get; set; }
}

/// <summary>
/// Message adapter, provided <see cref="IsBinaryEnabled"/> property.
/// </summary>
public interface IBinaryAdapter
{
	/// <summary>
	/// Enable binary mode.
	/// </summary>
	bool IsBinaryEnabled { get; set; }
}