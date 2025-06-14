namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Risk;
using StockSharp.Algo.Commissions;

/// <summary>
/// The emulation connection. It uses historical data and/or occasionally generated.
/// </summary>
public class HistoryEmulationConnector : BaseEmulationConnector
{
	private bool _stopPending;

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	public HistoryEmulationConnector(ISecurityProvider securityProvider)
		: this(securityProvider, [Portfolio.CreateSimulator()])
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public HistoryEmulationConnector(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
		: this(securityProvider, portfolioProvider, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securities">Instruments, which will be sent through the <see cref="Connector.SecurityReceived"/> event.</param>
	/// <param name="portfolios">Portfolios, which will be sent through the <see cref="Connector.PortfolioReceived"/> event.</param>
	public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios)
		: this(securities, portfolios, new StorageRegistry())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securities">Instruments, the operation will be performed with.</param>
	/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public HistoryEmulationConnector(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
	public HistoryEmulationConnector(ISecurityProvider securityProvider, IEnumerable<Portfolio> portfolios)
		: this(securityProvider, new CollectionPortfolioProvider(portfolios), new InMemoryExchangeInfoProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public HistoryEmulationConnector(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider)
		: this(securityProvider, portfolioProvider, exchangeInfoProvider, new StorageRegistry(exchangeInfoProvider))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public HistoryEmulationConnector(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry)
		: this(new HistoryMessageAdapter(new IncrementalIdGenerator(), securityProvider) { StorageRegistry = storageRegistry }, true, new InMemoryMessageChannel(new MessageByLocalTimeQueue(), "Emulator in", err => err.LogError()), securityProvider, portfolioProvider, exchangeInfoProvider)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="ownInnerAdapter">Control <paramref name="innerAdapter"/> lifetime.</param>
	/// <param name="inChannel">Incoming messages channel.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public HistoryEmulationConnector(IMessageAdapter innerAdapter, bool ownInnerAdapter, IMessageChannel inChannel, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider)
		: base(new EmulationMessageAdapter(innerAdapter, inChannel, true, securityProvider, portfolioProvider, exchangeInfoProvider) { OwnInnerAdapter = ownInnerAdapter }, false, false)
	{
		MarketTimeChangedInterval = HistoryMessageAdapter.MarketTimeChangedInterval;

		Adapter.LatencyManager = null;
		Adapter.CommissionManager = null;
		Adapter.PnLManager = null;
		Adapter.SlippageManager = null;
		Adapter.SupportSecurityAll = false;
		Adapter.SendFinishedCandlesImmediatelly = true;
		//Adapter.SupportCandlesCompression = false;
		Adapter.SupportBuildingFromOrderLog = false;
		Adapter.SupportPartialDownload = false;
		Adapter.SupportLookupTracking = false;
		Adapter.SupportOrderBookTruncate = false;
		Adapter.ConnectDisconnectEventOnFirstAdapter = false;
		Adapter.GenerateOrderBookFromLevel1 = false;

		InMessageChannel = new PassThroughMessageChannel();
		OutMessageChannel = new PassThroughMessageChannel();

		// при тестировании по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
		//ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

		//MaxMessageCount = 1000;

		CurrentTimeChanged += OnCurrentTimeChanged;
		Disconnected += OnDisconnected;

		SupportFilteredMarketDepth = false;
		UpdateSecurityLastQuotes = false;
		UpdateSecurityByLevel1 = false;
		UpdatePortfolioByChange = false;
		SupportBasketSecurities = true;

		innerAdapter.Parent ??= this;

		SubscriptionsOnConnect.Remove(SecurityLookup);
	}

	/// <inheritdoc />
	public override IRiskManager RiskManager => null;

	/// <inheritdoc />
	public override bool SupportSnapshots => false;

	/// <summary>
	/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public HistoryMessageAdapter HistoryMessageAdapter => EmulationAdapter.FindAdapter<HistoryMessageAdapter>();

	private ChannelStates _state = ChannelStates.Stopped;

	/// <summary>
	/// The emulator state.
	/// </summary>
	public ChannelStates State
	{
		get => _state;
		private set
		{
			if (_state == value)
				return;

			if (!EmulationAdapter.OwnInnerAdapter && _state == ChannelStates.Stopped && value == ChannelStates.Stopping)
				return;

			bool throwError;

			var channel = EmulationAdapter.InChannel;

			switch (value)
			{
				case ChannelStates.Stopped:
					throwError = _state != ChannelStates.Stopping;

					//if (EmulationAdapter.OwnInnerAdapter)
						if (!EmulationAdapter.OwnInnerAdapter && channel is InMemoryMessageChannel inMem)
							inMem.Disabled = true;

						channel.Close();

					break;
				case ChannelStates.Stopping:
					throwError = _state is not ChannelStates.Started and not ChannelStates.Suspended
						and not ChannelStates.Starting;  // при ошибках при запуске эмуляции состояние может быть Starting

					//if (EmulationAdapter.OwnInnerAdapter)
					{
						channel.Clear();

						if (_state == ChannelStates.Suspended)
							channel.Resume();
					}

					break;
				case ChannelStates.Starting:
					throwError = _state is not ChannelStates.Stopped and not ChannelStates.Suspended;
					break;
				case ChannelStates.Started:
					throwError = _state != ChannelStates.Starting;
					break;
				case ChannelStates.Suspending:
					throwError = _state != ChannelStates.Started;
					break;
				case ChannelStates.Suspended:
					throwError = _state != ChannelStates.Suspending;

					//if (EmulationAdapter.OwnInnerAdapter)
						channel.Suspend();

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);
			}

			if (throwError)
				throw new InvalidOperationException(LocalizedStrings.TaskCannotChangeState.Put(_state, value));

			LogInfo("State: {0}->{1}", _state, value);
			_state = value;

			try
			{
				StateChanged?.Invoke();
				StateChanged2?.Invoke(value);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}
	}

	/// <summary>
	/// Call <see cref="Connector.Disconnect"/> when any <see cref="Subscription"/> failed.
	/// </summary>
	public bool StopOnSubscriptionError { get; set; }

	private InMemoryMessageChannel InMemChannel
		=> (Adapter.InnerAdapters.FirstOrDefault() as EmulationMessageAdapter)?.InChannel as InMemoryMessageChannel;

	/// <summary>
	/// <see cref="InMemoryMessageChannel.MaxMessageCount"/>
	/// </summary>
	public int MaxMessageCount
	{
		get => InMemChannel?.MaxMessageCount ?? -1;
		set
		{
			var channel = InMemChannel;

			if (channel is not null)
				channel.MaxMessageCount = value;
		}
	}

	/// <inheritdoc/>
	protected override void RaiseSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
	{
		base.RaiseSubscriptionFailed(subscription, error, isSubscribe);

		if (StopOnSubscriptionError && subscription.SubscriptionMessage is MarketDataMessage)
			Disconnect();
	}

	/// <summary>
	/// The event on the emulator state change <see cref="State"/>.
	/// </summary>
	[Obsolete("Use StateChanged2 event.")]
	public event Action StateChanged;

	/// <summary>
	/// The event on the emulator state change <see cref="State"/>.
	/// </summary>
	public event Action<ChannelStates> StateChanged2;

	/// <summary>
	/// Progress changed event.
	/// </summary>
	public event Action<int> ProgressChanged;

	private DateTimeOffset _startTime;
	private DateTimeOffset _stopTime;
	private DateTimeOffset _nextTime;
	private long _stepTicks;
	private int _progress;

	private void OnCurrentTimeChanged(TimeSpan diff)
	{
		if (_stepTicks == default)
			return;

		if (CurrentTime < _nextTime && CurrentTime < _stopTime)
			return;

		_nextTime = _nextTime.AddTicks(_stepTicks);
		ProgressChanged?.Invoke(++_progress);
	}

	/// <summary>
	/// Has the emulator ended its operation due to end of data, or it was interrupted through the <see cref="IConnector.Disconnect"/>method.
	/// </summary>
	public bool IsFinished { get; private set; }
	
	/// <inheritdoc />
	public override TimeSpan MarketTimeChangedInterval
	{
		set
		{
			base.MarketTimeChangedInterval = value;
			HistoryMessageAdapter.MarketTimeChangedInterval = value;
		}
	}

	/// <summary>
	/// Commission rules.
	/// </summary>
	[Obsolete("Use EmulationSettings.CommissionRules property.")]
	public IEnumerable<ICommissionRule> CommissionRules { get; set; }

	/// <summary>
	/// <see cref="EmulationMessageAdapter.Settings"/>
	/// </summary>
	public MarketEmulatorSettings EmulationSettings => EmulationAdapter.Settings;

	/// <inheritdoc />
	public override void ClearCache()
	{
		base.ClearCache();

		IsFinished = false;
	}

	/// <inheritdoc />
	protected override void OnConnect()
	{
		_startTime = HistoryMessageAdapter.StartDate;
		_stopTime = HistoryMessageAdapter.StopDate;

		_stepTicks = (_stopTime - _startTime).Ticks / 100;

		_nextTime = _startTime.AddTicks(_stepTicks);
		_progress = default;

		_stopPending = false;

		base.OnConnect();
	}

	/// <inheritdoc />
	protected override void OnDisconnect()
	{
		if (/*EmulationAdapter.OwnInnerAdapter && */State == ChannelStates.Suspended)
			EmulationAdapter.InChannel.Resume();

		if (State is not ChannelStates.Stopped and not ChannelStates.Stopping)
			SendEmulationState(ChannelStates.Stopping);
		else
			_stopPending = true;
	}

	private void OnDisconnected()
	{
		State = ChannelStates.Stopped;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		CurrentTimeChanged -= OnCurrentTimeChanged;
		Disconnected -= OnDisconnected;

		base.DisposeManaged();

		MarketDataAdapter.DoDispose();
	}

	/// <summary>
	/// To start the emulation.
	/// </summary>
	public void Start()
	{
		var isResuming = /*EmulationAdapter.OwnInnerAdapter && */State == ChannelStates.Suspended;

		if (isResuming)
			EmulationAdapter.InChannel.Resume();

		SendEmulationState(ChannelStates.Starting);

		if (!isResuming)
		{
			foreach (var rule in EmulationSettings.CommissionRules)
				SendInMessage(new CommissionRuleMessage { Rule = rule });
		}
	}

	/// <summary>
	/// To suspend the emulation.
	/// </summary>
	public void Suspend()
	{
		SendEmulationState(ChannelStates.Suspending);
	}

	private void SendEmulationState(ChannelStates state)
	{
		var message = new EmulationStateMessage { State = state };

		if (EmulationAdapter.OwnInnerAdapter)
			SendInMessage(message);
		else
			ProcessEmulationStateMessage(message);
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		try
		{
			switch (message.Type)
			{
				case MessageTypes.EmulationState:
					ProcessEmulationStateMessage((EmulationStateMessage)message);
					break;

				default:
				{
					base.OnProcessMessage(message);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			SendOutError(ex);
			Disconnect();
		}
	}

	private void ProcessEmulationStateMessage(EmulationStateMessage message)
	{
		State = message.State;

		switch (State)
		{
			case ChannelStates.Stopping:
			{
				IsFinished = message.IsOk();
				
				// change ConnectionState to Disconnecting
				if (ConnectionState != ConnectionStates.Disconnecting)
					Disconnect();

				// base method cannot be invoked from OnDisconnect HistConnector.OnDisconnect
				base.OnDisconnect();

				break;
			}

			case ChannelStates.Starting:
			{
				State = ChannelStates.Started;
				break;
			}

			case ChannelStates.Suspending:
			{
				State = ChannelStates.Suspended;
				break;
			}
		}

		if (_stopPending && (State is ChannelStates.Started or ChannelStates.Suspended))
			ProcessEmulationStateMessage(new() { State = ChannelStates.Stopping });
	}

	/// <summary>
	/// Register historical data source.
	/// </summary>
	/// <param name="security">Instrument. If passed <see langword="null"/> the source will be applied for all subscriptions.</param>
	/// <param name="dataType">Data type.</param>
	/// <param name="getMessages">Historical data source.</param>
	/// <returns>Subscription.</returns>
	[Obsolete("Uses custom adapter implementation.")]
	public Subscription RegisterHistorySource(Security security, DataType dataType, Func<DateTimeOffset, IEnumerable<Message>> getMessages)
	{
		var subscription = new Subscription(new HistorySourceMessage
		{
			IsSubscribe = true,
			SecurityId = security?.ToSecurityId(copyExtended: true) ?? default,
			DataType2 = dataType,
			GetMessages = getMessages
		}, security);

		Subscribe(subscription);

		return subscription;
	}

	/// <summary>
	/// Unregister historical data source, previously registered by <see cref="RegisterHistorySource"/>.
	/// </summary>
	/// <param name="security">Instrument. If passed <see langword="null"/> the source will be removed for all subscriptions.</param>
	/// <param name="dataType">Data type.</param>
	[Obsolete("Uses UnSubscribe method.")]
	public void UnRegisterHistorySource(Security security, DataType dataType)
	{
		var secId = security?.ToSecurityId();
		
		var subscription = Subscriptions.FirstOrDefault(s => s.SubscriptionMessage is HistorySourceMessage sourceMsg && sourceMsg.SecurityId == secId && sourceMsg.DataType2 == dataType);
		
		if (subscription != null)
			UnSubscribe(subscription);
		else
			LogWarning(LocalizedStrings.SubscriptionNonExist, dataType);
	}
}