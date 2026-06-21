namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Risk;

partial class Strategy
{
	// All persisted settings are backed by StrategyParam<T> so they are registered in Parameters,
	// round-trip through Save/Load and carry Display/validation/optimization metadata, exactly as the
	// monolith Strategy does. The parameters are created in the constructor (Strategy.cs) where the
	// Parameters dictionary is initialized; the backing fields live here.

	/// <summary>
	/// Make <see cref="Security"/> and <see cref="Portfolio"/> as non-browsable.
	/// </summary>
	public static bool HideSecurityAndPortfolioParameters { get; set; }

	// System (non-user-strategy) parameters; excluded from Save when system parameters are not requested.
	private IStrategyParam[] _systemParams;

	private StrategyParam<Guid> _id;

	/// <summary>
	/// Strategy ID.
	/// </summary>
	public override Guid Id
	{
		get => _id.Value;
		set => _id.Value = value;
	}

	private StrategyParam<LogLevels> _logLevel;

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

	private StrategyParam<Security> _security;

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
		set
		{
			if (_security.Value == value)
				return;

			_security.Value = value;
			RaiseParametersChanged(nameof(Security));
		}
	}

	private StrategyParam<Portfolio> _portfolio;

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
		set
		{
			if (_portfolio.Value == value)
				return;

			_portfolio.Value = value;
			RaiseParametersChanged(nameof(Portfolio));
		}
	}

	private StrategyParam<decimal> _volume;

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
		set
		{
			if (_volume.Value == value)
				return;

			_volume.Value = value;
			RaiseParametersChanged(nameof(Volume));
		}
	}

	private StrategyParam<StrategyCommentModes> _commentMode;

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

	private StrategyParam<StrategyTradingModes> _tradingMode;

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

	private StrategyParam<bool> _waitAllTrades;

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

	private StrategyParam<TimeSpan> _ordersKeepTime;

	/// <summary>
	/// The time for storing <see cref="Orders"/> in memory. By default it equals to 1 day. If value is set in <see cref="TimeSpan.Zero"/>, orders will not be deleted.
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

	private StrategyParam<bool> _cancelOrdersWhenStopping;

	/// <summary>
	/// To cancel active orders at stop. Is On by default.
	/// </summary>
	[Browsable(false)]
	public bool CancelOrdersWhenStopping
	{
		get => _cancelOrdersWhenStopping.Value;
		set => _cancelOrdersWhenStopping.Value = value;
	}

	private StrategyParam<bool> _unsubscribeOnStop;

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

	private StrategyParam<bool> _disposeOnStop;

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

	private StrategyParam<bool> _waitRulesOnStop;

	/// <summary>
	/// Wait <see cref="Rules"/> to finish before strategy become into <see cref="ProcessStates.Stopped"/> state.
	/// </summary>
	[Browsable(false)]
	public bool WaitRulesOnStop
	{
		get => _waitRulesOnStop.Value;
		set => _waitRulesOnStop.Value = value;
	}

	private StrategyParam<decimal> _riskFreeRate;

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

	private StrategyParam<Level1Fields?> _indicatorSource;

	/// <summary>
	/// Default source for indicators when <see cref="IIndicator.Source"/> is not set.
	/// </summary>
	[ItemsSource(typeof(SourceItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IndicatorSourceKey,
		Description = LocalizedStrings.IndicatorSourceDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 12)]
	public Level1Fields? IndicatorSource
	{
		get => _indicatorSource.Value;
		set => _indicatorSource.Value = value;
	}

	private StrategyParam<TimeSpan?> _historySize;

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
	/// Keep trading statistics (orders, trades, profit etc.) after restart.
	/// </summary>
	[Browsable(false)]
	public bool KeepStatistics { get; set; }

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
		get => RiskManager.Rules;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (value.HasNullItem())
				throw new ArgumentException(LocalizedStrings.ProcessNullValues, nameof(value));

			var rules = RiskManager.Rules;
			rules.Clear();
			rules.AddRange(value);

			RaiseParametersChanged(nameof(RiskRules));
		}
	}

	// Registers every persisted setting as a StrategyParam, mirroring the monolith Strategy ctor.
	// Called from the constructor right after Parameters is created.
	private void InitParameters()
	{
		_id = Param(nameof(Id), base.Id).SetHidden().SetReadOnly();
		_logLevel = Param(nameof(LogLevel), LogLevels.Inherit).SetDisplay(LocalizedStrings.LogLevel, LocalizedStrings.LogLevelKey, LocalizedStrings.Logging).SetBasic(false);
		_security = Param<Security>(nameof(Security)).SetDisplay(LocalizedStrings.Security, LocalizedStrings.StrategySecurity, LocalizedStrings.General).SetNonBrowsable(HideSecurityAndPortfolioParameters);
		_portfolio = Param<Portfolio>(nameof(Portfolio)).SetDisplay(LocalizedStrings.Portfolio, LocalizedStrings.StrategyPortfolio, LocalizedStrings.General).SetNonBrowsable(HideSecurityAndPortfolioParameters).SetCanOptimize(false);
		_volume = Param(nameof(Volume), 1m).SetGreaterThanZero().SetDisplay(LocalizedStrings.Volume, LocalizedStrings.StrategyVolume, LocalizedStrings.General);
		_commentMode = Param(nameof(CommentMode), StrategyCommentModes.Disabled).SetDisplay(LocalizedStrings.Comment, LocalizedStrings.OrderComment, LocalizedStrings.General).SetBasic(false);
		_tradingMode = Param(nameof(TradingMode), StrategyTradingModes.Full).SetDisplay(LocalizedStrings.Trading, LocalizedStrings.AllowTrading, LocalizedStrings.General).SetBasic(false);
		_waitAllTrades = Param(nameof(WaitAllTrades), false).SetCanOptimize(false).SetHidden();
		_ordersKeepTime = Param(nameof(OrdersKeepTime), TimeSpan.FromDays(1)).SetNotNegative().SetDisplay(LocalizedStrings.Orders, LocalizedStrings.OrdersKeepTime, LocalizedStrings.General).SetBasic(false).SetCanOptimize(false);
		_cancelOrdersWhenStopping = Param(nameof(CancelOrdersWhenStopping), true).SetCanOptimize(false).SetHidden();
		_unsubscribeOnStop = Param(nameof(UnsubscribeOnStop), true).SetCanOptimize(false).SetHidden();
		_disposeOnStop = Param(nameof(DisposeOnStop), false).SetCanOptimize(false).SetHidden();
		_waitRulesOnStop = Param(nameof(WaitRulesOnStop), false).SetCanOptimize(false).SetHidden();
		_riskFreeRate = Param<decimal>(nameof(RiskFreeRate)).SetDisplay(LocalizedStrings.RiskFreeRate, LocalizedStrings.RiskFreeRateDesc, LocalizedStrings.General).SetCanOptimize(false);
		_indicatorSource = Param<Level1Fields?>(nameof(IndicatorSource)).SetDisplay(LocalizedStrings.IndicatorSource, LocalizedStrings.IndicatorSourceDesc, LocalizedStrings.General).SetCanOptimize(false);
		_historySize = Param<TimeSpan?>(nameof(HistorySize)).SetNullOrNotNegative().SetDisplay(LocalizedStrings.DaysHistory, LocalizedStrings.DaysHistoryDesc, LocalizedStrings.General).SetBasic(false).SetCanOptimize(false);
		_workingTime = Param(nameof(WorkingTime), new WorkingTime()).SetRequired().SetDisplay(LocalizedStrings.WorkingTime, LocalizedStrings.WorkingHours, LocalizedStrings.General).SetBasic(false);

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
	}
}
