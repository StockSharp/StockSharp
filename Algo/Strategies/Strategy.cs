#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: Strategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Runtime.CompilerServices;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Statistics;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Testing;

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
	    ITransactionProvider, IScheduledTask
	{
		private class StrategyChangeStateMessage : Message
		{
			public Strategy Strategy { get; }
			public ProcessStates State { get; }

			public StrategyChangeStateMessage(Strategy strategy, ProcessStates state)
				: base(ExtendedMessageTypes.StrategyChangeState)
			{
				Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
				State = state;
			}

			public override Message Clone()
			{
				return new StrategyChangeStateMessage(Strategy, State);
			}
		}

		private sealed class ChildStrategyList : SynchronizedSet<Strategy>, IStrategyChildStrategyList
		{
			private readonly Dictionary<Strategy, IMarketRule> _childStrategyRules = new();
			private readonly Strategy _parent;

			public ChildStrategyList(Strategy parent)
				: base(true)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			protected override void OnAdded(Strategy item)
			{
				//pyh: Нельзя использовать OnAdding тк логирование включается по событию Added которое вызовет base.OnAdded
				base.OnAdded(item);

				if (item.Parent != null)
					throw new ArgumentException(LocalizedStrings.ParentAlreadySet.Put(item, item.Parent));

				item.Parent = _parent;
				item.Connector = _parent.Connector;

				item.Portfolio ??= _parent.Portfolio;
				item.Security ??= _parent.Security;

				item.OrderRegistering += _parent.ProcessChildOrderRegistering;
				item.OrderRegistered += _parent.ProcessOrder;
				item.OrderChanged += _parent.OnChildOrderChanged;
				item.OrderRegisterFailed += _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed += _parent.OnChildOrderCancelFailed;
				item.NewMyTrade += _parent.AddMyTrade;
				item.OwnTradeReceived += _parent.OnChildOwnTradeReceived;
				item.OrderReRegistering += _parent.OnOrderReRegistering;
				item.ProcessStateChanged += OnChildProcessStateChanged;
				item.Error += _parent.OnError;
				item.IsOnlineChanged += _parent.OnChildStrategyIsOnlineChanged;

				item.Orders.ForEach(_parent.ProcessOrder);

				if (!item.MyTrades.IsEmpty())
					item.MyTrades.ForEach(_parent.AddMyTrade);

				//_parent._orderFails.AddRange(item.OrderFails);

				if (item.ProcessState == _parent.ProcessState && _parent.ProcessState == ProcessStates.Started)
					OnChildProcessStateChanged(item);
				else
					item.ProcessState = _parent.ProcessState;

				_parent.CheckRefreshOnlineState();
			}

			private void OnChildProcessStateChanged(Strategy child)
			{
				if (child.ProcessState == ProcessStates.Started)
				{
					// для предотвращения остановки родительской стратегии пока работают ее дочерние
					var rule =
						child
							.WhenStopped()
							.Do(() => _childStrategyRules.Remove(child))
							.Once()
							.Apply(_parent);

					rule.UpdateName(rule.Name + $" ({nameof(ChildStrategyList)}.{nameof(OnChildProcessStateChanged)})");

					_childStrategyRules.Add(child, rule);
				}

				_parent.CheckRefreshOnlineState();
			}

			protected override bool OnClearing()
			{
				foreach (var item in this.ToArray())
					Remove(item);

				return true;
			}

			protected override bool OnRemoving(Strategy item)
			{
				//item.Parent = null;

				item.OrderRegistering -= _parent.ProcessChildOrderRegistering;
				item.OrderRegistered -= _parent.ProcessOrder;
				item.OrderChanged -= _parent.OnChildOrderChanged;
				item.OrderRegisterFailed -= _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed -= _parent.OnChildOrderCancelFailed;
				item.OrderCanceling -= _parent.OnOrderCanceling;
				item.NewMyTrade -= _parent.AddMyTrade;
				item.OwnTradeReceived -= _parent.OnChildOwnTradeReceived;
				item.OrderReRegistering -= _parent.OnOrderReRegistering;
				item.ProcessStateChanged -= OnChildProcessStateChanged;
				item.Error -= _parent.OnError;
				item.IsOnlineChanged -= _parent.OnChildStrategyIsOnlineChanged;

				var rule = _childStrategyRules.TryGetValue(item);

				if (rule != null)
				{
					// правило могло быть удалено при остановке дочерней стратегии, но перед ее удалением из коллекции у родителя
					if (rule.IsReady)
						_parent.TryRemoveRule(rule);

					_childStrategyRules.Remove(item);
				}

				return base.OnRemoving(item);
			}

			protected override void OnRemoved(Strategy item)
			{
				base.OnRemoved(item);
				_parent.CheckRefreshOnlineState();
			}

			public void TryRemoveStoppedRule(IMarketRule rule)
			{
				if (rule.Token is Strategy child)
					_childStrategyRules.Remove(child);
			}
		}

		private sealed class StrategyRuleList : MarketRuleList
		{
			private readonly Strategy _strategy;

			public StrategyRuleList(Strategy strategy)
				: base(strategy)
			{
				_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			}

			protected override bool OnAdding(IMarketRule item)
			{
				return _strategy.ProcessState != ProcessStates.Stopping && base.OnAdding(item);
			}
		}

		private sealed class OrderInfo
		{
			public OrderInfo()
			{
				PrevState = OrderStates.None;
			}

			public bool IsOwn { get; set; }
			public bool IsCanceled { get; set; }
			public decimal ReceivedVolume { get; set; }
			public OrderStates PrevState { get; set; }
		}

		private class IndicatorList : SynchronizedSet<IIndicator>
		{
			private readonly Strategy _strategy;
			private readonly CachedSynchronizedSet<IIndicator> _nonFormedIndicators = new();

			public IndicatorList(Strategy strategy)
			{
				_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			}

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

		private readonly CachedSynchronizedDictionary<Order, OrderInfo> _ordersInfo = new();

		private readonly CachedSynchronizedDictionary<Subscription, bool> _subscriptions = new();
		private readonly SynchronizedDictionary<long, Subscription> _subscriptionsById = new();
		private readonly CachedSynchronizedSet<Subscription> _suspendSubscriptions = new();
		private Subscription _pfSubscription;
		private Subscription _orderSubscription;

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
			_childStrategies = new ChildStrategyList(this);

			Rules = new StrategyRuleList(this);

			NameGenerator = new StrategyNameGenerator(this);
			NameGenerator.Changed += name => _name.Value = name;

			Parameters = new(this);

			_id = this.Param(nameof(Id), base.Id);
			_volume = this.Param<decimal>(nameof(Volume), 1).SetValidator(v => v > 0);
			_name = this.Param(nameof(Name), new string(GetType().Name.Where(char.IsUpper).ToArray()));
			_disposeOnStop = this.Param(nameof(DisposeOnStop), false).CanOptimize(false);
			_waitRulesOnStop = this.Param(nameof(WaitRulesOnStop), true).CanOptimize(false);
			_cancelOrdersWhenStopping = this.Param(nameof(CancelOrdersWhenStopping), true).CanOptimize(false);
			_waitAllTrades = this.Param<bool>(nameof(WaitAllTrades)).CanOptimize(false);
			_commentMode = this.Param<StrategyCommentModes>(nameof(CommentMode));
			_ordersKeepTime = this.Param(nameof(OrdersKeepTime), TimeSpan.FromDays(1)).SetValidator(v => v >= TimeSpan.Zero);
			_logLevel = this.Param(nameof(LogLevel), LogLevels.Inherit);
			_stopOnChildStrategyErrors = this.Param(nameof(StopOnChildStrategyErrors), false).CanOptimize(false);
			_restoreChildOrders = this.Param(nameof(RestoreChildOrders), false).CanOptimize(false);
			_tradingMode = this.Param(nameof(TradingMode), StrategyTradingModes.Full);
			_unsubscribeOnStop = this.Param(nameof(UnsubscribeOnStop), true).CanOptimize(false);
			_workingTime = this.Param(nameof(WorkingTime), new WorkingTime()).NotNull();
			_isOnlineStateIncludesChildren = this.Param(nameof(IsOnlineStateIncludesChildren), true).CanOptimize(false);
			_historySize = this.Param<TimeSpan?>(nameof(HistorySize)).SetValidator(v => v is null || v >= TimeSpan.Zero);

			_systemParams = new IStrategyParam[]
			{
				_id,
				_disposeOnStop,
				_waitRulesOnStop,
				_cancelOrdersWhenStopping,
				_waitAllTrades,
				_ordersKeepTime,
				_ordersKeepTime,
				_stopOnChildStrategyErrors,
				_restoreChildOrders,
				_unsubscribeOnStop,
				_workingTime,
				_isOnlineStateIncludesChildren,
				_historySize,
			};

			_ordersKeepTime.CanOptimize = _historySize.CanOptimize = false;

			_riskManager = new RiskManager { Parent = this };

			_positionManager = new ChildStrategyPositionManager { Parent = this };

			_indicators = new(this);
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

				_pfSubscription = null;
				_orderSubscription = null;

				if (_connector != null)
				{
					ISubscriptionProvider isp = _connector;
					IConnector con = _connector;

					isp.OrderReceived             -= OnConnectorOrderReceived;
					isp.OwnTradeReceived          -= OnConnectorOwnTradeReceived;
					con.OrderRegisterFailed       -= OnConnectorOrderRegisterFailed;
					con.OrderCancelFailed         -= ProcessCancelOrderFail;
					con.OrderEdited               -= OnConnectorOrderEdited;
					con.OrderEditFailed           -= OnConnectorOrderEditFailed;
					con.NewMessage                -= OnConnectorNewMessage;
					isp.PositionReceived          -= OnConnectorPositionReceived;
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
					con.OrderRegisterFailed       += OnConnectorOrderRegisterFailed;
					con.OrderCancelFailed         += ProcessCancelOrderFail;
					con.OrderEdited               += OnConnectorOrderEdited;
					con.OrderEditFailed           += OnConnectorOrderEditFailed;
					con.NewMessage                += OnConnectorNewMessage;
					isp.PositionReceived          += OnConnectorPositionReceived;
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
					isp.SubscriptionReceived      += OnConnectorSubscriptionReceived;
					isp.SubscriptionOnline        += OnConnectorSubscriptionOnline;
					isp.SubscriptionStarted       += OnConnectorSubscriptionStarted;
					isp.SubscriptionStopped       += OnConnectorSubscriptionStopped;
					isp.SubscriptionFailed        += OnConnectorSubscriptionFailed;
				}

				foreach (var strategy in ChildStrategies)
					strategy.Connector = value;

				ConnectorChanged?.Invoke();
			}
		}

		private void OnConnectorPortfolioReceived(Subscription subscription, Portfolio portfolio)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				PortfolioReceived?.Invoke(subscription, portfolio);
		}

		private void OnConnectorOrderEditFailReceived(Subscription subscription, OrderFail fail)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				OrderEditFailReceived?.Invoke(subscription, fail);
		}

		private void OnConnectorOrderCancelFailReceived(Subscription subscription, OrderFail fail)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				OrderCancelFailReceived?.Invoke(subscription, fail);
		}

		private void OnConnectorOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				OrderRegisterFailReceived?.Invoke(subscription, fail);
		}

		/// <summary>
		/// To get the strategy getting <see cref="Connector"/>. If it is not initialized, the exception will be discarded.
		/// </summary>
		/// <returns>Connector.</returns>
		public IConnector SafeGetConnector()
			=> Connector ?? throw new InvalidOperationException(LocalizedStrings.ConnectionNotInit);

		private Portfolio _portfolio;

		/// <summary>
		/// Portfolio.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PortfolioKey,
			Description = LocalizedStrings.StrategyPortfolioKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
		public virtual Portfolio Portfolio
		{
			get => _portfolio;
			set
			{
				if (_portfolio == value)
					return;

				_portfolio = value;

				foreach (var strategy in ChildStrategies)
				{
					strategy.Portfolio ??= value;
				}

				RaiseParametersChanged();

				this.Notify();
			}
		}

		private Security _security;

		/// <summary>
		/// Security.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecurityKey,
			Description = LocalizedStrings.StrategySecurityKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 2)]
		public virtual Security Security
		{
			get => _security;
			set
			{
				if (_security == value)
					return;

				_security = value;

				foreach (var strategy in ChildStrategies)
				{
					strategy.Security ??= value;
				}

				RaiseParametersChanged();

				this.Notify(nameof(Position));
			}
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
		public decimal PnL => PnLManager.PnL;

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
		}

		/// <summary>
		/// Strategy environment parameters.
		/// </summary>
		[Browsable(false)]
		public SettingsStorage Environment { get; } = new();

		/// <summary>
		/// The maximal number of errors, which strategy shall receive prior to stop operation.
		/// </summary>
		/// <remarks>
		/// The default value is 1.
		/// </remarks>
		[Browsable(false)]
		[Obsolete("Use RiskErrorRule rule.")]
		public int MaxErrorCount { get; set; }

		/// <summary>
		/// The current number of errors.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Use RiskErrorRule rule.")]
		public int ErrorCount { get; private set; }

		/// <summary>
		/// The maximum number of order registration errors above which the algorithm will be stopped.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		[Browsable(false)]
		[Obsolete("Use RiskOrderErrorRule rule.")]
		public int MaxOrderRegisterErrorCount { get; set; }

		/// <summary>
		/// Current number of order registration errors.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Use RiskOrderErrorRule rule.")]
		public int OrderRegisterErrorCount { get; private set; }

		/// <summary>
		/// Current number of order changes.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Use RiskOrderFreqRule rule.")]
		public int CurrentRegisterCount { get; private set; }

		/// <summary>
		/// The maximum number of orders above which the algorithm will be stopped.
		/// </summary>
		/// <remarks>
		/// The default value is <see cref="int.MaxValue"/>.
		/// </remarks>
		[Browsable(false)]
		[Obsolete("Use RiskOrderFreqRule rule.")]
		public int MaxRegisterCount { get; set; }

		/// <summary>
		/// The order registration interval above which the new order would not be registered.
		/// </summary>
		/// <remarks>
		/// By default, the interval is disabled and it is equal to <see cref="TimeSpan.Zero"/>.
		/// </remarks>
		[Browsable(false)]
		[Obsolete("Use RiskOrderFreqRule rule.")]
		public TimeSpan RegisterInterval { get; set; }

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

				this.AddDebugLog("State: {0}->{1}", _processState, value);

				if (_processState == ProcessStates.Stopped && value == ProcessStates.Stopping)
					throw new InvalidOperationException(LocalizedStrings.StrategyAlreadyStopped.Put(Name, value));

				_processState = value;

				try
				{
					var child = (IEnumerable<Strategy>)ChildStrategies;

					if (ProcessState == ProcessStates.Stopping)
						child = child.Where(s => s.ProcessState == ProcessStates.Started);

					child.ToArray().ForEach(s => s.ProcessState = ProcessState);

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
						this.AddInfoLog(LocalizedStrings.WaitingCancellingAllOrders);
						ProcessCancelActiveOrders();
					}

					foreach (var rule in Rules.ToArray())
					{
						if (this.TryRemoveWithExclusive(rule))
							_childStrategies.TryRemoveStoppedRule(rule);
					}

					TryFinalStop();
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

			var ps = ParentStrategy;
			this.AddInfoLog("Strategy {0}. [{1},{2}]. Position {3}.", stateStr, ChildStrategies.Count, ps != null ? ps.ChildStrategies.Count : -1, Position);
		}

		private Strategy ParentStrategy => Parent as Strategy;

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
		/// The time for storing <see cref="Orders"/> and <see cref="StopOrders"/> orders in memory. By default it equals to 2 days. If value is set in <see cref="TimeSpan.Zero"/>, orders will not be deleted.
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

		private readonly StrategyParam<bool> _restoreChildOrders;

		/// <summary>
		/// Restore orders last time was registered by child strategies.
		/// </summary>
		[Browsable(false)]
		public bool RestoreChildOrders
		{
			get => _restoreChildOrders.Value;
			set => _restoreChildOrders.Value = value;
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
		/// True means that strategy is started and all of its subscriptions are in online state and all child strategies are online.
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

		private readonly StrategyParam<bool> _isOnlineStateIncludesChildren;

		/// <summary>
		/// If <see langword="true"/>, the strategy can only be <see cref="IsOnline"/> if all of its <see cref="ChildStrategies"/> are online as well.
		/// </summary>
		[Browsable(false)]
		public bool IsOnlineStateIncludesChildren
		{
			get => _isOnlineStateIncludesChildren.Value;
			set => _isOnlineStateIncludesChildren.Value = value;
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

		private readonly CachedSynchronizedSet<MyTrade> _myTrades = new();

		/// <summary>
		/// Trades, matched during the strategy operation.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

		/// <summary>
		/// Orders with errors, registered within the strategy.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Subscribe on OrderRegisterFailed event.")]
		public IEnumerable<OrderFail> OrderFails => Enumerable.Empty<OrderFail>();

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

		private readonly ChildStrategyList _childStrategies;

		/// <summary>
		/// Subsidiary trade strategies.
		/// </summary>
		[Browsable(false)]
		public IStrategyChildStrategyList ChildStrategies => _childStrategies;

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
		/// Automatically to clear resources, used by the strategy, when it stops (state <see cref="ProcessState"/> becomes equal to <see cref="ProcessStates.Stopped"/>) and delete it from the parent strategy through <see cref="ChildStrategies"/>.
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

		/// <inheritdoc />
		[Browsable(false)]
		public IMarketRuleList Rules { get; }

		//private readonly object _rulesSuspendLock = new object();
		private int _rulesSuspendCount;

		/// <inheritdoc />
		[Browsable(false)]
		public bool IsRulesSuspended => _rulesSuspendCount > 0;

		private readonly StrategyParam<bool> _stopOnChildStrategyErrors;

		/// <summary>
		/// Stop strategy when child strategies causes errors.
		/// </summary>
		/// <remarks>
		/// It is disabled by default.
		/// </remarks>
		[Browsable(false)]
		public bool StopOnChildStrategyErrors
		{
			get => _stopOnChildStrategyErrors.Value;
			set => _stopOnChildStrategyErrors.Value = value;
		}

		/// <summary>
		/// The event of sending order for registration.
		/// </summary>
		public event Action<Order> OrderRegistering;

		/// <summary>
		/// The event of order successful registration.
		/// </summary>
		public event Action<Order> OrderRegistered;

		/// <inheritdoc />
		public event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// The event of sending order for re-registration.
		/// </summary>
		public event Action<Order, Order> OrderReRegistering;

		/// <inheritdoc />
		public event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// The event of sending order for cancelling.
		/// </summary>
		public event Action<Order> OrderCanceling;

		/// <inheritdoc />
		public event Action<Order> OrderChanged;

		/// <inheritdoc />
		public event Action<long, Order> OrderEdited;

		/// <inheritdoc />
		public event Action<long, OrderFail> OrderEditFailed;

		/// <inheritdoc />
		public event Action<MyTrade> NewMyTrade;

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

			if (IsRootStrategy)
			{
				_pfSubscription = new Subscription(new PortfolioLookupMessage
				{
					IsSubscribe = true,
					StrategyId = EnsureGetId(),
				}, (SecurityMessage)null);

				Subscribe(_pfSubscription, true);
			}

			_orderSubscription = new Subscription(new OrderStatusMessage
			{
				IsSubscribe = true,
				StrategyId = EnsureGetId(),
			}, (SecurityMessage)null);

			Subscribe(_orderSubscription, true);
		}

		/// <summary>
		/// Init.
		/// </summary>
		protected void InitStartValues()
		{
			foreach (var parameter in Parameters.CachedValues)
			{
				if (parameter.Value is Unit unit && unit.GetTypeValue == null && (unit.Type == UnitTypes.Point || unit.Type == UnitTypes.Step))
					unit.SetSecurity(this.GetSecurity());
			}

			ErrorState = LogLevels.Info;

			if (Portfolio?.CurrentValue is not null)
				StatisticManager.Init<IPnLStatisticParameter, decimal>(Portfolio.CurrentValue.Value);

			_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		}

		/// <summary>
		/// The method is called when the <see cref="ProcessState"/> process state has been taken the <see cref="ProcessStates.Stopping"/> value.
		/// </summary>
		protected virtual void OnStopping()
		{
			if (UnsubscribeOnStop)
				UnSubscribe(false);
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

			var pos = _positions.TryGetValue((order.Security, order.Portfolio))?.CurrentValue;

			if (!CanTrade(pos > 0 && pos.Value.GetDirection() == order.Side.Invert() && pos.Value.Abs() >= order.Volume, out var reason))
			{
				ProcessOrderFail(order, new InvalidOperationException(reason));
				return;
			}

			this.AddInfoLog("Registration {0} (0x{5:X}) order for {1} with price {2} and volume {3}. {4}",
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
			this.AddInfoLog("EditOrder: {0}", order);

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

			this.AddInfoLog("Reregistration {0} with price {1} to price {2}. {3}", oldOrder.TransactionId, oldOrder.Price, newOrder.Price, oldOrder.Comment);

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

		private void ProcessChildOrderRegistering(Order order)
		{
			OnOrderRegistering(order);

			_newOrder?.Invoke(order);

			ProcessRisk(order);
		}

		private RiskActions? AddOrder(Order order, bool restored)
		{
			var action = ProcessRisk(order);

			if (action is not null)
				return action;

			_ordersInfo.Add(order, new() { IsOwn = true });

			if (!restored)
				order.UserOrderId = EnsureGetId();

			order.StrategyId = EnsureGetRootId();

			if (!order.State.IsFinal())
				ApplyMonitorRules(order);

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
				this.AddErrorLog(LocalizedStrings.ErrorRegOrder, nOrder.TransactionId, excp.Message);

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
			};

			OnOrderRegisterFailed(fail, canRisk && _ordersInfo.TryGetValue(order, out var info) && info.IsOwn);

			Rules.RemoveRulesByToken(order, null);
		}

		private void ApplyMonitorRules(Order order)
		{
			if (!CancelOrdersWhenStopping)
				return;

			IMarketRule matchedRule = order.WhenMatched(this);

			if (WaitAllTrades)
				matchedRule = matchedRule.And(order.WhenAllTrades(this));

			var successRule = order
				.WhenCanceled(this)
				.Or(matchedRule, order.WhenRegisterFailed(this))
				.Do(() => this.AddInfoLog(LocalizedStrings.OrderNoLongerActive.Put(order.TransactionId)))
				.Until(() =>
				{
					if (order.State == OrderStates.Failed)
						return true;

					if (order.State != OrderStates.Done)
					{
						this.AddWarningLog(LocalizedStrings.OrderHasState, order.TransactionId, order.State);
						return false;
					}

					if (!WaitAllTrades)
						return true;

					if (!_ordersInfo.TryGetValue(order, out var info))
					{
						this.AddWarningLog(LocalizedStrings.OrderNotFound, order.TransactionId);
						return false;
					}

					var leftVolume = order.GetMatchedVolume() - info.ReceivedVolume;

					if (leftVolume != 0)
					{
						this.AddDebugLog(LocalizedStrings.OrderHasBalance, order.TransactionId, leftVolume);
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
					this.AddInfoLog(LocalizedStrings.ErrorCancellingOrder.Put(order.TransactionId, f.Error.Message));
				})
				.Until(() => canFinish)
				.Apply(this)
				.Exclusive(successRule);
		}

		/// <inheritdoc />
		public void CancelOrder(Order order)
		{
			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.StrategyInStateCannotCancelOrder, ProcessState);
				return;
			}

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (TradingMode == StrategyTradingModes.Disabled)
			{
				this.AddWarningLog(LocalizedStrings.TradingDisabled);
				return;
			}

			lock (_ordersInfo.SyncRoot)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null || !info.IsOwn)
					throw new ArgumentException(LocalizedStrings.OrderNotFromStrategy.Put(order.TransactionId, Name));

				if (info.IsCanceled)
				{
					this.AddWarningLog(LocalizedStrings.OrderAlreadySentCancel, order.TransactionId);
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

			this.AddInfoLog(LocalizedStrings.OrderCancelling + " " + order.TransactionId);

			OnOrderCanceling(order);

			SafeGetConnector().CancelOrder(order);
		}

		/// <summary>
		/// To add the order to the strategy.
		/// </summary>
		/// <param name="order">Order.</param>
		private void ProcessOrder(Order order)
		{
			ProcessOrder(order, false);
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

			var isRegistered = (info != null && !info.IsOwn && !isChanging) || //иначе не добавляются заявки дочерних стратегий
			                   info != null && info.IsOwn && info.PrevState == OrderStates.Pending && (order.State == OrderStates.Active || order.State == OrderStates.Done);

			if (info != null && info.IsOwn)
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

					PositionChangeMessage posChange = null;

					if(!IsRootStrategy)
						posChange = _positionManager.ProcessMessage(order.ToMessage());

					StatisticManager.AddNewOrder(order);

					if (order.Commission != null)
					{
						Commission += order.Commission;
						RaiseCommissionChanged();
					}

					ProcessPositionChangeMessageImpl(posChange);
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
						//var info = _ordersInfo.TryGetValue(order);

						// заявка принадлежит дочерней стратегии
						if (info == null || !info.IsOwn)
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
				PositionChangeMessage posChange = null;
				if(!IsRootStrategy)
					posChange = _positionManager.ProcessMessage(order.ToMessage());

				StatisticManager.AddChangedOrder(order);

				OnOrderChanged(order);

				ProcessPositionChangeMessageImpl(posChange);
			}
		}

		private void AttachOrder(Order order, bool restored)
		{
			this.AddInfoLog("Order {0} attached.", order.TransactionId);

			AddOrder(order, restored);

			if(order.Type != OrderTypes.Conditional && !IsRootStrategy)
				ProcessPositionChangeMessageImpl(_positionManager.ProcessMessage(order.ToMessage()));

			ProcessOrder(order);

			OnOrderRegistering(order);
		}

		private void RecycleOrders()
		{
			if (OrdersKeepTime == TimeSpan.Zero)
				return;

			this.AddInfoLog(nameof(RecycleOrders));

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
			this.AddErrorLog(error);

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
			this.AddInfoLog(LocalizedStrings.Reset);

			ChildStrategies.ForEach(s => s.Reset());

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

				_positions.Clear();
				_positionManager.Reset();
			}

			ProcessState = ProcessStates.Stopped;
			ErrorState = LogLevels.Info;
			LastError = default;
			TotalWorkingTime = default;
			StartedTime = default;

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
				RaisePositionChanged(time);
				RaiseSlippageChanged();
			}
		}

		/// <summary>
		/// It is called from the <see cref="Reset"/> method.
		/// </summary>
		protected virtual void OnReseted()
		{
			RaiseReseted();
		}

		void IMarketRuleContainer.SuspendRules()
		{
			_rulesSuspendCount++;

			this.AddDebugLog(LocalizedStrings.RulesSuspended, _rulesSuspendCount);
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

			this.AddDebugLog(LocalizedStrings.RulesResume, _rulesSuspendCount);
		}

		private void TryFinalStop()
		{
			IMarketRule[] rules;

			if(Rules is ISynchronizedCollection coll)
			{
				lock (coll.SyncRoot)
					rules = Rules.ToArray();
			}
			else
			{
				rules = Rules.ToArray();
			}

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
				ParentStrategy?.ChildStrategies.Remove(this);

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
		/// The method, called at occurrence of new strategy trade.
		/// </summary>
		/// <param name="trade">New trade of a strategy.</param>
		protected virtual void OnNewMyTrade(MyTrade trade)
		{
			NewMyTrade?.Invoke(trade);
		}

		/// <summary>
		/// To call the event <see cref="OrderRegistering"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderRegistering(Order order)
		{
			TryAddChildOrder(order);

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
		/// To call the event <see cref="OrderCanceling"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderCanceling(Order order)
		{
			OrderCanceling?.Invoke(order);
		}

		/// <summary>
		/// To call the event <see cref="OrderReRegistering"/>.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		protected virtual void OnOrderReRegistering(Order oldOrder, Order newOrder)
		{
			TryAddChildOrder(newOrder);

			OrderReRegistering?.Invoke(oldOrder, newOrder);
		}

		/// <summary>
		/// The method, called at strategy order change.
		/// </summary>
		/// <param name="order">The changed order.</param>
		protected virtual void OnOrderChanged(Order order)
		{
			OrderChanged?.Invoke(order);
			ChangeLatency(order.LatencyCancellation);
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

		private void TryAddChildOrder(Order order)
		{
			_ordersInfo.SafeAdd(order, key => new() { IsOwn = false });
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

				case MessageTypes.PositionChange:
					ProcessPositionChangeMessage((PositionChangeMessage)message);
					break;

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
									this.AddDebugLog(LocalizedStrings.StrategyStopping, ProcessState);

								break;
							}
							case ProcessStates.Started:
							{
								if (ProcessState == ProcessStates.Stopped)
									ProcessState = ProcessStates.Started;
								else
									this.AddDebugLog(LocalizedStrings.StrategyStarting, ProcessState);

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
			if (IsDisposeStarted || !IsOwnOrder(trade.Order) || !TryAddMyTrade(trade))
				return;

			TryInvoke(() => OwnTradeReceived?.Invoke(subscription, trade));
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

			var id = EnsureGetId();

			return order.UserOrderId.EqualsIgnoreCase(id) || (RestoreChildOrders && order.StrategyId.EqualsIgnoreCase(id));
		}

		private void OnConnectorOrderReceived(Subscription subscription, Order order)
		{
			if (_orderSubscription != subscription || IsDisposeStarted)
				return;

			if (!_ordersInfo.ContainsKey(order) && CanAttach(order))
				AttachOrder(order, true);
			else if (IsOwnOrder(order))
				TryInvoke(() => ProcessOrder(order, true));

			TryInvoke(() => OrderReceived?.Invoke(subscription, order));
		}

		private void OnConnectorOrderEditFailed(long transactionId, OrderFail fail)
		{
			if(IsDisposeStarted)
				return;

			if (IsOwnOrder(fail.Order))
				OrderEditFailed?.Invoke(transactionId, fail);
		}

		private void OnConnectorOrderEdited(long transactionId, Order order)
		{
			if(IsDisposeStarted)
				return;

			if (IsOwnOrder(order))
			{
				OrderEdited?.Invoke(transactionId, order);
				ChangeLatency(order.LatencyEdition);
			}
		}

		private string _idStr;
		private string _rootIdStr;
		private Strategy _rootStrategy;

		private string EnsureGetId()     => _idStr     ??= Id.To<string>();
		private string EnsureGetRootId() => _rootIdStr ??= RootStrategy.EnsureGetId();

		/// <summary>
		/// Root strategy.
		/// </summary>
		protected Strategy RootStrategy => _rootStrategy ??= IsRootStrategy ? this : ParentStrategy.RootStrategy;

		/// <summary>
		/// Whether this is a root strategy.
		/// </summary>
		protected bool IsRootStrategy => ParentStrategy == null;

		private void OnConnectorOrderRegisterFailed(OrderFail fail)
		{
			if(IsDisposeStarted)
				return;

			if (_ordersInfo.TryGetValue(fail.Order, out var info))
				OnOrderRegisterFailed(fail, info.IsOwn);
		}

		private void UpdatePnLManager(Security security)
		{
			var msg = new Level1ChangeMessage { SecurityId = security.ToSecurityId(), ServerTime = CurrentTime }
					.TryAdd(Level1Fields.PriceStep, security.PriceStep)
					.TryAdd(Level1Fields.StepPrice, this.GetSecurityValue<decimal?>(security, Level1Fields.StepPrice) ?? security.StepPrice)
					.TryAdd(Level1Fields.Multiplier, this.GetSecurityValue<decimal?>(security, Level1Fields.Multiplier) ?? security.Multiplier);

			PnLManager.ProcessMessage(msg);
		}

		private void OnChildOwnTradeReceived(Subscription subscription, MyTrade trade)
		{
			TryInvoke(() => OwnTradeReceived?.Invoke(subscription, trade));
		}

		private void AddMyTrade(MyTrade trade) => TryAddMyTrade(trade);

		private bool TryAddMyTrade(MyTrade trade)
		{
			if (!_myTrades.TryAdd(trade))
				return false;

			if (WaitAllTrades)
			{
				lock (_ordersInfo.SyncRoot)
				{
					if (_ordersInfo.TryGetValue(trade.Order, out var info) && info.IsOwn)
						info.ReceivedVolume += trade.Trade.Volume;
				}
			}

			var isComChanged = false;
			var isSlipChanged = false;

			this.AddInfoLog("{0} trade {1} at price {2} for {3} orders {4}.",
				trade.Order.Side,
				(trade.Trade.Id is null ? trade.Trade.StringId : trade.Trade.Id.To<string>()),
				trade.Trade.Price, trade.Trade.Volume, trade.Order.TransactionId);

			if (trade.Commission != null)
			{
				Commission ??= 0;

				Commission += trade.Commission.Value;
				isComChanged = true;
			}

			UpdatePnLManager(trade.Trade.Security);

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

			if (trade.Slippage != null)
			{
				Slippage ??= 0;

				Slippage += trade.Slippage.Value;
				isSlipChanged = true;
			}

			trade.Position ??= GetPositionValue(trade.Order.Security, trade.Order.Portfolio);

			TryInvoke(() => OnNewMyTrade(trade));

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

			if (_pfSubscription != null)
			{
				PnLReceived?.Invoke(_pfSubscription);

				var evt = PnLReceived2;
				var pf = Portfolio;

				if (evt is not null && pf is not null)
				{
					var manager = PnLManager;
					evt(_pfSubscription, pf, time, manager.RealizedPnL, manager.UnrealizedPnL, Commission);

					ProcessRisk(() => new PositionChangeMessage
					{
						PortfolioName = pf.Name,
						SecurityId = SecurityId.Money,
						ServerTime = time,
						OriginalTransactionId = _pfSubscription.TransactionId,
					}
					.TryAdd(PositionChangeTypes.RealizedPnL, manager.RealizedPnL)
					.TryAdd(PositionChangeTypes.UnrealizedPnL, manager.UnrealizedPnL)
					.TryAdd(PositionChangeTypes.Commission, Commission)
					);
				}
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

		/// <summary>
		/// To process orders, received for the connection <see cref="Connector"/>, and find among them those, belonging to the strategy.
		/// </summary>
		/// <param name="newOrders">New orders.</param>
		/// <returns>Orders, belonging to the strategy.</returns>
		protected virtual IEnumerable<Order> ProcessNewOrders(IEnumerable<Order> newOrders)
		{
			return _ordersInfo.SyncGet(d => newOrders.Where(IsOwnOrder).ToArray());
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
					if (Parameters.TryGetValue(s.GetValue<string>(nameof(IStrategyParam.Id)) ?? s.GetValue<string>(nameof(IStrategyParam.Name)), out var param))
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
			Save(storage, KeepStatistics, true, false, false);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage"><see cref="SettingsStorage"/></param>
		/// <param name="saveStatistics"><see cref="KeepStatistics"/></param>
		/// <param name="saveSystemParameters">Save system parameters.</param>
		/// <param name="saveSecurity">Save <see cref="Security"/>.</param>
		/// <param name="savePortfolio">Save <see cref="Portfolio"/>.</param>
		public void Save(SettingsStorage storage, bool saveStatistics, bool saveSystemParameters, bool saveSecurity, bool savePortfolio)
		{
			var parameters = Parameters.CachedValues;

			if (!saveSystemParameters)
				parameters = parameters.Except(_systemParams).ToArray();

			storage
				.Set(nameof(Parameters), parameters.Select(p =>
				{
					var paramSettings = new SettingsStorage();
					p.Save(paramSettings, !saveSystemParameters);
					return paramSettings;
				}).ToArray())
				.Set(nameof(RiskManager), RiskManager.Save())
			;

			if (saveStatistics)
			{
				storage
					.Set(nameof(PnLManager), PnLManager.Save())
					.Set(nameof(StatisticManager), StatisticManager.Save())
				;
			}

			if (saveSecurity)
				storage.Set(nameof(Security), Security?.Id);

			if (savePortfolio)
				storage.Set(nameof(Portfolio), Portfolio?.Name);
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
		public void CancelActiveOrders()
		{
			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.StrategyInStateCannotCancelOrder, ProcessState);
				return;
			}

			this.AddInfoLog(LocalizedStrings.CancelAll);

			ProcessCancelActiveOrders();
		}

		/// <summary>
		/// To cancel all active orders (to stop and regular).
		/// </summary>
		protected virtual void ProcessCancelActiveOrders()
		{
			_ordersInfo.SyncGet(d => d.Keys.Filter(OrderStates.Active).ToArray()).ForEach(o =>
			{
				var info = _ordersInfo.TryGetValue(o);

				//заявка принадлежит дочерней статегии
				if (!info.IsOwn)
					return;

				if (info.IsCanceled)
				{
					this.AddWarningLog(LocalizedStrings.OrderAlreadySentCancel, o.TransactionId);
					return;
				}

				info.IsCanceled = true;

				CancelOrderHandler(o);
			});
		}

		private void OnChildOrderChanged(Order order)
		{
			ProcessOrder(order, true);
		}

		private void OnChildOrderRegisterFailed(OrderFail fail)
		{
			TryInvoke(() => OrderRegisterFailed?.Invoke(fail));
		}

		private void OnChildOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => OrderCancelFailed?.Invoke(fail));
		}

		private void ProcessCancelOrderFail(OrderFail fail)
		{
			if(IsDisposeStarted)
				return;

			var order = fail.Order;

			lock (_ordersInfo.SyncRoot)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null || !info.IsOwn)
					return;

				info.IsCanceled = false;
			}

			this.AddErrorLog(LocalizedStrings.ErrorCancellingOrder, order.TransactionId, fail.Error);

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

			if (!StopOnChildStrategyErrors && !Equals(this, strategy))
				return;

			this.AddErrorLog(error.ToString());
		}

		object ICloneable.Clone() => Clone();

		/// <summary>
		/// Create a copy of <see cref="Strategy"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual Strategy Clone()
		{
			var clone = GetType().CreateInstance<Strategy>();
			//clone.Connector = Connector;
			clone.Security = Security;
			clone.Portfolio = Portfolio;
			clone.PortfolioProvider = PortfolioProvider;

			var id = clone.Id;
			clone.Load(this.Save());
			clone.Id = id;

			return clone;
		}

		/// <summary>
		/// <see cref="IPortfolioProvider"/>
		/// </summary>
		public IPortfolioProvider PortfolioProvider { get; set; }

        private void TryInvoke(Action handler)
		{
			if (!IsDisposeStarted)
				handler();
		}

		private bool IsOwnOrder(Order order)
		{
			var info = _ordersInfo.TryGetValue(order);
			return info != null && info.IsOwn;
		}

		private RiskActions? ProcessRisk(Func<Message> getMessage)
		{
			if (getMessage is null)
				throw new ArgumentNullException(nameof(getMessage));

			if (RiskManager.Rules.Count == 0)
				return null;

			foreach (var rule in RiskManager.ProcessRules(getMessage()))
			{
				this.AddWarningLog(LocalizedStrings.ActivatingRiskRule,
					rule.Name, rule.Title, rule.Action);

				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
						this.ClosePosition();
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
		public virtual IEnumerable<Security> GetWorkingSecurities()
		{
			var security = Security;

			if (security is not null)
				yield return security;
		}

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
			GroupName = LocalizedStrings.SettingsKey,
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
		public bool IsBacktesting => Connector is HistoryEmulationConnector;

		private ISecurityProvider SecurityProvider => SafeGetConnector();

		int ISecurityProvider.Count => SecurityProvider.Count;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => SecurityProvider.Added += value;
			remove => SecurityProvider.Added -= value;
		}

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => SecurityProvider.Removed += value;
			remove => SecurityProvider.Removed -= value;
		}

		event Action ISecurityProvider.Cleared
		{
			add => SecurityProvider.Cleared += value;
			remove => SecurityProvider.Cleared -= value;
		}

		/// <inheritdoc />
		public Security LookupById(SecurityId id) => SecurityProvider.LookupById(id);

		/// <inheritdoc />
		public IEnumerable<Security> Lookup(SecurityLookupMessage criteria) => SecurityProvider.Lookup(criteria);

		SecurityMessage ISecurityMessageProvider.LookupMessageById(SecurityId id)
			=> SecurityProvider.LookupMessageById(id);

		IEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessages(SecurityLookupMessage criteria)
			=> SecurityProvider.LookupMessages(criteria);

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
					var slippage = parameters.TryGet(nameof(Order.Slippage)).To<decimal?>();

					this.ClosePosition(slippage ?? 0);

					break;
				}
			}
		}

		private void OnChildStrategyIsOnlineChanged(Strategy _)
		{
			if(IsOnlineStateIncludesChildren)
				CheckRefreshOnlineState();
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

				if (nowOnline && IsOnlineStateIncludesChildren)
					nowOnline = _childStrategies.SyncGet(c => c.All(s => s.IsOnline));

				if(nowOnline == wasOnline)
					return;

				IsOnline = nowOnline;
			}

			this.AddInfoLog("IsOnline: {0} ==> {1}. state={2}, children({3})", wasOnline, nowOnline, ProcessState, IsOnlineStateIncludesChildren);

			IsOnlineChanged?.Invoke(this);
		}

		//private bool IsChildOrder(Order order)
		//{
		//	var info = _ordersInfo.TryGetValue(order);
		//	return info != null && !info.IsOwn;
		//}

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
		public virtual IEnumerable<IOrderBookSource> OrderBookSources
			=> Enumerable.Empty<IOrderBookSource>();

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
			ChildStrategies.ForEach(s => s.Dispose());
			ChildStrategies.Clear();

			Connector = null;

			Parameters.Dispose();

			base.DisposeManaged();
		}
	}
}
