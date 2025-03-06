namespace StockSharp.Algo.Testing;

/// <summary>
/// The base connection of emulation.
/// </summary>
public abstract class BaseEmulationConnector : Connector
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseEmulationConnector"/>.
	/// </summary>
	/// <param name="emulationAdapter">Emulation message adapter.</param>
	/// <param name="applyHeartbeat">Apply on/off heartbeat mode for the specified adapter.</param>
	/// <param name="initChannels">Initialize channels.</param>
	protected BaseEmulationConnector(EmulationMessageAdapter emulationAdapter, bool applyHeartbeat, bool initChannels)
		: base(
			new InMemorySecurityStorage(emulationAdapter.CheckOnNull(nameof(emulationAdapter)).Emulator.SecurityProvider),
			new InMemoryPositionStorage(emulationAdapter.Emulator.PortfolioProvider),
			emulationAdapter.Emulator.ExchangeInfoProvider, initChannels: initChannels)
	{
		Adapter.InnerAdapters.Add(emulationAdapter ?? throw new ArgumentNullException(nameof(emulationAdapter)));
		Adapter.ApplyHeartbeat(EmulationAdapter, applyHeartbeat);

		Adapter.IsSupportTransactionLog = emulationAdapter.IsSupportTransactionLog;

		TimeChange = false;

		// sync transaction ids with underlying adapter
		TransactionIdGenerator = emulationAdapter.TransactionIdGenerator;
	}

	/// <inheritdoc />
	public override IEnumerable<Portfolio> Portfolios => base.Portfolios.Concat(EmulationAdapter.Emulator.PortfolioProvider.Portfolios).Distinct();

	private DateTimeOffset _currentTime;

	/// <inheritdoc />
	public override DateTimeOffset CurrentTime => _currentTime;

	/// <summary>
	/// The adapter, executing messages in <see cref="IMarketEmulator"/>.
	/// </summary>
	public EmulationMessageAdapter EmulationAdapter => (EmulationMessageAdapter)Adapter.InnerAdapters.First();

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		if (EmulationAdapter.OwnInnerAdapter)
			EmulationAdapter.Load(storage, nameof(EmulationAdapter));

		base.Load(storage);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		if (EmulationAdapter.OwnInnerAdapter)
			storage.SetValue(nameof(EmulationAdapter), EmulationAdapter.Save());

		base.Save(storage);
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		// output messages from adapters goes non ordered
		if (_currentTime < message.LocalTime)
			_currentTime = message.LocalTime;

		base.OnProcessMessage(message);
	}

	/// <inheritdoc />
	public override void ClearCache()
	{
		base.ClearCache();

		_currentTime = default;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (EmulationAdapter.OwnInnerAdapter)
			EmulationAdapter.Dispose();

		base.DisposeManaged();
	}

	/// <summary>
	/// To register the trades generator.
	/// </summary>
	/// <param name="generator">The trades generator.</param>
	/// <returns>Subscription.</returns>
	//[Obsolete("Uses custom adapter implementation.")]
	public Subscription RegisterTrades(TradeGenerator generator)
		=> SubscribeGenerator(generator);

	/// <summary>
	/// To delete the trades generator, registered earlier through <see cref="RegisterTrades"/>.
	/// </summary>
	/// <param name="generator">The trades generator.</param>
	[Obsolete("Uses UnSubscribe method.")]
	public void UnRegisterTrades(TradeGenerator generator)
		=> UnSubscribeGenerator(generator);

	/// <summary>
	/// To register the order books generator.
	/// </summary>
	/// <param name="generator">The order books generator.</param>
	/// <returns>Subscription.</returns>
	//[Obsolete("Uses custom adapter implementation.")]
	public Subscription RegisterMarketDepth(MarketDepthGenerator generator)
		=> SubscribeGenerator(generator);

	/// <summary>
	/// To delete the order books generator, earlier registered through <see cref="RegisterMarketDepth"/>.
	/// </summary>
	/// <param name="generator">The order books generator.</param>
	[Obsolete("Uses UnSubscribe method.")]
	public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		=> UnSubscribeGenerator(generator);

	/// <summary>
	/// To register the orders log generator.
	/// </summary>
	/// <param name="generator">The orders log generator.</param>
	/// <returns>Subscription.</returns>
	//[Obsolete("Uses custom adapter implementation.")]
	public Subscription RegisterOrderLog(OrderLogGenerator generator)
		=> SubscribeGenerator(generator);

	/// <summary>
	/// To delete the orders log generator, earlier registered through <see cref="RegisterOrderLog"/>.
	/// </summary>
	/// <param name="generator">The orders log generator.</param>
	[Obsolete("Uses UnSubscribe method.")]
	public void UnRegisterOrderLog(OrderLogGenerator generator)
		=> UnSubscribeGenerator(generator);

	private Subscription SubscribeGenerator(MarketDataGenerator generator)
	{
		if (generator == null)
			throw new ArgumentNullException(nameof(generator));

		var subscription = new Subscription(new GeneratorMessage
		{
			TransactionId = TransactionIdGenerator.GetNextId(),
			IsSubscribe = true,
			SecurityId = generator.SecurityId,
			Generator = generator,
			DataType2 = generator.DataType,
		});

		Subscribe(subscription);

		return subscription;
	}

	private void UnSubscribeGenerator(MarketDataGenerator generator)
	{
		if (generator is null)
			throw new ArgumentNullException(nameof(generator));

		var subscription = Subscriptions.FirstOrDefault(s => s.SubscriptionMessage is GeneratorMessage genMsg && genMsg.Generator == generator);
		
		if (subscription != null)
			UnSubscribe(subscription);
		else
			LogWarning(LocalizedStrings.SubscriptionNonExist, generator);
	}
}