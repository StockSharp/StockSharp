namespace StockSharp.Algo.Strategies;

using StockSharp.Algo;
using StockSharp.Alerts;
using StockSharp.Reporting;

// MarketRulesAndMisc subsystem ported from the monolith StrategyOld onto the decomposed engine.
//
// This file re-implements, with identical public signatures, the parts of the monolith surface
// that the decomposed Strategy.cs (and its already-present partials) does not yet provide:
//
//   - IMarketRuleContainer (Rules, IsRulesSuspended, SuspendRules, ResumeRules, ActivateRule);
//   - IScheduledTask (WorkingTime, CanStart, CanStop);
//   - IReportSource (ReportSource aggregation + Prepare + the report projections);
//   - ICustomTypeDescriptor (parameters projected as property descriptors, GetParameters);
//   - ICloneable<Strategy> (Clone / CreateClone / CopyTo);
//   - the misc members UnrealizedPnLInterval, PortfolioProvider, Environment, GetWorkingPortfolios,
//     ToReportValue and the alert-service helpers (GetAlertService / SetAlertService).
//
// IPortfolioProvider is NOT re-declared here: the decomposed Strategy already lists it on its
// primary declaration and implements it in Strategy_Positions.cs.
//
// Where the monolith stored state inside its Environment SettingsStorage (alert service), the
// decomposed Strategy has no such member, so a minimal backing field is used instead - mirroring
// the GetChart/SetChart precedent already established in Strategy_HighLevelCharting.cs.
partial class Strategy : IMarketRuleContainer, ICloneable<Strategy>, IScheduledTask, IReportSource, ICustomTypeDescriptor
{
	#region IMarketRuleContainer

	private MarketRuleList _rules;
	private int _rulesSuspendCount;
	private bool _ruleLifecycleHooked;

	/// <summary>
	/// Rule list backed by the strategy. Adding is blocked once the strategy is stopping, matching
	/// the monolith StrategyRuleList behaviour.
	/// </summary>
	private sealed class StrategyRuleList(Strategy strategy)
		: MarketRuleList(strategy)
	{
		private readonly Strategy _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

		protected override bool OnAdding(IMarketRule item)
			=> _strategy.ProcessState != ProcessStates.Stopping && base.OnAdding(item);
	}

	/// <inheritdoc />
	[Browsable(false)]
	public IMarketRuleList Rules
	{
		get
		{
			if (_rules is null)
			{
				_rules = new StrategyRuleList(this);
				EnsureRuleLifecycleHook();
			}

			return _rules;
		}
	}

	// The monolith disposed rules from TryFinalStop (respecting WaitRulesOnStop) and cleared them on
	// Reset / OnReseted. The decomposed engine drives the state machine itself, so the rule cleanup is
	// attached lazily - and only once - the first time the rule list is materialised, leaving strategies
	// that never use rules with no extra wiring.
	private void EnsureRuleLifecycleHook()
	{
		if (_ruleLifecycleHooked)
			return;

		_ruleLifecycleHooked = true;

		Reseted += DisposeRules;
		ProcessStateChanged += _ =>
		{
			if (ProcessState == ProcessStates.Stopped && !WaitRulesOnStop)
				DisposeRules();
		};
	}

	private void DisposeRules()
	{
		if (_rules is null)
			return;

		foreach (var rule in GetRules())
			rule.Dispose();

		_rules.Clear();
	}

	private IMarketRule[] GetRules()
	{
		if (_rules is null)
			return [];

		using (_rules.EnterScope())
			return [.. _rules];
	}

	/// <inheritdoc />
	[Browsable(false)]
	public bool IsRulesSuspended => _rulesSuspendCount > 0;

	void IMarketRuleContainer.SuspendRules()
	{
		_rulesSuspendCount++;

		this.AddDebugLog(LocalizedStrings.RulesSuspended, _rulesSuspendCount);
	}

	void IMarketRuleContainer.ResumeRules()
	{
		if (_rulesSuspendCount > 0)
			_rulesSuspendCount--;

		this.AddDebugLog(LocalizedStrings.RulesResume, _rulesSuspendCount);
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
			OnError(error);
		}
		finally
		{
			// Mirror the monolith ActivateRule finally: a rule completing while stopping may have been the
			// last gate, so re-drive the final stop. The engine no-ops unless still stopping and unblocked.
			if (ProcessState == ProcessStates.Stopping)
				Engine.TryFinalStopAsync(default).NoWait();
		}
	}

	/// <summary>
	/// Final-stop gate mirroring the monolith TryFinalStop: deny the Stopping -&gt; Stopped transition only
	/// while <see cref="WaitRulesOnStop"/> is set and there are still outstanding rules to drain.
	/// </summary>
	private bool CanFinalStop()
		=> !(WaitRulesOnStop && GetRules().Length > 0);

	#endregion

	#region IScheduledTask

	private StrategyParam<WorkingTime> _workingTime;

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
		set
		{
			if (_workingTime.Value == value)
				return;

			_workingTime.Value = value ?? throw new ArgumentNullException(nameof(value));
			RaiseParametersChanged(nameof(WorkingTime));
		}
	}

	bool IScheduledTask.CanStart => ProcessState == ProcessStates.Stopped;
	bool IScheduledTask.CanStop => ProcessState == ProcessStates.Started;

	#endregion

	#region Misc

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

			// Keep the engine-driven PnL refresh cadence in sync with the public API.
			Engine.UnrealizedPnLInterval = value;
		}
	}

	/// <summary>
	/// Strategy environment parameters.
	/// </summary>
	[Browsable(false)]
	public SettingsStorage Environment { get; } = [];

	/// <summary>
	/// <see cref="IPortfolioProvider"/> used to resolve portfolios by name.
	/// </summary>
	[Browsable(false)]
	public IPortfolioProvider PortfolioProvider { get; set; }

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

	private const string _keyAlertService = "AlertService";

	/// <summary>
	/// To get the <see cref="IAlertNotificationService"/> associated with the strategy.
	/// </summary>
	/// <returns>Alert notification service.</returns>
	public IAlertNotificationService GetAlertService()
		=> Environment.GetValue<IAlertNotificationService>(_keyAlertService);

	/// <summary>
	/// To set a <see cref="IAlertNotificationService"/> for the strategy.
	/// </summary>
	/// <param name="service">Alert notification service.</param>
	public void SetAlertService(IAlertNotificationService service)
		=> Environment.SetValue(_keyAlertService, service);

	#endregion

	#region ICloneable<Strategy>

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

		// Round-trip full persisted state through Save/Load as the monolith CopyTo does (a parameter-by-id
		// copy would drop the RiskManager). The clone keeps its OWN id, restored after Load.
		var id = copy.Id;
		copy.Load(this.Save());
		copy.Id = id;

		// set after Load to avoid being overwritten by the deserialized values
		copy.Security = Security;
		copy.Portfolio = Portfolio;
		copy.PortfolioProvider = PortfolioProvider;

		copy.Environment.AddRange(Environment);
	}

	#endregion

	#region IReportSource

	private readonly ReportSource _reportSource = new();

	/// <summary>
	/// Report data source with aggregation support.
	/// </summary>
	[Browsable(false)]
	public ReportSource ReportSource => _reportSource;

	/// <summary>
	/// Maximum number of orders before automatic aggregation is triggered.
	/// Default is 10000. Set to 0 to disable automatic aggregation.
	/// </summary>
	[Browsable(false)]
	public int MaxOrdersBeforeAggregation
	{
		get => _reportSource.MaxOrdersBeforeAggregation;
		set => _reportSource.MaxOrdersBeforeAggregation = value;
	}

	/// <summary>
	/// Maximum number of trades before automatic aggregation is triggered.
	/// Default is 10000. Set to 0 to disable automatic aggregation.
	/// </summary>
	[Browsable(false)]
	public int MaxTradesBeforeAggregation
	{
		get => _reportSource.MaxTradesBeforeAggregation;
		set => _reportSource.MaxTradesBeforeAggregation = value;
	}

	/// <summary>
	/// Time interval for aggregation. Orders/trades within the same interval are grouped together.
	/// Default is 1 hour. Set to <see cref="TimeSpan.Zero"/> to disable time-based grouping.
	/// </summary>
	[Browsable(false)]
	public TimeSpan AggregationInterval
	{
		get => _reportSource.AggregationInterval;
		set => _reportSource.AggregationInterval = value;
	}

	/// <inheritdoc />
	public void Prepare()
	{
		_reportSource.Name = Name;
		_reportSource.TotalWorkingTime = TotalWorkingTime;
		_reportSource.Commission = Commission;
		_reportSource.Position = Position;
		_reportSource.PnL = PnL;
		_reportSource.Slippage = Slippage;
		_reportSource.Latency = Latency;

		// Projections built from the decomposed pipelines, since the monolith fed them incrementally.
		_reportSource.ClearOrders();
		foreach (var order in Orders)
			AddOrderToReport(order);

		_reportSource.ClearTrades();
		foreach (var trade in Trades.MyTrades)
			AddTradeToReport(trade);

		// Sync parameters
		_reportSource.ClearParameters();
		foreach (var p in GetParameters().Where(p => p.IsBrowsable()))
			_reportSource.AddParameter(p.GetName(), ToReportValue(p.Value));

		// Sync statistic parameters
		_reportSource.ClearStatisticParameters();
		foreach (var p in StatisticManager.Parameters)
			_reportSource.AddStatisticParameter(p.Name, ToReportValue(p.Value));
	}

	/// <inheritdoc />
	[Browsable(false)]
	string IReportSource.Name => Name;

	/// <inheritdoc />
	[Browsable(false)]
	TimeSpan IReportSource.TotalWorkingTime => TotalWorkingTime;

	/// <inheritdoc />
	[Browsable(false)]
	decimal? IReportSource.Commission => Commission;

	/// <inheritdoc />
	[Browsable(false)]
	decimal IReportSource.Position => Position;

	/// <inheritdoc />
	[Browsable(false)]
	decimal IReportSource.PnL => PnL;

	/// <inheritdoc />
	[Browsable(false)]
	decimal? IReportSource.Slippage => Slippage;

	/// <inheritdoc />
	[Browsable(false)]
	TimeSpan? IReportSource.Latency => Latency;

	/// <inheritdoc />
	[Browsable(false)]
	IEnumerable<(string Name, object Value)> IReportSource.StatisticParameters
		=> StatisticManager.Parameters.Select(p => (p.Name, ToReportValue(p.Value)));

	/// <inheritdoc />
	[Browsable(false)]
	IEnumerable<(string Name, object Value)> IReportSource.Parameters
		=> GetParameters().Where(p => p.IsBrowsable()).Select(p => (p.GetName(), ToReportValue(p.Value)));

	/// <inheritdoc />
	[Browsable(false)]
	IEnumerable<ReportOrder> IReportSource.Orders => _reportSource.Orders;

	/// <inheritdoc />
	[Browsable(false)]
	IEnumerable<ReportTrade> IReportSource.OwnTrades => _reportSource.OwnTrades;

	/// <inheritdoc />
	[Browsable(false)]
	IEnumerable<ReportPosition> IReportSource.Positions => _reportSource.Positions;

	/// <summary>
	/// To convert parameter value to report value.
	/// </summary>
	/// <param name="value">The parameter value.</param>
	/// <returns>The report value.</returns>
	protected virtual object ToReportValue(object value)
	{
		if (value is Security s)
			return s.Id;
		else if (value is Position p)
			return p.ToString();

		return value.To<string>();
	}

	private void AddOrderToReport(Order order)
	{
		if (order?.Security is null)
			return;

		_reportSource.AddOrder(new ReportOrder(
			order.Id,
			order.TransactionId,
			order.Security.ToSecurityId(),
			order.Side,
			order.ServerTime,
			order.Price,
			order.State,
			order.Balance,
			order.Volume,
			order.Type
		));
	}

	private void AddTradeToReport(MyTrade trade)
	{
		if (trade?.Trade is null || trade.Order is null)
			return;

		_reportSource.AddTrade(new ReportTrade(
			trade.Trade.Id,
			trade.Order.TransactionId,
			trade.Trade.SecurityId,
			trade.Trade.ServerTime,
			trade.Trade.Price,
			trade.Order.Price,
			trade.Trade.Volume,
			trade.Order.Side,
			trade.Order.Id,
			trade.Slippage,
			trade.PnL,
			trade.Position
		));
	}

	#endregion

	#region ICustomTypeDescriptor

	private sealed class StrategyParamPropDescriptor(IStrategyParam param)
		: NamedPropertyDescriptor(param.Id, [.. param.Attributes])
	{
		public override Type ComponentType => typeof(Strategy);
		public override bool IsReadOnly => false;
		public override Type PropertyType => param.Type;

		public override object GetValue(object component) => param.Value;
		public override void SetValue(object component, object value) => param.Value = value;

		public override bool CanResetValue(object component) => false;
		public override void ResetValue(object component) => throw new NotSupportedException();
		public override bool ShouldSerializeValue(object component) => false;
	}

	/// <summary>
	/// Get parameters.
	/// </summary>
	/// <returns>Parameters.</returns>
	public virtual IStrategyParam[] GetParameters() => Parameters.CachedValues;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(this, true);

	string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(this, true);
	string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(this, true);
	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
	object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(this, true);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => ((ICustomTypeDescriptor)this).GetProperties().TryGetDefault(GetType());
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => new([.. GetParameters().Select(p => new StrategyParamPropDescriptor(p))]);
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => this.GetFilteredProperties(attributes);

	#endregion
}