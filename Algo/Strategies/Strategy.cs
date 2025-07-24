namespace StockSharp.Algo.Strategies;

using System.Runtime.CompilerServices;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Strategies.Protective;

/// <summary>
/// <see cref="Order.Comment"/> auto-fill modes.
/// </summary>
[DataContract]
[Serializable]
public enum StrategyCommentModes
{
	/// <summary>
	/// Disabled.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisabledKey)]
	Disabled,

	/// <summary>
	/// By <see cref="Strategy.Id"/>.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IdKey)]
	Id,

	/// <summary>
	/// By <see cref="Strategy.Name"/>.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NameKey)]
	Name,
}

/// <summary>
/// Strategy trading modes.
/// </summary>
[DataContract]
[Serializable]
public enum StrategyTradingModes
{
	/// <summary>
	/// Allow trading.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradingKey,
		Description = LocalizedStrings.AllowTradingKey)]
	Full,

	/// <summary>
	/// Disabled trading.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DisabledKey,
		Description = LocalizedStrings.TradingDisabledKey)]
	Disabled,

	/// <summary>
	/// Cancel orders only.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CancelOrdersKey,
		Description = LocalizedStrings.CancelOrdersKey)]
	CancelOrdersOnly,

	/// <summary>
	/// Accept orders for reduce position only.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionReduceOnlyKey,
		Description = LocalizedStrings.PosConditionReduceOnlyDetailsKey)]
	ReducePositionOnly,
}

/// <summary>
/// The base class for all trade strategies.
/// </summary>
public partial class Strategy : BaseLogReceiver, INotifyPropertyChangedEx, IMarketRuleContainer,
    ICloneable<Strategy>, IMarketDataProvider, ISubscriptionProvider, ISecurityProvider,
    ITransactionProvider, IScheduledTask, ICustomTypeDescriptor, ITimeProvider,
	IPortfolioProvider, IPositionProvider
{
	private class StrategyChangeStateMessage(Strategy strategy, ProcessStates state)
		: Message(ExtendedMessageTypes.StrategyChangeState)
	{
		public Strategy Strategy { get; } = strategy ?? throw new ArgumentNullException(nameof(strategy));
		public ProcessStates State { get; } = state;

		public override Message Clone()
		{
			return new StrategyChangeStateMessage(Strategy, State);
		}
	}

	private class StrategyRuleList(Strategy strategy)
		: MarketRuleList(strategy)
	{
		private readonly Strategy _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

		protected override bool OnAdding(IMarketRule item)
		{
			return _strategy.ProcessState != ProcessStates.Stopping && base.OnAdding(item);
		}
	}

	private class OrderInfo
	{
		public bool IsCanceled { get; set; }
		public decimal ReceivedVolume { get; set; }
		public OrderStates PrevState { get; set; } = OrderStates.None;
	}

	private class IndicatorList(Strategy strategy)
		: SynchronizedSet<IIndicator>
	{
		private readonly Strategy _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
		private readonly CachedSynchronizedSet<IIndicator> _nonFormedIndicators = [];
		private bool _allFormed = true;

		public bool AllFormed
		{
			get => _allFormed;
			private set
			{
				if (_allFormed == value)
					return;

				_allFormed = value;
				_strategy.Notify(nameof(IsFormed));
			}
		}

		private void RefreshAllFormed()
		{
			AllFormed = _nonFormedIndicators.Cache.All(i => i.IsFormed);
		}

		private void OnChanged(IIndicatorValue input, IIndicatorValue result)
		{
			var indicator = result.Indicator ?? throw new InvalidOperationException("Indicator is not present.");

			if (!indicator.IsFormed)
				return;

			UnTrackIndicator(indicator);
		}

		private void UnTrackIndicator(IIndicator indicator)
		{
			if (indicator is null)
				throw new ArgumentNullException(nameof(indicator));

			indicator.Changed -= OnChanged;
			_nonFormedIndicators.Remove(indicator);
			RefreshAllFormed();
		}

		protected override void OnAdded(IIndicator item)
		{
			base.OnAdded(item);

			if (AllFormed)
				AllFormed = item.IsFormed;

			if (!item.IsFormed)
			{
				item.Changed += OnChanged;
				_nonFormedIndicators.Add(item);
			}
		}

		protected override void OnRemoved(IIndicator item)
		{
			base.OnRemoved(item);

			UnTrackIndicator(item);
		}

		protected override bool OnClearing()
		{
			foreach (var item in this)
				item.Changed -= OnChanged;

			_nonFormedIndicators.Clear();
			RefreshAllFormed();

			return base.OnClearing();
		}
	}

	private readonly CachedSynchronizedDictionary<Order, OrderInfo> _ordersInfo = [];

	private readonly CachedSynchronizedDictionary<Subscription, bool> _subscriptions = [];
	private readonly SynchronizedDictionary<long, Subscription> _subscriptionsById = [];
	private readonly CachedSynchronizedSet<Subscription> _suspendSubscriptions = [];

	private DateTimeOffset _firstOrderTime;
	private DateTimeOffset _lastOrderTime;
	private TimeSpan _maxOrdersKeepTime;
	private DateTimeOffset _lastPnlRefreshTime;
	private DateTimeOffset _prevTradeDate;
	private bool _isPrevDateTradable;
	private bool _stopping;
	private BoardMessage _boardMsg;

	private readonly IStrategyParam[] _systemParams;

	/// <summary>
	/// Initializes a new instance of the <see cref="Strategy"/>.
	/// </summary>
	public Strategy()
	{
		Rules = new StrategyRuleList(this);

		Parameters = new(this);

		_id = Param(nameof(Id), base.Id).SetHidden().SetReadOnly();
		_volume = Param(nameof(Volume), 1m).SetGreaterThanZero().SetDisplay(LocalizedStrings.Volume, LocalizedStrings.StrategyVolume, LocalizedStrings.General);
		_name = Param(nameof(Name), new string([.. GetType().Name.Where(char.IsUpper)])).SetDisplay(LocalizedStrings.Name, LocalizedStrings.StrategyName, LocalizedStrings.General).SetBasic(false);
		_disposeOnStop = Param(nameof(DisposeOnStop), false).SetCanOptimize(false).SetHidden();
		_waitRulesOnStop = Param(nameof(WaitRulesOnStop), false).SetCanOptimize(false).SetHidden();
		_cancelOrdersWhenStopping = Param(nameof(CancelOrdersWhenStopping), true).SetCanOptimize(false).SetHidden();
		_waitAllTrades = Param(nameof(WaitAllTrades), false).SetCanOptimize(false).SetHidden();
		_commentMode = Param(nameof(CommentMode), StrategyCommentModes.Disabled).SetDisplay(LocalizedStrings.Comment, LocalizedStrings.OrderComment, LocalizedStrings.General).SetBasic(false);
		_ordersKeepTime = Param(nameof(OrdersKeepTime), TimeSpan.FromDays(1)).SetNotNegative().SetDisplay(LocalizedStrings.Orders, LocalizedStrings.OrdersKeepTime, LocalizedStrings.General).SetBasic(false).SetCanOptimize(false);
		_logLevel = Param(nameof(LogLevel), LogLevels.Inherit).SetDisplay(LocalizedStrings.LogLevel, LocalizedStrings.LogLevelKey, LocalizedStrings.Logging).SetBasic(false);
		_tradingMode = Param(nameof(TradingMode), StrategyTradingModes.Full).SetDisplay(LocalizedStrings.Trading, LocalizedStrings.AllowTrading, LocalizedStrings.General).SetBasic(false);
		_unsubscribeOnStop = Param(nameof(UnsubscribeOnStop), true).SetCanOptimize(false).SetHidden();
		_workingTime = Param(nameof(WorkingTime), new WorkingTime()).SetRequired().SetDisplay(LocalizedStrings.WorkingTime, LocalizedStrings.WorkingHours, LocalizedStrings.General).SetBasic(false);
		_historySize = Param<TimeSpan?>(nameof(HistorySize)).SetNullOrNotNegative().SetDisplay(LocalizedStrings.DaysHistory, LocalizedStrings.DaysHistoryDesc, LocalizedStrings.General).SetBasic(false).SetCanOptimize(false);
		_security = Param<Security>(nameof(Security)).SetDisplay(LocalizedStrings.Security, LocalizedStrings.StrategySecurity, LocalizedStrings.General).SetNonBrowsable(HideSecurityAndPortfolioParameters);
		_portfolio = Param<Portfolio>(nameof(Portfolio)).SetDisplay(LocalizedStrings.Portfolio, LocalizedStrings.StrategyPortfolio, LocalizedStrings.General).SetNonBrowsable(HideSecurityAndPortfolioParameters).SetCanOptimize(false);
		_riskFreeRate = Param<decimal>(nameof(RiskFreeRate)).SetDisplay(LocalizedStrings.RiskFreeRate, LocalizedStrings.RiskFreeRateDesc, LocalizedStrings.General).SetCanOptimize(false);

		_systemParams =
		[
			_id,
			_disposeOnStop,
			_waitRulesOnStop,
			_cancelOrdersWhenStopping,
			_waitAllTrades,
			_ordersKeepTime,
			_unsubscribeOnStop,
			_workingTime,
			_historySize,
		];

		NameGenerator = new(this);
		NameGenerator.Changed += name => _name.Value = name;

		_riskManager = new RiskManager { Parent = this };
		_indicators = new(this);
		_posManager = new(this);
	}

	private readonly StrategyParam<Guid> _id;

	/// <summary>
	/// Strategy ID.
	/// </summary>
	public override Guid Id
	{
		get => _id.Value;
		set => _id.Value = value;
	}

	private readonly StrategyParam<LogLevels> _logLevel;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LogLevelKey,
		Description = LocalizedStrings.LogLevelKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.LoggingKey)]
	public override LogLevels LogLevel
	{
		get => _logLevel.Value;
		set => _logLevel.Value = value;
	}

	private readonly StrategyParam<string> _name;

	/// <summary>
	/// Strategy name.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.StrategyNameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public override string Name
	{
		get => _name.Value;
		set
		{
			if (value == Name)
				return;

			NameGenerator.Value = value;
			_name.Value = value;
		}
	}

	/// <summary>
	/// The generator of strategy name.
	/// </summary>
	[Browsable(false)]
	public StrategyNameGenerator NameGenerator { get; }

	private Connector _connector;

	/// <summary>
	/// Connection to the trading system.
	/// </summary>
	[Browsable(false)]
	public virtual Connector Connector
	{
		get => _connector;
		set
		{
			if (Connector == value)
				return;

			if (_connector != null)
			{
				ISubscriptionProvider isp = _connector;
				IConnector con = _connector;

				isp.OrderReceived             -= OnConnectorOrderReceived;
				isp.OwnTradeReceived          -= OnConnectorOwnTradeReceived;
#pragma warning disable CS0618 // Type or member is obsolete
				con.OrderRegisterFailed       -= OnConnectorOrderRegisterFailed;
				con.OrderCancelFailed         -= ProcessCancelOrderFail;
				con.OrderEdited               -= OnConnectorOrderEdited;
				con.OrderEditFailed           -= OnConnectorOrderEditFailed;
#pragma warning restore CS0618 // Type or member is obsolete
				con.NewMessage                -= OnConnectorNewMessage;
				con.CurrentTimeChanged        -= OnConnectorCurrentTimeChanged;
				isp.Level1Received            -= OnConnectorLevel1Received;
				isp.OrderBookReceived         -= OnConnectorOrderBookReceived;
				isp.TickTradeReceived         -= OnConnectorTickTradeReceived;
				isp.OrderLogReceived          -= OnConnectorOrderLogReceived;
				isp.SecurityReceived          -= OnConnectorSecurityReceived;
				isp.BoardReceived             -= OnConnectorBoardReceived;
				isp.NewsReceived              -= OnConnectorNewsReceived;
				isp.CandleReceived            -= OnConnectorCandleReceived;
				isp.OrderRegisterFailReceived -= OnConnectorOrderRegisterFailReceived;
				isp.OrderCancelFailReceived   -= OnConnectorOrderCancelFailReceived;
				isp.OrderEditFailReceived     -= OnConnectorOrderEditFailReceived;
				isp.PortfolioReceived         -= OnConnectorPortfolioReceived;
				isp.DataTypeReceived          -= OnConnectorDataTypeReceived;
				isp.SubscriptionReceived      -= OnConnectorSubscriptionReceived;
				isp.SubscriptionOnline        -= OnConnectorSubscriptionOnline;
				isp.SubscriptionStarted       -= OnConnectorSubscriptionStarted;
				isp.SubscriptionStopped       -= OnConnectorSubscriptionStopped;
				isp.SubscriptionFailed        -= OnConnectorSubscriptionFailed;

				// do not make any trading activity on disposing stage
				if (!IsDisposeStarted)
					UnSubscribe(true);
			}

			_connector = value;

			if (_connector != null)
			{
				ISubscriptionProvider isp = _connector;
				IConnector con = _connector;

				isp.OrderReceived             += OnConnectorOrderReceived;
				isp.OwnTradeReceived          += OnConnectorOwnTradeReceived;
#pragma warning disable CS0618 // Type or member is obsolete
				con.OrderRegisterFailed       += OnConnectorOrderRegisterFailed;
				con.OrderCancelFailed         += ProcessCancelOrderFail;
				con.OrderEdited               += OnConnectorOrderEdited;
				con.OrderEditFailed           += OnConnectorOrderEditFailed;
#pragma warning restore CS0618 // Type or member is obsolete
				con.NewMessage                += OnConnectorNewMessage;
				con.CurrentTimeChanged        += OnConnectorCurrentTimeChanged;
				isp.Level1Received            += OnConnectorLevel1Received;
				isp.OrderBookReceived         += OnConnectorOrderBookReceived;
				isp.TickTradeReceived         += OnConnectorTickTradeReceived;
				isp.OrderLogReceived          += OnConnectorOrderLogReceived;
				isp.SecurityReceived          += OnConnectorSecurityReceived;
				isp.BoardReceived             += OnConnectorBoardReceived;
				isp.NewsReceived              += OnConnectorNewsReceived;
				isp.CandleReceived            += OnConnectorCandleReceived;
				isp.OrderRegisterFailReceived += OnConnectorOrderRegisterFailReceived;
				isp.OrderCancelFailReceived   += OnConnectorOrderCancelFailReceived;
				isp.OrderEditFailReceived     += OnConnectorOrderEditFailReceived;
				isp.PortfolioReceived         += OnConnectorPortfolioReceived;
				isp.DataTypeReceived          += OnConnectorDataTypeReceived;
				isp.SubscriptionReceived      += OnConnectorSubscriptionReceived;
				isp.SubscriptionOnline        += OnConnectorSubscriptionOnline;
				isp.SubscriptionStarted       += OnConnectorSubscriptionStarted;
				isp.SubscriptionStopped       += OnConnectorSubscriptionStopped;
				isp.SubscriptionFailed        += OnConnectorSubscriptionFailed;
			}

			ConnectorChanged?.Invoke();
		}
	}

	private void OnConnectorPortfolioReceived(Subscription subscription, Portfolio portfolio)
	{
		if (CanProcess(subscription))
			PortfolioReceived?.Invoke(subscription, portfolio);
	}

	private void OnConnectorOrderEditFailReceived(Subscription subscription, OrderFail fail)
	{
		if (CanProcess(subscription))
			OrderEditFailReceived?.Invoke(subscription, fail);
	}

	private void OnConnectorOrderCancelFailReceived(Subscription subscription, OrderFail fail)
	{
		if (CanProcess(subscription))
			OrderCancelFailReceived?.Invoke(subscription, fail);
	}

	private void OnConnectorOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
	{
		if (CanProcess(subscription))
			OrderRegisterFailReceived?.Invoke(subscription, fail);
	}

	/// <summary>
	/// To get the strategy getting <see cref="Connector"/>. If it is not initialized, the exception will be discarded.
	/// </summary>
	/// <returns>Connector.</returns>
	public IConnector SafeGetConnector()
		=> Connector ?? throw new InvalidOperationException(LocalizedStrings.ConnectionNotInit);

	/// <summary>
	/// Make <see cref="Security"/> and <see cref="Portfolio"/> as non-browsable.
	/// </summary>
	public static bool HideSecurityAndPortfolioParameters { get; set; }

	private readonly StrategyParam<Portfolio> _portfolio;

	/// <summary>
	/// Portfolio.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfolioKey,
		Description = LocalizedStrings.StrategyPortfolioKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public Portfolio Portfolio
	{
		get => _portfolio.Value;
		set => _portfolio.Value = value;
	}

	private readonly StrategyParam<Security> _security;

	/// <summary>
	/// Security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.StrategySecurityKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public Security Security
	{
		get => _security.Value;
		set => _security.Value = value;
	}

	/// <summary>
	/// Total slippage.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.TotalSlippageKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 99)]
	[ReadOnly(true)]
	[Browsable(false)]
	public decimal? Slippage { get; private set; }

	/// <summary>
	/// <see cref="Slippage"/> change event.
	/// </summary>
	public event Action SlippageChanged;

	private IPnLManager _pnLManager = new PnLManager { UseOrderBook = true };

	/// <summary>
	/// The profit-loss manager. It accounts trades of this strategy, as well as of its subsidiary strategies <see cref="ChildStrategies"/>.
	/// </summary>
	[Browsable(false)]
	public IPnLManager PnLManager
	{
		get => _pnLManager;
		set => _pnLManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// The aggregate value of profit-loss without accounting commission <see cref="Commission"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PnLKey,
		Description = LocalizedStrings.TotalPnLKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 100)]
	[ReadOnly(true)]
	[Browsable(false)]
	public decimal PnL => PnLManager.GetPnL();

	/// <summary>
	/// <see cref="PnL"/> change event.
	/// </summary>
	public event Action PnLChanged;

	/// <summary>
	/// <see cref="PnL"/> change event.
	/// </summary>
	public event Action<Subscription, Portfolio, DateTimeOffset, decimal, decimal?, decimal?> PnLReceived2;

	/// <summary>
	/// Total commission.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.TotalCommissionDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 101)]
	[ReadOnly(true)]
	[Browsable(false)]
	public decimal? Commission { get; private set; }

	/// <summary>
	/// <see cref="Commission"/> change event.
	/// </summary>
	public event Action CommissionChanged;

	/// <summary>
	/// Total latency.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LatencyKey,
		Description = LocalizedStrings.TotalLatencyKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 102)]
	[ReadOnly(true)]
	[Browsable(false)]
	public TimeSpan? Latency { get; private set; }

	/// <summary>
	/// <see cref="Latency"/> change event.
	/// </summary>
	public event Action LatencyChanged;

	private IStatisticManager _statisticManager = new StatisticManager();

	/// <summary>
	/// The statistics manager.
	/// </summary>
	[Browsable(false)]
	public IStatisticManager StatisticManager
	{
		get => _statisticManager;
		protected set => _statisticManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	private IRiskManager _riskManager;

	/// <summary>
	/// The risks control manager.
	/// </summary>
	[Browsable(false)]
	public IRiskManager RiskManager
	{
		get => _riskManager;
		set => _riskManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// The risk rules.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.SettingsKey,
		Name = LocalizedStrings.RisksKey,
		Description = LocalizedStrings.RiskSettingsKey,
		Order = 300)]
	public IEnumerable<IRiskRule> RiskRules
	{
		get => _riskManager.Rules;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			_riskManager.Rules.Clear();
			_riskManager.Rules.AddRange(value);

			RaiseParametersChanged();
		}
	}

	/// <summary>
	/// Strategy parameters.
	/// </summary>
	[Browsable(false)]
	public StrategyParameterDictionary Parameters { get; }

	private StrategyParam<T> Param<T>(StrategyParam<T> p)
	{
		Parameters.Add(p ?? throw new ArgumentNullException(nameof(p)));
		return p;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	/// <param name="id">Parameter identifier.</param>
	/// <param name="initialValue">The initial value.</param>
	/// <returns>The strategy parameter.</returns>
	public StrategyParam<T> Param<T>(string id, T initialValue = default)
		=> Param(new StrategyParam<T>(id, initialValue)).SetBasic(true);

	/// <summary>
	/// <see cref="Parameters"/> change event.
	/// </summary>
	public event Action ParametersChanged;

	/// <summary>
	/// To call events <see cref="ParametersChanged"/> and <see cref="PropertyChanged"/>.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	public void RaiseParametersChanged([CallerMemberName]string name = default)
	{
		ParametersChanged?.Invoke();
		this.Notify(name);

		if (name == nameof(Security) || name == nameof(Portfolio))
		{
			this.Notify(nameof(Position));
		}
	}

	/// <summary>
	/// Strategy environment parameters.
	/// </summary>
	[Browsable(false)]
	public SettingsStorage Environment { get; } = [];

	bool IScheduledTask.CanStart => ProcessState == ProcessStates.Stopped;
	bool IScheduledTask.CanStop => ProcessState == ProcessStates.Started;

	private readonly StrategyParam<WorkingTime> _workingTime;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WorkingTimeKey,
		Description = LocalizedStrings.WorkingHoursKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 15)]
	public WorkingTime WorkingTime
	{
		get => _workingTime.Value;
		set => _workingTime.Value = value;
	}

	private ProcessStates _processState;

	/// <summary>
	/// The operation state.
	/// </summary>
	[Browsable(false)]
	public virtual ProcessStates ProcessState
	{
		get => _processState;
		private set
		{
			if (_processState == value)
				return;

			LogDebug("State: {0}->{1}", _processState, value);

			if (_processState == ProcessStates.Stopped && value == ProcessStates.Stopping)
				throw new InvalidOperationException(LocalizedStrings.StrategyAlreadyStopped.Put(Name, value));

			_processState = value;

			try
			{
				switch (value)
				{
					case ProcessStates.Started:
					{
						StartedTime = base.CurrentTime;
						TotalWorkingTime = default;
						LogProcessState(value);
						OnStarted(StartedTime);
						break;
					}
					case ProcessStates.Stopping:
					{
						LogProcessState(value);
						OnStopping();
						break;
					}
					case ProcessStates.Stopped:
					{
						if (StartedTime != default)
							TotalWorkingTime += base.CurrentTime - StartedTime;

						StartedTime = default;
						LogProcessState(value);
						OnStopped();
						break;
					}
				}
			}
			catch (Exception error)
			{
				OnError(this, error);

				if (value == ProcessStates.Started)
				{
					Stop(error);
					return;
				}
			}

			try
			{
				CheckRefreshOnlineState();
				RaiseProcessStateChanged(this);
				this.Notify();
			}
			catch (Exception error)
			{
				OnError(this, error);
			}

			if (ProcessState == ProcessStates.Stopping)
			{
				if (CancelOrdersWhenStopping)
				{
					LogInfo(LocalizedStrings.WaitingCancellingAllOrders);
					ProcessCancelActiveOrders(default, default, default, default, default, default, default);
				}

				foreach (var rule in GetRules())
				{
					if (this.TryRemoveWithExclusive(rule))
					{
					}
				}

				try
				{
					TryFinalStop();
				}
				catch (Exception error)
				{
					OnError(this, error);
				}
			}
		}
	}

	private void LogProcessState(ProcessStates state)
	{
		var stateStr = state switch
		{
			ProcessStates.Stopped => LocalizedStrings.Stopped,
			ProcessStates.Stopping => LocalizedStrings.Stopping,
			ProcessStates.Started => LocalizedStrings.Started,
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.InvalidValue),
		};

		LogInfo("Strategy {0}. Position {1}.", stateStr, Position);
	}

	/// <summary>
	/// <see cref="ProcessState"/> change event.
	/// </summary>
	public event Action<Strategy> ProcessStateChanged;

	/// <summary>
	/// To call the event <see cref="ProcessStateChanged"/>.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	protected void RaiseProcessStateChanged(Strategy strategy)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		ProcessStateChanged?.Invoke(strategy);
	}

	private readonly StrategyParam<bool> _cancelOrdersWhenStopping;

	/// <summary>
	/// To cancel active orders at stop. Is On by default.
	/// </summary>
	[Browsable(false)]
	public bool CancelOrdersWhenStopping
	{
		get => _cancelOrdersWhenStopping.Value;
		set => _cancelOrdersWhenStopping.Value = value;
	}

	/// <summary>
	/// Orders, registered within the strategy framework.
	/// </summary>
	[Browsable(false)]
	public IEnumerable<Order> Orders => _ordersInfo.CachedKeys;

	private readonly StrategyParam<TimeSpan> _ordersKeepTime;

	/// <summary>
	/// The time for storing <see cref="Orders"/> in memory. By default it equals to 2 days. If value is set in <see cref="TimeSpan.Zero"/>, orders will not be deleted.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrdersKey,
		Description = LocalizedStrings.OrdersKeepTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public TimeSpan OrdersKeepTime
	{
		get => _ordersKeepTime.Value;
		set => _ordersKeepTime.Value = value;
	}

	private readonly StrategyParam<StrategyTradingModes> _tradingMode;

	/// <summary>
	/// Allow trading.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradingKey,
		Description = LocalizedStrings.AllowTradingKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	public StrategyTradingModes TradingMode
	{
		get => _tradingMode.Value;
		set => _tradingMode.Value = value;
	}

	private bool _isOnline;

	/// <summary>
	/// True means that strategy is started and all of its subscriptions are in online state.
	/// </summary>
	[Browsable(false)]
	public bool IsOnline
	{
		get => _isOnline;
		private set
		{
			if (_isOnline == value)
				return;

			_isOnline = value;
			this.Notify();
		}
	}

	/// <summary>
	/// Strategy fully formed.
	/// </summary>
	/// <remarks>
	/// Default implementation used <see cref="Indicators"/> collection to check all <see cref="IIndicator.IsFormed"/> are <see langword="true"/>.
	/// </remarks>
	[Browsable(false)]
	public virtual bool IsFormed => _indicators.AllFormed;

	private readonly StrategyParam<bool> _unsubscribeOnStop;

	/// <summary>
	/// Unsubscribe all active subscription while strategy become stopping.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnsubscribeKey,
		Description = LocalizedStrings.UnsubscribeOnStopKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 11)]
	public bool UnsubscribeOnStop
	{
		get => _unsubscribeOnStop.Value;
		set => _unsubscribeOnStop.Value = value;
	}

	private readonly CachedSynchronizedSet<MyTrade> _myTrades = [];

	/// <summary>
	/// Trades, matched during the strategy operation.
	/// </summary>
	[Browsable(false)]
	public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

	private readonly StrategyParam<decimal> _volume;

	/// <summary>
	/// Operational volume.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.StrategyVolumeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public virtual decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	private LogLevels _errorState;

	/// <summary>
	/// The state of an error.
	/// </summary>
	[Browsable(false)]
	public LogLevels ErrorState
	{
		get => _errorState;
		private set
		{
			if (_errorState == value)
				return;

			_errorState = value;
			this.Notify();
		}
	}

	private DateTimeOffset _startedTime;

	/// <summary>
	/// Strategy start time.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StartTimeKey,
		Description = LocalizedStrings.StrategyStartTimeKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 105)]
	[ReadOnly(true)]
	[Browsable(false)]
	public DateTimeOffset StartedTime
	{
		get => _startedTime;
		private set
		{
			if (_startedTime == value)
				return;

			_startedTime = value;
			this.Notify();
		}
	}

	private TimeSpan _totalWorkingTime;

	/// <summary>
	/// The total time of strategy operation less time periods, when strategy was stopped.
	/// </summary>
	[Browsable(false)]
	public TimeSpan TotalWorkingTime
	{
		get => _totalWorkingTime;
		private set
		{
			if (_totalWorkingTime == value)
				return;

			_totalWorkingTime = value;
			this.Notify();
		}
	}

	private readonly StrategyParam<bool> _disposeOnStop;

	/// <summary>
	/// Automatically to clear resources, used by the strategy, when it stops (state <see cref="ProcessState"/> becomes equal to <see cref="ProcessStates.Stopped"/>).
	/// </summary>
	/// <remarks>
	/// The mode is used only for one-time strategies, i.e. for those strategies, which will not be started again (for example, quoting). It is disabled by default.
	/// </remarks>
	[Browsable(false)]
	public bool DisposeOnStop
	{
		get => _disposeOnStop.Value;
		set => _disposeOnStop.Value = value;
	}

	private readonly StrategyParam<bool> _waitRulesOnStop;

	/// <summary>
	/// Wait <see cref="Rules"/> to finish before strategy become into <see cref="ProcessStates.Stopped"/> state.
	/// </summary>
	[Browsable(false)]
	public bool WaitRulesOnStop
	{
		get => _waitRulesOnStop.Value;
		set => _waitRulesOnStop.Value = value;
	}

	private readonly StrategyParam<bool> _waitAllTrades;

	/// <summary>
	/// Stop strategy only after getting all trades by registered orders.
	/// </summary>
	/// <remarks>
	/// It is disabled by default.
	/// </remarks>
	[Browsable(false)]
	public bool WaitAllTrades
	{
		get => _waitAllTrades.Value;
		set => _waitAllTrades.Value = value;
	}

	private readonly StrategyParam<StrategyCommentModes> _commentMode;

	/// <summary>
	/// Set <see cref="Order.Comment"/> by <see cref="Name"/> or <see cref="Id"/>.
	/// </summary>
	/// <remarks>
	/// By default is <see cref="StrategyCommentModes.Disabled"/>.
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommentKey,
		Description = LocalizedStrings.OrderCommentKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	public StrategyCommentModes CommentMode
	{
		get => _commentMode.Value;
		set => _commentMode.Value = value;
	}

	private readonly StrategyParam<decimal> _riskFreeRate;

	/// <summary>
	/// Annual risk-free rate (e.g., 0.03 = 3%).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RiskFreeRateKey,
		Description = LocalizedStrings.RiskFreeRateDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 11)]
	public decimal RiskFreeRate
	{
		get => _riskFreeRate.Value;
		set => _riskFreeRate.Value = value;
	}

	/// <inheritdoc />
	[Browsable(false)]
	public IMarketRuleList Rules { get; }

	private IMarketRule[] GetRules()
	{
		lock (Rules.SyncRoot)
			return [.. Rules];
	}

	//private readonly object _rulesSuspendLock = new object();
	private int _rulesSuspendCount;

	/// <inheritdoc />
	[Browsable(false)]
	public bool IsRulesSuspended => _rulesSuspendCount > 0;

	/// <summary>
	/// The event of sending order for registration.
	/// </summary>
	public event Action<Order> OrderRegistering;

	/// <summary>
	/// The event of sending order for re-registration.
	/// </summary>
	public event Action<Order, Order> OrderReRegistering;

	/// <summary>
	/// The event of sending order for cancelling.
	/// </summary>
	public event Action<Order> OrderCanceling;

	/// <summary>
	/// The event of strategy connection change.
	/// </summary>
	public event Action ConnectorChanged;

	/// <summary>
	/// The event of strategy online state change.
	/// </summary>
	public event Action<Strategy> IsOnlineChanged;

	/// <summary>
	/// The event of error occurrence in the strategy.
	/// </summary>
	public event Action<Strategy, Exception> Error;

	/// <summary>
	/// The last error that caused the strategy to stop.
	/// </summary>
	[Browsable(false)]
	public Exception LastError { get; private set; }

	/// <summary>
	/// The method is called when the <see cref="Start()"/> method has been called and the <see cref="ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
	/// </summary>
	protected virtual void OnStarted(DateTimeOffset time)
	{
		InitStartValues();

		Subscribe(PortfolioLookup, true);
		Subscribe(OrderLookup, true);
	}

	/// <summary>
	/// Init.
	/// </summary>
	protected void InitStartValues()
	{
		ErrorState = LogLevels.Info;

		var manager = StatisticManager;

		if (Portfolio?.CurrentValue is decimal beginValue)
		{
			foreach (var p in manager.Parameters.OfType<IBeginValueStatisticParameter>())
				p.BeginValue = beginValue;
		}

		foreach (var p in manager.Parameters.OfType<IRiskFreeRateStatisticParameter>())
			p.RiskFreeRate = RiskFreeRate;

		_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
	}

	/// <summary>
	/// The method is called when the <see cref="ProcessState"/> process state has been taken the <see cref="ProcessStates.Stopping"/> value.
	/// </summary>
	protected virtual void OnStopping()
	{
		if (UnsubscribeOnStop)
			UnSubscribe(false);

		UnSubscribe(PortfolioLookup);
		UnSubscribe(OrderLookup);
	}

	/// <summary>
	/// The method is called when the <see cref="ProcessState"/> process state has been taken the <see cref="ProcessStates.Stopped"/> value.
	/// </summary>
	protected virtual void OnStopped()
	{
	}

	private readonly IndicatorList _indicators;

	/// <summary>
	/// All indicators used in strategy. Uses in default implementation of <see cref="IsFormed"/>.
	/// </summary>
	[Browsable(false)]
	public INotifyList<IIndicator> Indicators => _indicators;

	private string _lastCantTradeReason;

	private bool CanTrade(bool reducePosition, out string reason)
	{
		if(CanTrade(CurrentTime, reducePosition, out reason))
		{
			_lastCantTradeReason = null;
			return true;
		}

		var logLevel = reason == _lastCantTradeReason ? LogLevels.Verbose : LogLevels.Warning;

		_lastCantTradeReason = reason;
		this.AddLog(logLevel, () => $"can't send orders: {_lastCantTradeReason}");
		return false;
	}

	/// <summary>
	/// Check if can trade.
	/// </summary>
	protected virtual bool CanTrade(DateTimeOffset time, bool reducePosition, out string noTradeReason)
	{
		if (ProcessState != ProcessStates.Started)
		{
			noTradeReason = LocalizedStrings.StrategyInStateCannotRegisterOrder.Put(ProcessState);
			return false;
		}

		if (!IsFormed)
		{
			noTradeReason = LocalizedStrings.NonFormed;
			return false;
		}

		if (TradingMode == StrategyTradingModes.Disabled)
		{
			noTradeReason = LocalizedStrings.TradingDisabled;
			return false;
		}
		else if (TradingMode == StrategyTradingModes.ReducePositionOnly && !reducePosition)
		{
			noTradeReason = LocalizedStrings.PosConditionReduceOnlyKey;
			return false;
		}

		if (_stopping)
		{
			noTradeReason = "Strategy is stopping.";
			return false;
		}

		noTradeReason = null;
		return true;
	}

	/// <inheritdoc />
	public void RegisterOrder(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		var pos = GetPositionValue(order.Security, order.Portfolio);

		if (!CanTrade(pos > 0 && pos.Value.GetDirection() == order.Side.Invert() && pos.Value.Abs() >= order.Volume, out var reason))
		{
			ProcessOrderFail(order, new InvalidOperationException(reason));
			return;
		}

		LogInfo("Registration {0} (0x{5:X}) order for {1} with price {2} and volume {3}. {4}",
			order.Type, order.Side, order.Price, order.Volume, order.Comment, order.GetHashCode());

		order.Security ??= Security;
		order.Portfolio ??= Portfolio;

		if (order.Comment.IsEmpty())
		{
			switch (CommentMode)
			{
				case StrategyCommentModes.Disabled:
					break;
				case StrategyCommentModes.Id:
					order.Comment = EnsureGetId();
					break;
				case StrategyCommentModes.Name:
					order.Comment = Name;
					break;
				default:
					throw new ArgumentOutOfRangeException(CommentMode.To<string>());
			}
		}

		var action = AddOrder(order, false);

		if (action is not null)
		{
			ProcessOrderFail(order, new InvalidOperationException(action.Value.GetFieldDisplayName()));
			return;
		}

		ProcessRegisterOrderAction(null, order, (oOrder, nOrder) =>
		{
			OnOrderRegistering(nOrder);

			SafeGetConnector().RegisterOrder(nOrder);
		});
	}

	/// <inheritdoc />
	public bool? IsOrderEditable(Order order) => SafeGetConnector().IsOrderEditable(order);

	/// <inheritdoc />
	public bool? IsOrderReplaceable(Order order) => SafeGetConnector().IsOrderReplaceable(order);

	/// <inheritdoc />
	public void EditOrder(Order order, Order changes)
	{
		LogInfo("EditOrder: {0}", order);

		if (!CanTrade(changes.Volume > 0 && order.Balance > changes.Volume, out var reason))
		{
			ProcessOrderFail(order, new InvalidOperationException(reason));
			return;
		}

		var action = ProcessRisk(() => order.CreateReplaceMessage(changes, order.Security.ToSecurityId()));

		if (action is null)
			SafeGetConnector().EditOrder(order, changes);
	}

	/// <inheritdoc />
	public void ReRegisterOrder(Order oldOrder, Order newOrder)
	{
		if (oldOrder == null)
			throw new ArgumentNullException(nameof(oldOrder));

		if (newOrder == null)
			throw new ArgumentNullException(nameof(newOrder));

		LogInfo("Reregistration {0} with price {1} to price {2}. {3}", oldOrder.TransactionId, oldOrder.Price, newOrder.Price, oldOrder.Comment);

		if (!CanTrade(newOrder.Volume > 0 && oldOrder.Balance > newOrder.Volume, out var reason))
		{
			ProcessOrderFail(newOrder, new InvalidOperationException(reason));
			return;
		}

		var action = AddOrder(newOrder, false);

		if (action is not null)
		{
			ProcessOrderFail(newOrder, new InvalidOperationException(action.Value.GetFieldDisplayName()));
			return;
		}

		ProcessRegisterOrderAction(oldOrder, newOrder, (oOrder, nOrder) =>
		{
			OnOrderReRegistering(oOrder, nOrder);

			SafeGetConnector().ReRegisterOrder(oOrder, nOrder);
		});
	}

	private RiskActions? ProcessRisk(Order order)
		=> ProcessRisk(() => order.CreateRegisterMessage());

	private RiskActions? AddOrder(Order order, bool restored)
	{
		var action = ProcessRisk(order);

		if (action is not null)
			return action;

		_ordersInfo.Add(order, new());

		if (!restored)
			order.UserOrderId = EnsureGetId();

		order.StrategyId = EnsureGetId();

		if (!order.State.IsFinal() && CancelOrdersWhenStopping)
		{
			IMarketRule matchedRule = order.WhenMatched(this);

			if (WaitAllTrades)
				matchedRule = matchedRule.And(order.WhenAllTrades(this));

			var successRule = order
				.WhenCanceled(this)
				.Or(matchedRule, order.WhenRegisterFailed(this))
				.Do(() => LogInfo(LocalizedStrings.OrderNoLongerActive.Put(order.TransactionId)))
				.Until(() =>
				{
					if (order.State == OrderStates.Failed)
						return true;

					if (order.State != OrderStates.Done)
					{
						LogWarning(LocalizedStrings.OrderHasState, order.TransactionId, order.State);
						return false;
					}

					if (!WaitAllTrades)
						return true;

					if (!_ordersInfo.TryGetValue(order, out var info))
					{
						LogWarning(LocalizedStrings.OrderNotFound, order.TransactionId);
						return false;
					}

					var leftVolume = order.GetMatchedVolume() - info.ReceivedVolume;

					if (leftVolume != 0)
					{
						LogDebug(LocalizedStrings.OrderHasBalance, order.TransactionId, leftVolume);
						return false;
					}

					return true;
				})
				.Apply(this);

			var canFinish = false;

			order
				.WhenCancelFailed(this)
				.Do(f =>
				{
					if (ProcessState != ProcessStates.Stopping)
						return;

					canFinish = true;
					LogInfo(LocalizedStrings.ErrorCancellingOrder.Put(order.TransactionId, f.Error.Message));
				})
				.Until(() => canFinish)
				.Apply(this)
				.Exclusive(successRule);
		}

		_newOrder?.Invoke(order);

		return null;
	}

	private void ProcessRegisterOrderAction(Order oOrder, Order nOrder, Action<Order, Order> action)
	{
		try
		{
			action(oOrder, nOrder);
		}
		catch (Exception excp)
		{
			LogError(LocalizedStrings.ErrorRegOrder, nOrder.TransactionId, excp.Message);

			ProcessOrderFail(nOrder, excp, true);
		}
	}

	private void ProcessOrderFail(Order order, Exception error, bool canRisk = false)
	{
		if (order is null)	throw new ArgumentNullException(nameof(order));
		if (error is null)	throw new ArgumentNullException(nameof(error));

		order.ApplyNewState(OrderStates.Failed, this);

		if (IsDisposeStarted)
			return;

		var fail = new OrderFail
		{
			Order = order,
			Error = error,
			ServerTime = CurrentTime,
			TransactionId = order.TransactionId,
		};

		OnOrderRegisterFailed(fail, canRisk && _ordersInfo.ContainsKey(order));

		foreach (var subscription in _subscriptions.CachedKeys.Where(s => s.SubscriptionMessage.Type == MessageTypes.OrderStatus))
			OrderRegisterFailReceived?.Invoke(subscription, fail);

		Rules.RemoveRulesByToken(order, null);
	}

	/// <inheritdoc />
	public void CancelOrder(Order order)
	{
		if (ProcessState != ProcessStates.Started)
		{
			LogWarning(LocalizedStrings.StrategyInStateCannotCancelOrder, ProcessState);
			return;
		}

		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (TradingMode == StrategyTradingModes.Disabled)
		{
			LogWarning(LocalizedStrings.TradingDisabled);
			return;
		}

		lock (_ordersInfo.SyncRoot)
		{
			if (!_ordersInfo.TryGetValue(order, out var info))
				throw new ArgumentException(LocalizedStrings.OrderNotFromStrategy.Put(order.TransactionId, Name));

			if (info.IsCanceled)
			{
				LogWarning(LocalizedStrings.OrderAlreadySentCancel, order.TransactionId);
				return;
			}

			info.IsCanceled = true;
		}

		CancelOrderHandler(order);
	}

	private void CancelOrderHandler(Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		LogInfo(LocalizedStrings.OrderCancelling + " " + order.TransactionId);

		OrderCanceling?.Invoke(order);

		SafeGetConnector().CancelOrder(order);
	}

	/// <summary>
	/// To add the order to the strategy.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="isChanging">The order came from the change event.</param>
	private void ProcessOrder(Order order, bool isChanging)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		var info = _ordersInfo.TryGetValue(order);

		var isRegistered = info != null && info.PrevState == OrderStates.Pending && (order.State == OrderStates.Active || order.State == OrderStates.Done);

		if (info != null)
			info.PrevState = order.State;

		if(IsDisposeStarted)
			return;

		if (isRegistered)
		{
			if (order.Type == OrderTypes.Conditional)
			{
				OnOrderRegistered(order);

				StatisticManager.AddNewOrder(order);
			}
			else
			{
				OnOrderRegistered(order);

				StatisticManager.AddNewOrder(order);

				if (order.Commission != null)
				{
					Commission += order.Commission;
					RaiseCommissionChanged();
				}
			}

			if (_firstOrderTime == default)
				_firstOrderTime = order.Time;

			_lastOrderTime = order.Time;

			ChangeLatency(order.LatencyRegistration);

			RecycleOrders();

			if (ProcessState == ProcessStates.Stopping && CancelOrdersWhenStopping)
			{
				lock (_ordersInfo.SyncRoot)
				{
					if (info == null)
						return;

					// для заявки уже был послан сигнал на снятие
					if (info.IsCanceled)
						return;

					info.IsCanceled = true;
				}

				CancelOrderHandler(order);
			}
		}
		else if (isChanging)
		{
			StatisticManager.AddChangedOrder(order);

			OrderChanged?.Invoke(order);
			ChangeLatency(order.LatencyCancellation);
		}
	}

	private void AttachOrder(Order order, bool restored)
	{
		LogInfo("Order {0} attached.", order.TransactionId);

		AddOrder(order, restored);

		ProcessOrder(order, false);

		OnOrderRegistering(order);
	}

	private void RecycleOrders()
	{
		if (OrdersKeepTime == TimeSpan.Zero)
			return;

		LogInfo(nameof(RecycleOrders));

		var diff = _lastOrderTime - _firstOrderTime;

		if (diff <= _maxOrdersKeepTime)
			return;

		_firstOrderTime = _lastOrderTime - OrdersKeepTime;

		_ordersInfo.SyncDo(d => d.RemoveWhere(o => o.Key.State == OrderStates.Done && o.Key.Time < _firstOrderTime));
	}

	/// <inheritdoc />
	public override DateTimeOffset CurrentTime => Connector?.CurrentTime ?? base.CurrentTime;

	/// <inheritdoc />
	protected override void RaiseLog(LogMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		switch (message.Level)
		{
			case LogLevels.Warning:
				if (ErrorState == LogLevels.Info)
					ErrorState = LogLevels.Warning;
				break;
			case LogLevels.Error:
				ErrorState = LogLevels.Error;
				break;
		}

		// mika
		// так как некоторые стратегии слишком много пишут в лог, то получается слишком медленно
		//
		//TryInvoke(() => base.RaiseLog(message));
		base.RaiseLog(message);
	}

	/// <summary>
	/// To start the trade algorithm.
	/// </summary>
	public virtual void Start()
	{
		_stopping = false;
		SafeGetConnector().SendOutMessage(new StrategyChangeStateMessage(this, ProcessStates.Started));
	}

	/// <summary>
	/// To stop the trade algorithm.
	/// </summary>
	public virtual void Stop()
	{
		if (ProcessState == ProcessStates.Stopped)
			return;

		_stopping = true;
		SafeGetConnector().SendOutMessage(new StrategyChangeStateMessage(this, ProcessStates.Stopping));
	}

	/// <summary>
	/// To stop the trade algorithm by error reason.
	/// </summary>
	/// <param name="error">Error.</param>
	public void Stop(Exception error)
	{
		LogError(error);

		LastError = error ?? throw new ArgumentNullException(nameof(error));
		Stop();
	}

	/// <summary>
	/// Keep trading statistics (orders, trades, profit etc.) after restart.
	/// </summary>
	[Browsable(false)]
	public bool KeepStatistics { get; set; }

	/// <summary>
	/// The event of the strategy re-initialization.
	/// </summary>
	public event Action Reseted;

	/// <summary>
	/// To call the event <see cref="Reseted"/>.
	/// </summary>
	private void RaiseReseted() => Reseted?.Invoke();

	/// <summary>
	/// To re-initialize the trade algorithm. It is called after initialization of the strategy object and loading stored parameters.
	/// </summary>
	public void Reset()
	{
		LogInfo(LocalizedStrings.Reset);

		Position[] positions = null;

		if (!KeepStatistics)
		{
			StatisticManager.Reset();

			PnLManager.Reset();

			Commission = default;
			Latency = default;
			Slippage = default;

			RiskManager.Reset();

			_myTrades.Clear();
			_ordersInfo.Clear();

			positions = _posManager.Positions;
			_posManager.Reset();
		}

		ProcessState = ProcessStates.Stopped;
		ErrorState = LogLevels.Info;
		LastError = default;
		TotalWorkingTime = default;
		StartedTime = default;

		_portfolioLookup = default;
		_orderLookup = default;

		_boardMsg = default;
		_firstOrderTime = _lastOrderTime = _lastPnlRefreshTime = _prevTradeDate = default;
		_idStr = default;

		_subscriptions.Clear();
		_subscriptionsById.Clear();
		_indicators.Clear();

		IsOnline = false;

		OnReseted();

		if (!KeepStatistics)
		{
			var time = CurrentTime;

			// события вызываем только после вызова Reseted
			// чтобы сбросить состояние у подписчиков стратегии.
			RaisePnLChanged(time);
			RaiseCommissionChanged();
			RaiseLatencyChanged();
			RaiseSlippageChanged();

			foreach (var position in positions)
			{
				position.CurrentValue = 0;
				_positionChanged?.Invoke(position);
			}

			RaisePositionChanged(time);
		}
	}

	/// <summary>
	/// It is called from the <see cref="Reset"/> method.
	/// </summary>
	protected virtual void OnReseted()
	{
		RaiseReseted();

		_protectiveController = default;
		_posController = default;
		_takeProfit = default;
		_stopLoss = default;
		_isStopTrailing = default;
		_takeTimeout = default;
		_stopTimeout = default;
		_protectiveUseMarketOrders = default;

		_chart = default;
		_drawingOrders = default;
		_drawingTrades = default;
		_ordersElems.Clear();
		_tradesElems.Clear();
		_subscriptionElems.Clear();
		_indElems.Clear();
	}

	void IMarketRuleContainer.SuspendRules()
	{
		_rulesSuspendCount++;

		LogDebug(LocalizedStrings.RulesSuspended, _rulesSuspendCount);
	}

	void IMarketRuleContainer.ResumeRules()
	{
		if (_rulesSuspendCount > 0)
		{
			_rulesSuspendCount--;

			if (_rulesSuspendCount == 0)
			{
				foreach (var subscription in _suspendSubscriptions.CopyAndClear())
					SubscriptionProvider.Subscribe(subscription);
			}
		}

		LogDebug(LocalizedStrings.RulesResume, _rulesSuspendCount);
	}

	private void TryFinalStop()
	{
		var rules = GetRules();

		if (rules.Any())
		{
			if (WaitRulesOnStop)
			{
				this.AddLog(LogLevels.Info,
					() => LocalizedStrings.AttemptsStopRules.Put(rules.Length, rules.Select(r => r.ToString()).JoinCommaSpace()));

				return;
			}

			foreach (var rule in rules)
				rule.Dispose();

			Rules.Clear();
		}

		ProcessState = ProcessStates.Stopped;

		if (DisposeOnStop)
		{
			Dispose();
		}
	}

	void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
	{
		if (_rulesSuspendCount > 0)
		{
			this.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.CannotProcessRulesSuspended);
			return;
		}

		try
		{
			this.ActiveRule(rule, process);
		}
		catch (Exception error)
		{
			OnError(this, error);
		}
		finally
		{
			if (_processState == ProcessStates.Stopping)
				TryFinalStop();
		}
	}

	private TimeSpan _unrealizedPnLInterval = TimeSpan.FromMinutes(1);

	/// <summary>
	/// The interval for unrealized profit recalculation. The default value is 1 minute.
	/// </summary>
	[Browsable(false)]
	public TimeSpan UnrealizedPnLInterval
	{
		get => _unrealizedPnLInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_unrealizedPnLInterval = value;
		}
	}

	/// <summary>
	/// To call the event <see cref="OrderRegistering"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	protected virtual void OnOrderRegistering(Order order)
	{
		OrderRegistering?.Invoke(order);
	}

	/// <summary>
	/// To call the event <see cref="OrderRegistered"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	protected virtual void OnOrderRegistered(Order order)
	{
		OrderRegistered?.Invoke(order);
	}

	/// <summary>
	/// To call the event <see cref="OrderReRegistering"/>.
	/// </summary>
	/// <param name="oldOrder">Cancelling order.</param>
	/// <param name="newOrder">New order to register.</param>
	protected virtual void OnOrderReRegistering(Order oldOrder, Order newOrder)
	{
		OrderReRegistering?.Invoke(oldOrder, newOrder);
	}

	/// <summary>
	/// The method, called at strategy order registration error.
	/// </summary>
	/// <param name="fail">Error registering order.</param>
	/// <param name="calcRisk">Invoke risk manager.</param>
	protected virtual void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
	{
		OrderRegisterFailed?.Invoke(fail);
		StatisticManager.AddRegisterFailedOrder(fail);

		if (calcRisk)
			ProcessRisk(() => fail.ToMessage(fail.Order.TransactionId));
	}

	private void OnConnectorNewMessage(Message message)
	{
		if(IsDisposeStarted)
			return;

		DateTimeOffset? msgTime = null;

		switch (message.Type)
		{
			case MessageTypes.QuoteChange:
			{
				// при тестировании на истории в стакане могут быть свои заявки по ценам планок,
				// исключаем эти цены из расчетов нереализованной прибыли
				// (убрать свои заявки из стакана не получается, т.к. заявка могла уже исполниться,
				// но сам стакан еще не обновился и придет только следующим сообщением).

				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					return;

				// TODO на истории когда в стакане будут свои заявки по планкам, то противополжная сторона стакана будет пустой
				// необходимо исключать свои заявки как-то иначе.
				if (quoteMsg.Asks.IsEmpty() || quoteMsg.Bids.IsEmpty())
					return;

				PnLManager.ProcessMessage(message);
				msgTime = quoteMsg.ServerTime;

				break;
			}

			case MessageTypes.Level1Change:
				PnLManager.ProcessMessage(message);
				msgTime = ((Level1ChangeMessage)message).ServerTime;
				break;

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					PnLManager.ProcessMessage(execMsg);

				msgTime = execMsg.ServerTime;
				break;
			}

			case MessageTypes.Time:
			{
				var timeMsg = (TimeMessage)message;

				if (timeMsg.IsBack())
					return;

				msgTime = CurrentTime;
				break;
			}

			case ExtendedMessageTypes.StrategyChangeState:
			{
				var stateMsg = (StrategyChangeStateMessage)message;

				if (stateMsg.Strategy == this)
				{
					switch (stateMsg.State)
					{
						case ProcessStates.Stopping:
						{
							if (ProcessState == ProcessStates.Started)
								ProcessState = ProcessStates.Stopping;
							else
								LogDebug(LocalizedStrings.StrategyStopping, ProcessState);

							break;
						}
						case ProcessStates.Started:
						{
							if (ProcessState == ProcessStates.Stopped)
								ProcessState = ProcessStates.Started;
							else
								LogDebug(LocalizedStrings.StrategyStarting, ProcessState);

							break;
						}
					}
				}

				return;
			}

			default:
			{
				if (message is CandleMessage)
					PnLManager.ProcessMessage(message);

				return;
			}
		}

		var unrealInterval = UnrealizedPnLInterval;
		if (msgTime == null || unrealInterval == default || (msgTime.Value - _lastPnlRefreshTime) < unrealInterval)
			return;

		_lastPnlRefreshTime = msgTime.Value;

		_boardMsg ??= Security?.Board?.ToMessage() ?? Portfolio?.Board?.ToMessage();

		if (_boardMsg is not null)
		{
			var date = _lastPnlRefreshTime.Date;

			if (date != _prevTradeDate)
			{
				_prevTradeDate = date;
				_isPrevDateTradable = _boardMsg.IsTradeDate(_prevTradeDate);
			}

			if (!_isPrevDateTradable)
				return;

			var period = _boardMsg.WorkingTime.GetPeriod(date);

			var tod = _lastPnlRefreshTime.TimeOfDay;

			if (period != null && !period.Times.IsEmpty() && !period.Times.Any(r => r.Contains(tod)))
				return;
		}

		if (Positions.Any())
			RaisePnLChanged(msgTime.Value);
	}

	private void OnConnectorOwnTradeReceived(Subscription subscription, MyTrade trade)
	{
		if (!CanProcess(subscription) || !_ordersInfo.ContainsKey(trade.Order) || !TryAddMyTrade(trade))
			return;

		TryInvoke(() =>
		{
			OwnTradeReceived?.Invoke(subscription, trade);

			if (subscription == OrderLookup)
			{
				OnOwnTradeReceived(trade);
				_drawingTrades?.Add(trade);
			}

			if (_protectiveController is null)
				return;

			var security = trade.Order.Security;
			var portfolio = trade.Order.Portfolio;

			_posController ??= _protectiveController.GetController(
				security.ToSecurityId(),
				portfolio.Name,
				new LocalProtectiveBehaviourFactory(security.PriceStep, security.Decimals),
				_takeProfit ?? new(), _stopLoss ?? new(), _isStopTrailing, _takeTimeout, _stopTimeout, _protectiveUseMarketOrders);

			var info = _posController?.Update(trade.Trade.Price, trade.GetPosition(), trade.Trade.ServerTime);

			if (info is not null)
				ActiveProtection(info.Value);
		});
	}

	/// <summary>
	/// Determines the specified order can be owned by the strategy.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns>Check result.</returns>
	protected virtual bool CanAttach(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.UserOrderId.EqualsIgnoreCase(EnsureGetId());
	}

	private void OnConnectorOrderReceived(Subscription subscription, Order order)
	{
		if (!CanProcess(subscription))
			return;

		if (!_ordersInfo.ContainsKey(order) && CanAttach(order))
			AttachOrder(order, true);
		else if (_ordersInfo.ContainsKey(order))
			TryInvoke(() => ProcessOrder(order, true));

		TryInvoke(() =>
		{
			OrderReceived?.Invoke(subscription, order);

			_posManager.ProcessOrder(order);

			if (subscription == OrderLookup)
			{
				OnOrderReceived(order);
				_drawingOrders?.Add(order);
			}
		});
	}

	/// <summary>
	/// Order received.
	/// </summary>
	/// <param name="order"><see cref="Order"/></param>
	protected virtual void OnOrderReceived(Order order)
	{
	}

	/// <summary>
	/// Own trade received.
	/// </summary>
	/// <param name="trade"><see cref="MyTrade"/></param>
	protected virtual void OnOwnTradeReceived(MyTrade trade)
	{
	}

	private void OnConnectorOrderEditFailed(long transactionId, OrderFail fail)
	{
		if(IsDisposeStarted)
			return;

		if (_ordersInfo.ContainsKey(fail.Order))
			OrderEditFailed?.Invoke(transactionId, fail);
	}

	private void OnConnectorOrderEdited(long transactionId, Order order)
	{
		if(IsDisposeStarted)
			return;

		if (_ordersInfo.ContainsKey(order))
		{
			OrderEdited?.Invoke(transactionId, order);
			ChangeLatency(order.LatencyEdition);
		}
	}

	private string _idStr;
	private string EnsureGetId() => _idStr ??= Id.To<string>();

	private void OnConnectorOrderRegisterFailed(OrderFail fail)
	{
		if(IsDisposeStarted)
			return;

		if (_ordersInfo.ContainsKey(fail.Order))
			OnOrderRegisterFailed(fail, true);
	}

	/// <summary>
	/// Try add own trade.
	/// </summary>
	/// <param name="trade"><see cref="MyTrade"/></param>
	/// <returns>Operation result.</returns>
	public bool TryAddMyTrade(MyTrade trade)
	{
		if (!_myTrades.TryAdd(trade))
			return false;

		var order = trade.Order;
		var tick = trade.Trade;

		if (WaitAllTrades)
		{
			lock (_ordersInfo.SyncRoot)
			{
				if (_ordersInfo.TryGetValue(order, out var info))
					info.ReceivedVolume += tick.Volume;
			}
		}

		var isComChanged = false;
		var isSlipChanged = false;

		LogInfo("{0} trade {1} at price {2} for {3} orders {4}.",
			order.Side,
			(tick.Id is null ? tick.StringId : tick.Id.To<string>()),
			tick.Price, tick.Volume, order.TransactionId);

		if (trade.Commission != null)
		{
			Commission ??= 0;

			Commission += trade.Commission.Value;
			isComChanged = true;
		}

		var tradeSec = order.Security;

		PnLManager.UpdateSecurity(new Level1ChangeMessage
		{
			SecurityId = tradeSec.ToSecurityId(),
			ServerTime = CurrentTime
		}
		.TryAdd(Level1Fields.PriceStep, tradeSec.PriceStep)
		.TryAdd(Level1Fields.StepPrice, this.GetSecurityValue<decimal?>(tradeSec, Level1Fields.StepPrice) ?? tradeSec.StepPrice)
		.TryAdd(Level1Fields.Multiplier, this.GetSecurityValue<decimal?>(tradeSec, Level1Fields.Multiplier) ?? tradeSec.Multiplier)
		);

		var execMsg = trade.ToMessage();
		DateTimeOffset? pnLChangeTime = null;

		var tradeInfo = PnLManager.ProcessMessage(execMsg);

		if (tradeInfo != null)
		{
			if (tradeInfo.PnL != 0)
			{
				pnLChangeTime = execMsg.LocalTime;

				trade.PnL ??= tradeInfo.PnL;
			}

			StatisticManager.AddMyTrade(tradeInfo);
		}

		if (trade.Slippage is decimal slippage)
		{
			Slippage = (Slippage ?? 0) + slippage;

			isSlipChanged = true;
		}

		trade.Position ??= GetPositionValue(tradeSec, order.Portfolio);

		TryInvoke(() => NewMyTrade?.Invoke(trade));

		TryInvoke(() =>
		{
			if (isComChanged)
				RaiseCommissionChanged();
		});
		TryInvoke(() =>
		{
			if (pnLChangeTime is not null)
				RaisePnLChanged(pnLChangeTime.Value);
		});
		TryInvoke(() =>
		{
			if (isSlipChanged)
				RaiseSlippageChanged();
		});

		ProcessRisk(() => execMsg);

		return true;
	}

	private void RaiseSlippageChanged()
	{
		this.Notify(nameof(Slippage));
		SlippageChanged?.Invoke();
	}

	private void RaiseCommissionChanged()
	{
		this.Notify(nameof(Commission));
		CommissionChanged?.Invoke();
	}

	private void RaisePnLChanged(DateTimeOffset time)
	{
		this.Notify(nameof(PnL));
		PnLChanged?.Invoke();

		PnLReceived?.Invoke(PortfolioLookup);

		var evt = PnLReceived2;
		var pf = Portfolio;

		if (evt is not null && pf is not null)
		{
			var manager = PnLManager;
			evt(PortfolioLookup, pf, time, manager.RealizedPnL, manager.UnrealizedPnL, Commission);

			ProcessRisk(() => new PositionChangeMessage
			{
				PortfolioName = pf.Name,
				SecurityId = SecurityId.Money,
				ServerTime = time,
				OriginalTransactionId = PortfolioLookup.TransactionId,
			}
			.TryAdd(PositionChangeTypes.RealizedPnL, manager.RealizedPnL)
			.TryAdd(PositionChangeTypes.UnrealizedPnL, manager.UnrealizedPnL)
			.TryAdd(PositionChangeTypes.Commission, Commission)
			);
		}

		StatisticManager.AddPnL(_lastPnlRefreshTime, PnL, Commission);
	}

	private void RaiseLatencyChanged()
	{
		this.Notify(nameof(Latency));
		LatencyChanged?.Invoke();
	}

	private void ChangeLatency(TimeSpan? diff)
	{
		if (diff == null || diff == TimeSpan.Zero)
			return;

		if (Latency == null)
			Latency = TimeSpan.Zero;

		Latency += diff.Value;
		RaiseLatencyChanged();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		var parameters = storage.GetValue<SettingsStorage[]>(nameof(Parameters));

		if (parameters is not null)
		{
			// в настройках могут быть дополнительные параметры, которые будут добавлены позже
			foreach (var s in parameters)
			{
				if (Parameters.TryGetValue(s.GetValue<string>(nameof(IStrategyParam.Id)), out var param))
					param.Load(s);
			}
		}

		RiskManager.LoadIfNotNull(storage, nameof(RiskManager));

		if (!KeepStatistics)
			return;
		
		PnLManager.LoadIfNotNull(storage, nameof(PnLManager));
		StatisticManager.LoadIfNotNull(storage, nameof(StatisticManager));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		Save(storage, KeepStatistics, true);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage"><see cref="SettingsStorage"/></param>
	/// <param name="saveStatistics"><see cref="KeepStatistics"/></param>
	/// <param name="saveSystemParameters">Save system parameters.</param>
	public void Save(SettingsStorage storage, bool saveStatistics, bool saveSystemParameters/*, bool saveSecurity, bool savePortfolio*/)
	{
		var parameters = GetParameters();

		if (!saveSystemParameters)
			parameters = [.. parameters.Except(_systemParams)];

		storage
			.Set(nameof(Parameters), parameters.Select(p => p.Save()).ToArray())
			.Set(nameof(RiskManager), RiskManager.Save())
		;

		if (saveStatistics)
		{
			storage
				.Set(nameof(PnLManager), PnLManager.Save())
				.Set(nameof(StatisticManager), StatisticManager.Save())
			;
		}
	}

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	void INotifyPropertyChangedEx.NotifyPropertyChanged(string info)
	{
		PropertyChanged?.Invoke(this, info);
	}

	/// <summary>
	/// To cancel all active orders (to stop and regular).
	/// </summary>
	/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
	/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
	/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
	/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
	/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
	/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
	/// <param name="transactionId">Order cancellation transaction id.</param>
	public void CancelActiveOrders(bool? isStopOrder = default, Portfolio portfolio = default, Sides? direction = default, ExchangeBoard board = default, Security security = default, SecurityTypes? securityType = default, long? transactionId = default)
	{
		if (ProcessState != ProcessStates.Started)
		{
			LogWarning(LocalizedStrings.StrategyInStateCannotCancelOrder, ProcessState);
			return;
		}

		LogInfo(LocalizedStrings.CancelAll);

		ProcessCancelActiveOrders(isStopOrder, portfolio, direction, board, security, securityType, transactionId);
	}

	private void ProcessCancelActiveOrders(bool? isStopOrder, Portfolio portfolio, Sides? direction, ExchangeBoard board, Security security, SecurityTypes? securityType, long? transactionId)
	{
		_ordersInfo.SyncGet(d => d.Keys.Filter(OrderStates.Active).ToArray()).ForEach(o =>
		{
			var info = _ordersInfo.TryGetValue(o);

			if (isStopOrder is not null && isStopOrder != (o.Type == OrderTypes.Conditional))
				return;

			if (portfolio is not null && o.Portfolio != portfolio)
				return;

			if (direction is not null && o.Side != direction)
				return;

			if (board is not null && o.Security.Board != board)
				return;

			if (security is not null && o.Security != security)
				return;

			if (securityType is not null && o.Security.Type != securityType)
				return;

			if (transactionId is not null && o.TransactionId != transactionId)
				return;

			if (info.IsCanceled)
			{
				LogWarning(LocalizedStrings.OrderAlreadySentCancel, o.TransactionId);
				return;
			}

			info.IsCanceled = true;

			CancelOrderHandler(o);
		});
	}

	private void ProcessCancelOrderFail(OrderFail fail)
	{
		if(IsDisposeStarted)
			return;

		var order = fail.Order;

		lock (_ordersInfo.SyncRoot)
		{
			if (!_ordersInfo.TryGetValue(order, out var info))
				return;

			info.IsCanceled = false;
		}

		LogError(LocalizedStrings.ErrorCancellingOrder, order.TransactionId, fail.Error);

		OrderCancelFailed?.Invoke(fail);

		StatisticManager.AddFailedOrderCancel(fail);
	}

	/// <summary>
	/// Processing of error, occurred as result of strategy operation.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="error">Error.</param>
	protected virtual void OnError(Strategy strategy, Exception error)
	{
		ProcessRisk(() => error.ToErrorMessage());

		Error?.Invoke(strategy, error);

		LogError(error.ToString());
	}

	object ICloneable.Clone() => Clone();

	/// <inheritdoc />
	public virtual Strategy Clone()
	{
		var clone = CreateClone();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Create clone object (non-initialized).
	/// </summary>
	/// <returns><see cref="Strategy"/></returns>
	protected virtual Strategy CreateClone()
		=> GetType().CreateInstance<Strategy>();

	/// <summary>
	/// Copy settings into <paramref name="copy"/>.
	/// </summary>
	/// <param name="copy"><see cref="Strategy"/></param>
	protected virtual void CopyTo(Strategy copy)
	{
		if (copy is null)
			throw new ArgumentNullException(nameof(copy));

		//copy.Connector = Connector;
		copy.Security = Security;
		copy.Portfolio = Portfolio;
		copy.PortfolioProvider = PortfolioProvider;

		var id = copy.Id;
		copy.Load(this.Save());
		copy.Id = id;

		copy.Environment.AddRange(Environment);
	}

	/// <summary>
	/// <see cref="IPortfolioProvider"/>
	/// </summary>
	[Browsable(false)]
	public IPortfolioProvider PortfolioProvider { get; set; }

	private void TryInvoke(Action handler)
	{
		if (!IsDisposeStarted)
			handler();
	}

	private RiskActions? ProcessRisk(Func<Message> getMessage)
	{
		if (getMessage is null)
			throw new ArgumentNullException(nameof(getMessage));

		if (RiskManager.Rules.Count == 0)
			return null;

		foreach (var rule in RiskManager.ProcessRules(getMessage()))
		{
			LogWarning(LocalizedStrings.ActivatingRiskRule,
				rule.Name, rule.Title, rule.Action);

			switch (rule.Action)
			{
				case RiskActions.ClosePositions:
					ClosePosition();
					return rule.Action;
				case RiskActions.StopTrading:
					Stop();
					return rule.Action;
				case RiskActions.CancelOrders:
					CancelActiveOrders();
					return rule.Action;
				default:
					throw new InvalidOperationException(rule.Action.ToString());
			}
		}
		
		return null;
	}

	/// <summary>
	/// Get all securities required for strategy.
	/// </summary>
	/// <returns>Securities.</returns>
	public virtual IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [];

	/// <summary>
	/// Get all portfolios required for strategy.
	/// </summary>
	/// <returns>Portfolios.</returns>
	public virtual IEnumerable<Portfolio> GetWorkingPortfolios()
	{
		var pf = Portfolio;

		if (pf is not null)
			yield return pf;
	}

	private readonly StrategyParam<TimeSpan?> _historySize;

	/// <summary>
	/// History to initialize the strategy on Live trading.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.GeneralKey,
		Name = LocalizedStrings.DaysHistoryKey,
		Description = LocalizedStrings.DaysHistoryDescKey,
		Order = 20)]
	[TimeSpanEditor(Mask = TimeSpanEditorMask.Days | TimeSpanEditorMask.Hours | TimeSpanEditorMask.Minutes | TimeSpanEditorMask.Seconds)]
	public TimeSpan? HistorySize
	{
		get => _historySize.Value;
		set => _historySize.Value = value;
	}

	/// <summary>
	/// Calculated from code version of <see cref="HistorySize"/>.
	/// </summary>
	protected virtual TimeSpan? HistoryCalculated => null;

	/// <summary>
	/// Determines <see cref="Connector"/> is <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	[Browsable(false)]
	public bool IsBacktesting => Connector is HistoryEmulationConnector;

	/// <summary>
	/// Apply incoming command.
	/// </summary>
	/// <param name="cmdMsg">The message contains information about command to change state.</param>
	public virtual void ApplyCommand(CommandMessage cmdMsg)
	{
		if (cmdMsg == null)
			throw new ArgumentNullException(nameof(cmdMsg));

		var parameters = cmdMsg.Parameters;

		switch (cmdMsg.Command)
		{
			case CommandTypes.Start:
			{
				Start();
				break;
			}

			case CommandTypes.Stop:
			{
				Stop();
				break;
			}

			case CommandTypes.CancelOrders:
			{
				CancelActiveOrders();
				break;
			}

			case CommandTypes.RegisterOrder:
			{
				var secId = parameters.TryGet(nameof(Order.Security));
				var pfName = parameters.TryGet(nameof(Order.Portfolio));
				var side = parameters[nameof(Order.Side)].To<Sides>();
				var volume = parameters[nameof(Order.Volume)].To<decimal>();
				var price = parameters.TryGet(nameof(Order.Price)).To<decimal?>() ?? 0;
				var comment = parameters.TryGet(nameof(Order.Comment));
				var clientCode = parameters.TryGet(nameof(Order.ClientCode));
				var tif = parameters.TryGet(nameof(Order.TimeInForce)).To<TimeInForce?>();

				var order = new Order
				{
					Security = secId.IsEmpty() ? Security : this.LookupById(secId),
					Portfolio = pfName.IsEmpty() ? Portfolio : Connector.LookupByPortfolioName(pfName),
					Side = side,
					Volume = volume,
					Price = price,
					Comment = comment,
					ClientCode = clientCode,
					TimeInForce = tif,
				};

				RegisterOrder(order);

				break;
			}

			case CommandTypes.CancelOrder:
			{
				var orderId = parameters[nameof(Order.Id)].To<long>();

				CancelOrder(Orders.First(o => o.Id == orderId));

				break;
			}

			case CommandTypes.ClosePosition:
			{
				ClosePosition();
				break;
			}
		}
	}

	private readonly object _onlineStateLock = new();

	private void CheckRefreshOnlineState()
	{
		bool wasOnline, nowOnline;

		lock (_onlineStateLock)
		{
			wasOnline = IsOnline;

			nowOnline = ProcessState == ProcessStates.Started;

			if (nowOnline)
				nowOnline = _subscriptions.SyncGet(d => d.CachedKeys.Where(s => !s.SubscriptionMessage.IsHistoryOnly()).All(s => s.State == SubscriptionStates.Online));

			if (nowOnline == wasOnline)
				return;

			IsOnline = nowOnline;
		}

		LogInfo("IsOnline: {0} ==> {1}. state={2}", wasOnline, nowOnline, ProcessState);

		IsOnlineChanged?.Invoke(this);
	}

	private void UnSubscribe(bool globalAndLocal)
	{
		foreach (var pair in _subscriptions.CachedPairs)
		{
			if (globalAndLocal || !pair.Value)
			{
				var subscription = pair.Key;

				if (subscription.State.IsActive())
					UnSubscribe(subscription);

				_subscriptions.Remove(subscription);
				_subscriptionsById.Remove(subscription.TransactionId);
			}
		}
	}

	/// <summary>
	/// All possible <see cref="IOrderBookMessage"/> sources that can be received via <see cref="OrderBookDrawing"/>.
	/// </summary>
	[Browsable(false)]
	public virtual IEnumerable<IOrderBookSource> OrderBookSources
		=> [];

	/// <summary>
	/// <see cref="DrawOrderBook"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, IOrderBookMessage> OrderBookDrawing;

	/// <summary>
	/// <see cref="DrawOrderBookOrder"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, Order> OrderBookDrawingOrder;

	/// <summary>
	/// <see cref="DrawOrderBookOrderFail"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, OrderFail> OrderBookDrawingOrderFail;

	/// <summary>
	/// Draw <see cref="IOrderBookMessage"/>.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	public void DrawOrderBook(Subscription subscription, IOrderBookSource source, IOrderBookMessage book)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (book is null)			throw new ArgumentNullException(nameof(book));

		OrderBookDrawing?.Invoke(subscription, source, book);
	}

	/// <summary>
	/// Draw order book order.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="order">Order.</param>
	public void DrawOrderBookOrder(Subscription subscription, IOrderBookSource source, Order order)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (order is null)			throw new ArgumentNullException(nameof(order));

		OrderBookDrawingOrder?.Invoke(subscription, source, order);
	}

	/// <summary>
	/// Draw order book order fail.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="fail">Order fail.</param>
	public void DrawOrderBookOrderFail(Subscription subscription, IOrderBookSource source, OrderFail fail)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (fail is null)			throw new ArgumentNullException(nameof(fail));

		OrderBookDrawingOrderFail?.Invoke(subscription, source, fail);
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		Connector = null;

		Parameters.Dispose();

		base.DisposeManaged();
	}
}
