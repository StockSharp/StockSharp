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

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Positions;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Statistics;
	using StockSharp.Algo.Strategies.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// <see cref="Order.Comment"/> auto-fill modes.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum StrategyCommentModes
	{
		/// <summary>
		/// Disabled.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str2558Key)]
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
	/// The base class for all trade strategies.
	/// </summary>
	public partial class Strategy : BaseLogReceiver, INotifyPropertyChangedEx, IMarketRuleContainer,
	    ICloneable<Strategy>, IMarketDataProvider, ISubscriptionProvider, ISecurityProvider, ICandleManager,
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

		private static readonly MemoryStatisticsValue<Strategy> _strategyStat = new MemoryStatisticsValue<Strategy>(LocalizedStrings.Str1355);

		static Strategy()
		{
			MemoryStatistics.Instance.Values.Add(_strategyStat);
		}

		private sealed class ChildStrategyList : SynchronizedSet<Strategy>, IStrategyChildStrategyList
		{
			private readonly Dictionary<Strategy, IMarketRule> _childStrategyRules = new Dictionary<Strategy, IMarketRule>();
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
					throw new ArgumentException(LocalizedStrings.Str1356);

				item.Parent = _parent;
				item.Connector = _parent.Connector;

				if (item.Portfolio == null)
					item.Portfolio = _parent.Portfolio;

				if (item.Security == null)
					item.Security = _parent.Security;

				item.OrderRegistering += _parent.ProcessChildOrderRegistering;
				item.OrderRegistered += _parent.ProcessOrder;
				//item.ReRegistering += _parent.ReRegisterSlippage;
				item.OrderChanged += _parent.OnChildOrderChanged;
				item.OrderRegisterFailed += _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed += _parent.OnChildOrderCancelFailed;
				item.NewMyTrade += _parent.AddMyTrade;
				item.OrderReRegistering += _parent.OnOrderReRegistering;
				item.ProcessStateChanged += OnChildProcessStateChanged;
				item.Error += _parent.OnError;

				item.Orders.ForEach(_parent.ProcessOrder);

				if (!item.MyTrades.IsEmpty())
					item.MyTrades.ForEach(_parent.AddMyTrade);

				//_parent._orderFails.AddRange(item.OrderFails);

				if (item.ProcessState == _parent.ProcessState && _parent.ProcessState == ProcessStates.Started)
					OnChildProcessStateChanged(item);
				else
					item.ProcessState = _parent.ProcessState;
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
			}

			protected override bool OnClearing()
			{
				foreach (var item in ToArray())
					Remove(item);

				return true;
			}

			protected override bool OnRemoving(Strategy item)
			{
				//item.Parent = null;

				item.OrderRegistering -= _parent.ProcessChildOrderRegistering;
				item.OrderRegistered -= _parent.ProcessOrder;
				//item.ReRegistering -= _parent.ReRegisterSlippage;
				item.OrderChanged -= _parent.OnChildOrderChanged;
				item.OrderRegisterFailed -= _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed -= _parent.OnChildOrderCancelFailed;
				item.OrderCanceling -= _parent.OnOrderCanceling;
				item.NewMyTrade -= _parent.AddMyTrade;
				item.OrderReRegistering -= _parent.OnOrderReRegistering;
				item.ProcessStateChanged -= OnChildProcessStateChanged;
				item.Error -= _parent.OnError;

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
			public OrderFail RegistrationFail { get; set; }
			//public OrderFail CancelationFail { get; set; }
			public OrderStates PrevState { get; set; }
		}

		private readonly CachedSynchronizedDictionary<Order, OrderInfo> _ordersInfo = new CachedSynchronizedDictionary<Order, OrderInfo>();

		private readonly CachedSynchronizedDictionary<Subscription, bool> _subscriptions = new CachedSynchronizedDictionary<Subscription, bool>();
		private readonly SynchronizedDictionary<long, Subscription> _subscriptionsById = new SynchronizedDictionary<long, Subscription>();
		private Subscription _pfSubscription;
		private Subscription _orderSubscription;

		private DateTimeOffset _firstOrderTime;
		private DateTimeOffset _lastOrderTime;
		private TimeSpan _maxOrdersKeepTime;
		private DateTimeOffset _lastPnlRefreshTime;
		private DateTimeOffset _prevTradeDate;
		private bool _isPrevDateTradable;
		private bool _stopping;
		private DateTimeOffset _lastRegisterTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="Strategy"/>.
		/// </summary>
		public Strategy()
		{
			_childStrategies = new ChildStrategyList(this);

			Rules = new StrategyRuleList(this);

			NameGenerator = new StrategyNameGenerator(this);
			NameGenerator.Changed += name => _name.Value = name;

			_id = this.Param(nameof(Id), base.Id);
			_volume = this.Param<decimal>(nameof(Volume), 1);
			_name = this.Param(nameof(Name), new string(GetType().Name.Where(char.IsUpper).ToArray()));
			_maxErrorCount = this.Param(nameof(MaxErrorCount), 1);
			_maxOrderRegisterErrorCount = this.Param(nameof(MaxOrderRegisterErrorCount), 10);
			_disposeOnStop = this.Param(nameof(DisposeOnStop), false);
			_waitRulesOnStop = this.Param(nameof(WaitRulesOnStop), true);
			_cancelOrdersWhenStopping = this.Param(nameof(CancelOrdersWhenStopping), true);
			_waitAllTrades = this.Param<bool>(nameof(WaitAllTrades));
			_commentMode = this.Param<StrategyCommentModes>(nameof(CommentMode));
			_ordersKeepTime = this.Param(nameof(OrdersKeepTime), TimeSpan.FromDays(1));
			_logLevel = this.Param(nameof(LogLevel), LogLevels.Inherit);
			_stopOnChildStrategyErrors = this.Param(nameof(StopOnChildStrategyErrors), false);
			_restoreChildOrders = this.Param(nameof(RestoreChildOrders), false);
			_allowTrading = this.Param(nameof(AllowTrading), true);
			_unsubscribeOnStop = this.Param(nameof(UnsubscribeOnStop), true);
			_maxRegisterCount = this.Param(nameof(MaxRegisterCount), int.MaxValue);
			_registerInterval = this.Param<TimeSpan>(nameof(RegisterInterval));
			_workingTime = this.Param(nameof(WorkingTime), new WorkingTime());

			_maxErrorCount.CanOptimize =
			_maxOrderRegisterErrorCount.CanOptimize =
			_maxRegisterCount.CanOptimize = false;
			
			InitMaxOrdersKeepTime();

			_strategyStat.Add(this);

			RiskManager = new RiskManager { Parent = this };

			_positionManager = new PositionManager(true) { Parent = this };
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
		[CategoryLoc(LocalizedStrings.LoggingKey)]
		//[PropertyOrder(8)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str1358Key)]
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
			Description = LocalizedStrings.Str1359Key,
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
					_connector.OrderReceived -= OnConnectorOrderReceived;
					_connector.OwnTradeReceived -= OnConnectorOwnTradeReceived;
					_connector.OrderRegisterFailed -= OnConnectorOrderRegisterFailed;
					_connector.OrderCancelFailed -= ProcessCancelOrderFail;
					_connector.OrderEdited -= OnConnectorOrderEdited;
					_connector.OrderEditFailed -= OnConnectorOrderEditFailed;
					_connector.NewMessage -= OnConnectorNewMessage;
					_connector.ValuesChanged -= OnConnectorValuesChanged;
					_connector.PositionReceived -= OnConnectorPositionReceived;
					_connector.Level1Received -= OnConnectorLevel1Received;
					_connector.OrderBookReceived -= OnConnectorOrderBookReceived;
					_connector.TickTradeReceived -= OnConnectorTickTradeReceived;
					_connector.SecurityReceived -= OnConnectorSecurityReceived;
					_connector.BoardReceived -= OnConnectorBoardReceived;
					_connector.MarketDepthReceived -= OnConnectorMarketDepthReceived;
					_connector.OrderLogItemReceived -= OnConnectorOrderLogItemReceived;
					_connector.NewsReceived -= OnConnectorNewsReceived;
					_connector.CandleReceived -= OnConnectorCandleReceived;
					_connector.OrderRegisterFailReceived -= OnConnectorOrderRegisterFailReceived;
					_connector.OrderCancelFailReceived -= OnConnectorOrderCancelFailReceived;
					_connector.OrderEditFailReceived -= OnConnectorOrderEditFailReceived;
					_connector.PortfolioReceived -= OnConnectorPortfolioReceived;
					_connector.SubscriptionReceived -= OnConnectorSubscriptionReceived;
					_connector.SubscriptionOnline -= OnConnectorSubscriptionOnline;
					_connector.SubscriptionStarted -= OnConnectorSubscriptionStarted;
					_connector.SubscriptionStopped -= OnConnectorSubscriptionStopped;
					_connector.SubscriptionFailed -= OnConnectorSubscriptionFailed;

					UnSubscribe(true);
				}

				_connector = value;

				if (_connector != null)
				{
					_connector.OrderReceived += OnConnectorOrderReceived;
					_connector.OwnTradeReceived += OnConnectorOwnTradeReceived;
					_connector.OrderRegisterFailed += OnConnectorOrderRegisterFailed;
					_connector.OrderCancelFailed += ProcessCancelOrderFail;
					_connector.OrderEdited += OnConnectorOrderEdited;
					_connector.OrderEditFailed += OnConnectorOrderEditFailed;
					_connector.NewMessage += OnConnectorNewMessage;
					_connector.ValuesChanged += OnConnectorValuesChanged;
					_connector.PositionReceived += OnConnectorPositionReceived;
					_connector.Level1Received += OnConnectorLevel1Received;
					_connector.OrderBookReceived += OnConnectorOrderBookReceived;
					_connector.TickTradeReceived += OnConnectorTickTradeReceived;
					_connector.SecurityReceived += OnConnectorSecurityReceived;
					_connector.BoardReceived += OnConnectorBoardReceived;
					_connector.MarketDepthReceived += OnConnectorMarketDepthReceived;
					_connector.OrderLogItemReceived += OnConnectorOrderLogItemReceived;
					_connector.NewsReceived += OnConnectorNewsReceived;
					_connector.CandleReceived += OnConnectorCandleReceived;
					_connector.OrderRegisterFailReceived += OnConnectorOrderRegisterFailReceived;
					_connector.OrderCancelFailReceived += OnConnectorOrderCancelFailReceived;
					_connector.OrderEditFailReceived += OnConnectorOrderEditFailReceived;
					_connector.PortfolioReceived += OnConnectorPortfolioReceived;
					_connector.SubscriptionReceived += OnConnectorSubscriptionReceived;
					_connector.SubscriptionOnline += OnConnectorSubscriptionOnline;
					_connector.SubscriptionStarted += OnConnectorSubscriptionStarted;
					_connector.SubscriptionStopped += OnConnectorSubscriptionStopped;
					_connector.SubscriptionFailed += OnConnectorSubscriptionFailed;

					if (ParentStrategy == null)
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

				foreach (var strategy in ChildStrategies)
					strategy.Connector = value;

				ConnectorChanged?.Invoke();
			}
		}

		private void OnConnectorPortfolioReceived(Subscription subscription, Portfolio portfolio)
		{
			if (_subscriptions.ContainsKey(subscription))
				PortfolioReceived?.Invoke(subscription, portfolio);
		}

		private void OnConnectorOrderEditFailReceived(Subscription subscription, OrderFail fail)
		{
			if (_subscriptions.ContainsKey(subscription))
				OrderEditFailReceived?.Invoke(subscription, fail);
		}

		private void OnConnectorOrderCancelFailReceived(Subscription subscription, OrderFail fail)
		{
			if (_subscriptions.ContainsKey(subscription))
				OrderCancelFailReceived?.Invoke(subscription, fail);
		}

		private void OnConnectorOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		{
			if (_subscriptions.ContainsKey(subscription))
				OrderRegisterFailReceived?.Invoke(subscription, fail);
		}

		/// <summary>
		/// To get the strategy getting <see cref="Connector"/>. If it is not initialized, the exception will be discarded.
		/// </summary>
		/// <returns>Connector.</returns>
		public IConnector SafeGetConnector()
		{
			var connector = Connector;

			if (connector == null)
				throw new InvalidOperationException(LocalizedStrings.Str1360);

			return connector;
		}

		private Portfolio _portfolio;

		/// <summary>
		/// Portfolio.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PortfolioKey,
			Description = LocalizedStrings.Str1361Key,
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
					if (strategy.Portfolio == null)
						strategy.Portfolio = value;
				}

				RaiseParametersChanged(nameof(Portfolio));

				this.Notify(nameof(Position));
			}
		}

		private Security _security;

		/// <summary>
		/// Security.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecurityKey,
			Description = LocalizedStrings.Str1362Key,
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
					if (strategy.Security == null)
						strategy.Security = value;
				}
				
				RaiseParametersChanged(nameof(Security));

				this.Notify(nameof(Position));

				//PositionManager.SecurityId = value?.ToSecurityId();
			}
		}

		/// <summary>
		/// Total slippage.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str163Key,
			Description = LocalizedStrings.Str1363Key,
			GroupName = LocalizedStrings.Str436Key,
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
		/// The profit-loss manager. It accounts trades of this strategy, as well as of its subsidiary strategies <see cref="Strategy.ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		public IPnLManager PnLManager
		{
			get => _pnLManager;
			set => _pnLManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// The aggregate value of profit-loss without accounting commission <see cref="Strategy.Commission"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PnLKey,
			Description = LocalizedStrings.Str1364Key,
			GroupName = LocalizedStrings.Str436Key,
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
		public event Action<Subscription> PnLReceived;

		/// <summary>
		/// Total commission.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str159Key,
			Description = LocalizedStrings.Str1365Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 101)]
		[ReadOnly(true)]
		[Browsable(false)]
		public decimal? Commission { get; private set; }

		/// <summary>
		/// <see cref="Commission"/> change event.
		/// </summary>
		public event Action CommissionChanged;

		private IPositionManager _positionManager;

		/// <summary>
		/// The position manager. It accounts trades of this strategy, as well as of its subsidiary strategies <see cref="Strategy.ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Use Positions property.")]
		public IPositionManager PositionManager
		{
			get => _positionManager;
			set => _positionManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// The position aggregate value.
		/// </summary>
		[Browsable(false)]
		public decimal Position
		{
			get => Security == null || Portfolio == null ? 0m : GetPositionValue(Security, Portfolio) ?? 0;
			[Obsolete]
			set	{ }
		}

		/// <summary>
		/// <see cref="Position"/> change event.
		/// </summary>
		public event Action PositionChanged;

		/// <summary>
		/// Total latency.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str161Key,
			Description = LocalizedStrings.Str1366Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 102)]
		[ReadOnly(true)]
		[Browsable(false)]
		public TimeSpan? Latency { get; private set; }

		/// <summary>
		/// <see cref="Latency"/> change event.
		/// </summary>
		public event Action LatencyChanged;

		private StatisticManager _statisticManager = new StatisticManager();

		/// <summary>
		/// The statistics manager.
		/// </summary>
		[Browsable(false)]
		public StatisticManager StatisticManager
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
		/// Strategy parameters.
		/// </summary>
		[Browsable(false)]
		public CachedSynchronizedDictionary<string, IStrategyParam> Parameters { get; } = new CachedSynchronizedDictionary<string, IStrategyParam>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// <see cref="Parameters"/> change event.
		/// </summary>
		public event Action ParametersChanged;

		/// <summary>
		/// To call events <see cref="ParametersChanged"/> and <see cref="PropertyChanged"/>.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		protected internal void RaiseParametersChanged(string name)
		{
			ParametersChanged?.Invoke();
			this.Notify(name);
		}

		/// <summary>
		/// Strategy environment parameters.
		/// </summary>
		[Browsable(false)]
		public SettingsStorage Environment { get; } = new SettingsStorage();

		private readonly StrategyParam<int> _maxErrorCount;

		/// <summary>
		/// The maximal number of errors, which strategy shall receive prior to stop operation.
		/// </summary>
		/// <remarks>
		/// The default value is 1.
		/// </remarks>
		[Browsable(false)]
		public int MaxErrorCount
		{
			get => _maxErrorCount.Value;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1367);

				_maxErrorCount.Value = value;
			}
		}

		private int _errorCount;

		/// <summary>
		/// The current number of errors.
		/// </summary>
		[Browsable(false)]
		public int ErrorCount
		{
			get => _errorCount;
			private set
			{
				if (_errorCount == value)
					return;

				_errorCount = value;
				this.Notify(nameof(ErrorCount));
			}
		}

		private readonly StrategyParam<int> _maxOrderRegisterErrorCount;

		/// <summary>
		/// The maximum number of order registration errors above which the algorithm will be stopped.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MaxOrderRegisterErrorCountKey,
			Description = LocalizedStrings.MaxOrderRegisterErrorCountDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 12)]
		public virtual int MaxOrderRegisterErrorCount
		{
			get => _maxOrderRegisterErrorCount.Value;
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_maxOrderRegisterErrorCount.Value = value;
			}
		}

		private int _orderRegisterErrorCount;

		/// <summary>
		/// Current number of order registration errors.
		/// </summary>
		[Browsable(false)]
		public int OrderRegisterErrorCount
		{
			get => _orderRegisterErrorCount;
			private set
			{
				if (_orderRegisterErrorCount == value)
					return;

				_orderRegisterErrorCount = value;
				this.Notify(nameof(OrderRegisterErrorCount));
			}
		}

		/// <summary>
		/// Current number of order changes.
		/// </summary>
		[Browsable(false)]
		public int CurrentRegisterCount { get; private set; }

		private readonly StrategyParam<int> _maxRegisterCount;

		/// <summary>
		/// The maximum number of orders above which the algorithm will be stopped.
		/// </summary>
		/// <remarks>
		/// The default value is <see cref="int.MaxValue"/>.
		/// </remarks>
		public virtual int MaxRegisterCount
		{
			get => _maxRegisterCount.Value;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1291);

				_maxRegisterCount.Value = value;
			}
		}

		private readonly StrategyParam<TimeSpan> _registerInterval;

		/// <summary>
		/// The order registration interval above which the new order would not be registered.
		/// </summary>
		/// <remarks>
		/// By default, the interval is disabled and it is equal to <see cref="TimeSpan.Zero"/>.
		/// </remarks>
		public virtual TimeSpan RegisterInterval
		{
			get => _registerInterval.Value;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_registerInterval.Value = value;
			}
		}

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
			set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));

				if (WorkingTime == value)
					return;

				_workingTime.Value = value;
			}
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

				this.AddDebugLog(LocalizedStrings.Str1368Params, _processState, value);

				if (_processState == ProcessStates.Stopped && value == ProcessStates.Stopping)
					throw new InvalidOperationException(LocalizedStrings.Str1369Params.Put(Name, value));

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
							StartedTime = CurrentTime;
							LogProcessState(value);
							OnStarted();
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
							TotalWorkingTime += CurrentTime - StartedTime;
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
				}

				try
				{
					RaiseProcessStateChanged(this);
					this.Notify(nameof(ProcessState));
				}
				catch (Exception error)
				{
					OnError(this, error);
				}
				
				if (ProcessState == ProcessStates.Stopping)
				{
					if (CancelOrdersWhenStopping)
					{
						this.AddInfoLog(LocalizedStrings.Str1370);
						ProcessCancelActiveOrders();
					}

					foreach (var rule in Rules.ToArray())
					{
						if (this.TryRemoveWithExclusive(rule))
							_childStrategies.TryRemoveStoppedRule(rule);
					}

					TryFinalStop();
				}

				RaiseNewStateMessage(nameof(ProcessState), ProcessState);
			}
		}

		private void LogProcessState(ProcessStates state)
		{
			string stateStr;

			switch (state)
			{
				case ProcessStates.Stopped:
					stateStr = LocalizedStrings.Str1371;
					break;
				case ProcessStates.Stopping:
					stateStr = LocalizedStrings.Str1372;
					break;
				case ProcessStates.Started:
					stateStr = LocalizedStrings.Str1373;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.Str1219);
			}

			this.AddInfoLog(LocalizedStrings.Str1374Params, stateStr, ChildStrategies.Count, Parent != null ? ParentStrategy.ChildStrategies.Count : -1, Position);
		}

		private Strategy ParentStrategy => Parent as Strategy;

		/// <summary>
		/// <see cref="ProcessState"/> change event.
		/// </summary>
		public event Action<Strategy> ProcessStateChanged;

		/// <summary>
		/// To call the event <see cref="Strategy.ProcessStateChanged"/>.
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
		public virtual bool CancelOrdersWhenStopping
		{
			get => _cancelOrdersWhenStopping.Value;
			set => _cancelOrdersWhenStopping.Value = value;
		}

		/// <summary>
		/// Orders, registered within the strategy framework.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<Order> Orders => _ordersInfo.CachedKeys;

		/// <summary>
		/// Stop-orders, registered within the strategy framework.
		/// </summary>
		[Browsable(false)]
		[Obsolete("Use Orders property.")]
		public IEnumerable<Order> StopOrders => Orders.Where(o => o.Type == OrderTypes.Conditional);

		private readonly StrategyParam<TimeSpan> _ordersKeepTime;

		/// <summary>
		/// The time for storing <see cref="Orders"/> and <see cref="StopOrders"/> orders in memory. By default it equals to 2 days. If value is set in <see cref="TimeSpan.Zero"/>, orders will not be deleted.
		/// </summary>
		[Browsable(false)]
		public TimeSpan OrdersKeepTime
		{
			get => _ordersKeepTime.Value;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1375);

				_ordersKeepTime.Value = value;
				InitMaxOrdersKeepTime();
				RecycleOrders();
			}
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

		private readonly StrategyParam<bool> _allowTrading;

		/// <summary>
		/// Allow trading.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str3599Key,
			Description = LocalizedStrings.AllowTradingKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 10)]
		public virtual bool AllowTrading
		{
			get => _allowTrading.Value;
			set => _allowTrading.Value = value;
		}

		private readonly StrategyParam<bool> _unsubscribeOnStop;

		/// <summary>
		/// Unsubscribe all active subscription while strategy become stopping.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.XamlStr426Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 11)]
		public virtual bool UnsubscribeOnStop
		{
			get => _unsubscribeOnStop.Value;
			set => _unsubscribeOnStop.Value = value;
		}

		private void InitMaxOrdersKeepTime()
		{
			_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		}

		private readonly CachedSynchronizedSet<MyTrade> _myTrades = new CachedSynchronizedSet<MyTrade>();

		/// <summary>
		/// Trades, matched during the strategy operation.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

		/// <summary>
		/// Orders with errors, registered within the strategy.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<OrderFail> OrderFails => _ordersInfo.CachedValues.Where(i => i.RegistrationFail != null).Select(i => i.RegistrationFail);

		private readonly StrategyParam<decimal> _volume;

		/// <summary>
		/// Operational volume.
		/// </summary>
		/// <remarks>
		/// If the value is set 0, the parameter is ignored.
		/// </remarks>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.Str1376Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 4)]
		public virtual decimal Volume
		{
			get => _volume.Value;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1377);

				_volume.Value = value;
			}
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
				this.Notify(nameof(ErrorState));
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
			Name = LocalizedStrings.Str1378Key,
			Description = LocalizedStrings.Str1379Key,
			GroupName = LocalizedStrings.Str436Key,
			Order = 105)]
		[ReadOnly(true)]
		[Browsable(false)]
		public DateTimeOffset StartedTime
		{
			get => _startedTime;
			private set
			{
				_startedTime = value;
				this.Notify(nameof(StartedTime));
			}
		}

		private TimeSpan _totalWorkingTime;

		/// <summary>
		/// The total time of strategy operation less time periods, when strategy was stopped.
		/// </summary>
		[Browsable(false)]
		public TimeSpan TotalWorkingTime
		{
			get
			{
				var retVal = _totalWorkingTime;

				if (StartedTime != default && Connector != null)
					retVal += CurrentTime - StartedTime;

				return retVal;
			}
			private set
			{
				if (_totalWorkingTime == value)
					return;

				_totalWorkingTime = value;
				this.Notify(nameof(TotalWorkingTime));
			}
		}

		private readonly StrategyParam<bool> _disposeOnStop;

		/// <summary>
		/// Automatically to clear resources, used by the strategy, when it stops (state <see cref="Strategy.ProcessState"/> becomes equal to <see cref="ProcessStates.Stopped"/>) and delete it from the parent strategy through <see cref="Strategy.ChildStrategies"/>.
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
		//[Browsable(false)]
		public virtual bool WaitRulesOnStop
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
			Name = LocalizedStrings.Str135Key,
			Description = LocalizedStrings.Str136Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 10)]
		public virtual StrategyCommentModes CommentMode
		{
			get => _commentMode.Value;
			set => _commentMode.Value = value;
		}

		/// <summary>
		/// To add to <see cref="Order.Comment"/> the name of the strategy <see cref="Name"/>, registering the order.
		/// </summary>
		/// <remarks>
		/// It is disabled by default.
		/// </remarks>
		[Browsable(false)]
		[Obsolete("Use CommentMode property.")]
		public bool CommentOrders
		{
			get => CommentMode == StrategyCommentModes.Name;
			set => CommentMode = value ? StrategyCommentModes.Name : StrategyCommentModes.Disabled;
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

#pragma warning disable 67
		/// <inheritdoc />
		[Obsolete("Use OrderRegisterFailed event.")]
		public event Action<OrderFail> StopOrderRegisterFailed;

		/// <inheritdoc />
		[Obsolete("Use OrderChanged event.")]
		public event Action<Order> StopOrderChanged;

		/// <summary>
		/// The event of sending stop-order for registration.
		/// </summary>
		[Obsolete("Use OrderRegistering event.")]
		public event Action<Order> StopOrderRegistering;

		/// <summary>
		/// The event of stop-order successful registration.
		/// </summary>
		[Obsolete("Use OrderRegistered event.")]
		public event Action<Order> StopOrderRegistered;

		/// <summary>
		/// The event of sending stop-order for cancelling.
		/// </summary>
		[Obsolete("Use OrderCanceling event.")]
		public event Action<Order> StopOrderCanceling;

		/// <summary>
		/// The event of sending stop-order for re-registration.
		/// </summary>
		[Obsolete("Use OrderReRegistering event.")]
		public event Action<Order, Order> StopOrderReRegistering;

		/// <inheritdoc />
		[Obsolete("Use OrderCancelFailed event.")]
		public event Action<OrderFail> StopOrderCancelFailed;
#pragma warning restore 67

		/// <inheritdoc />
		public event Action<MyTrade> NewMyTrade;

		/// <summary>
		/// The event of strategy connection change.
		/// </summary>
		public event Action ConnectorChanged;

		/// <summary>
		/// The event of error occurrence in the strategy.
		/// </summary>
		public event Action<Strategy, Exception> Error;

		/// <summary>
		/// The method is called when the <see cref="Start()"/> method has been called and the <see cref="ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
		/// </summary>
		protected virtual void OnStarted()
		{
			if (Security == null)
				throw new InvalidOperationException(LocalizedStrings.Str1380);

			if (Portfolio == null)
				throw new InvalidOperationException(LocalizedStrings.Str1381);

			InitStartValues();
		}
		
		/// <summary>
		/// Init.
		/// </summary>
		protected void InitStartValues()
		{
			foreach (var parameter in Parameters.CachedValues)
			{
				if (parameter.Value is Unit unit && unit.GetTypeValue == null && (unit.Type == UnitTypes.Point || unit.Type == UnitTypes.Step))
					unit.SetSecurity(Security);
			}

			ErrorCount = default;
			ErrorState = LogLevels.Info;

			OrderRegisterErrorCount = default;
			CurrentRegisterCount = default;
			_lastRegisterTime = default;
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

		private bool CheckRegisterLimits()
		{
			if (CurrentRegisterCount >= MaxRegisterCount)
			{
				this.AddWarningLog(LocalizedStrings.Str1317Params, MaxRegisterCount);
				Stop();
				return false;
			}

			CurrentRegisterCount++;

			return true;
		}

		private bool CheckIntervalLimit()
		{
			if (RegisterInterval == default)
				return true;

			var now = CurrentTime;
			var diff = (_lastRegisterTime + RegisterInterval) - now;

			if (diff >= TimeSpan.Zero)
			{
				this.AddInfoLog(LocalizedStrings.Str1318 + " " +
					LocalizedStrings.Str1319Params, diff, _lastRegisterTime, now, RegisterInterval);

				return false;
			}
			else
			{
				_lastRegisterTime = now;
				return true;
			}
		}

		private bool CanTrade()
		{
			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.Str1383Params, ProcessState);
				return false;
			}

			if (!AllowTrading)
			{
				this.AddWarningLog(LocalizedStrings.AllowTrading);
				return false;
			}

			if (_stopping)
			{
				this.AddWarningLog("Strategy is stopping.");
				return false;
			}

			if (!CheckRegisterLimits())
				return false;

			if (!CheckIntervalLimit())
				return false;

			return true;
		}

		/// <inheritdoc />
		public void RegisterOrder(Order order)
		{
			if (order is null)
				throw new ArgumentNullException(nameof(order));

			this.AddInfoLog(LocalizedStrings.Str1382Params,
				order.Type, order.Direction, order.Price, order.Volume, order.Comment, order.GetHashCode());

			if (!CanTrade())
				return;

			if (order.Security == null)
				order.Security = Security;

			if (order.Portfolio == null)
				order.Portfolio = Portfolio;

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

			AddOrder(order, false);

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

			if (!CanTrade())
				return;

			SafeGetConnector().EditOrder(order, changes);	
		}

		/// <inheritdoc />
		public void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			this.AddInfoLog(LocalizedStrings.Str1384Params, oldOrder.TransactionId, oldOrder.Price, newOrder.Price, oldOrder.Comment);

			if (!CanTrade())
				return;

			AddOrder(newOrder, false);

			ProcessRegisterOrderAction(oldOrder, newOrder, (oOrder, nOrder) =>
			{
				OnOrderReRegistering(oOrder, nOrder);

				//ReRegisterSlippage(oOrder, nOrder);

				SafeGetConnector().ReRegisterOrder(oOrder, nOrder);	
			});
		}

		private void ProcessRisk(Order order)
		{
			ProcessRisk(order.CreateRegisterMessage());
		}

		private void ProcessChildOrderRegistering(Order order)
		{
			OnOrderRegistering(order);

			_newOrder?.Invoke(order);

			ProcessRisk(order);
		}

		private void AddOrder(Order order, bool restored)
		{
			_ordersInfo.Add(order, new OrderInfo { IsOwn = true });

			if (!restored)
				order.UserOrderId = EnsureGetId();

			order.StrategyId = EnsureGetRootId();

			if (!order.State.IsFinal())
				ApplyMonitorRules(order);

			_newOrder?.Invoke(order);

			ProcessRisk(order);
		}

		private void ProcessRegisterOrderAction(Order oOrder, Order nOrder, Action<Order, Order> action)
		{
			try
			{
				action(oOrder, nOrder);
			}
			catch (Exception excp)
			{
				Rules.RemoveRulesByToken(nOrder, null);

				nOrder.ApplyNewState(OrderStates.Failed, this);

				var fail = new OrderFail { Order = nOrder, Error = excp, ServerTime = CurrentTime };

				OnConnectorOrderRegisterFailed(fail);
			}
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
				.Do(() => this.AddInfoLog(LocalizedStrings.Str1386Params.Put(order.TransactionId)))
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
						this.AddWarningLog(LocalizedStrings.Str1156Params, order.TransactionId);
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
				.Do(() =>
				{
					if (ProcessState != ProcessStates.Stopping)
						return;

					canFinish = true;
					this.AddInfoLog(LocalizedStrings.Str1387Params.Put(order.TransactionId));
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
				this.AddWarningLog(LocalizedStrings.Str1388Params, ProcessState);
				return;
			}

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			lock (_ordersInfo.SyncRoot)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null || !info.IsOwn)
					throw new ArgumentException(LocalizedStrings.Str1389Params.Put(order.TransactionId, Name));

				if (info.IsCanceled)
				{
					this.AddWarningLog(LocalizedStrings.Str1390Params, order.TransactionId);
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

			this.AddInfoLog(LocalizedStrings.Str1315Params, order.TransactionId);

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

			TryInvoke(() =>
			{
				if (isRegistered)
				{
					if (order.Type == OrderTypes.Conditional)
					{
						//_stopOrders.Add(order);
						OnOrderRegistered(order);

						StatisticManager.AddNewOrder(order);
					}
					else
					{
						//_orders.Add(order);
						OnOrderRegistered(order);

						//SlippageManager.Registered(order);

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
					StatisticManager.AddChangedOrder(order);

					OnOrderChanged(order);
				}
			});
		}

		/// <summary>
		/// To add the active order to the strategy and process trades by the order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="myTrades">Trades for order.</param>
		/// <remarks>
		/// It is used to restore a state of the strategy, when it is necessary to subscribe for getting data on orders, registered earlier.
		/// </remarks>
		[Obsolete]
		public virtual void AttachOrder(Order order, IEnumerable<MyTrade> myTrades)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (myTrades == null)
				throw new ArgumentNullException(nameof(myTrades));

			AttachOrder(order, true);

			//myTrades.ForEach(OnConnectorNewMyTrade);
		}

		private void AttachOrder(Order order, bool restored)
		{
			this.AddInfoLog("Order {0} attached.", order.TransactionId);

			AddOrder(order, restored);

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
			_stopping = true;
			SafeGetConnector().SendOutMessage(new StrategyChangeStateMessage(this, ProcessStates.Stopping));
		}

		/// <summary>
		/// The event of the strategy re-initialization.
		/// </summary>
		public event Action Reseted;

		/// <summary>
		/// To call the event <see cref="Reseted"/>.
		/// </summary>
		private void RaiseReseted()
		{
			Reseted?.Invoke();
		}

		/// <summary>
		/// To re-initialize the trade algorithm. It is called after initialization of the strategy object and loading stored parameters.
		/// </summary>
		public virtual void Reset()
		{
			this.AddInfoLog(LocalizedStrings.Str1393);
			
			//ThrowIfTraderNotRegistered();

			//if (Security == null)
			//	throw new InvalidOperationException(LocalizedStrings.Str1380);

			//if (Portfolio == null)
			//	throw new InvalidOperationException(LocalizedStrings.Str1381);

			ChildStrategies.ForEach(s => s.Reset());

			StatisticManager.Reset();

			PnLManager.Reset();
			
			Commission = null;
			//CommissionManager.Reset();

			Latency = null;
			//LatencyManager.Reset();

			Slippage = null;
			//SlippageManager.Reset();

			RiskManager.Reset();

			_myTrades.Clear();
			_ordersInfo.Clear();

			ProcessState = ProcessStates.Stopped;
			ErrorState = LogLevels.Info;
			ErrorCount = 0;

			_firstOrderTime = _lastOrderTime = _lastPnlRefreshTime = _prevTradeDate = default;
			_idStr = null;

			_positions.Clear();

			_subscriptions.Clear();
			_subscriptionsById.Clear();

			OnReseted();

			// события вызываем только после вызова Reseted
			// чтобы сбросить состояние у подписчиков стратегии.
			RaisePnLChanged();
			RaiseCommissionChanged();
			RaiseLatencyChanged();
			RaisePositionChanged();
			RaiseSlippageChanged();
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

			this.AddDebugLog(LocalizedStrings.Str1394Params, _rulesSuspendCount);
		}

		void IMarketRuleContainer.ResumeRules()
		{
			if (_rulesSuspendCount > 0)
				_rulesSuspendCount--;

			this.AddDebugLog(LocalizedStrings.Str1395Params, _rulesSuspendCount);
		}

		private void TryFinalStop()
		{
			if (!Rules.IsEmpty())
			{
				if (WaitRulesOnStop)
				{
					this.AddLog(LogLevels.Info,
						() => LocalizedStrings.Str1396Params.Put(Rules.Count, Rules.Select(r => r.ToString()).JoinCommaSpace()));

					return;
				}
				else
				{
					foreach (var rule in Rules)
						rule.Dispose();

					Rules.Clear();
				}
			}

			ProcessState = ProcessStates.Stopped;

			if (DisposeOnStop)
			{
				//Trace.WriteLine(Name+" strategy-dispose-on-stop");

				ParentStrategy?.ChildStrategies.Remove(this);

				Dispose();
			}
		}

		void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
		{
			if (_rulesSuspendCount > 0)
			{
				this.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.Str1397);
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
		public virtual TimeSpan UnrealizedPnLInterval
		{
			get => _unrealizedPnLInterval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

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
		/// To call the event <see cref="Strategy.OrderRegistering"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderRegistering(Order order)
		{
			TryAddChildOrder(order);

			OrderRegistering?.Invoke(order);
			//SlippageManager.Registering(order);
		}

		/// <summary>
		/// To call the event <see cref="OrderRegistered"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderRegistered(Order order)
		{
			OrderRegisterErrorCount = 0;
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
		protected virtual void OnOrderRegisterFailed(OrderFail fail)
		{
			OrderRegisterFailed?.Invoke(fail);
			StatisticManager.AddRegisterFailedOrder(fail);
		}

		private void TryAddChildOrder(Order order)
		{
			lock (_ordersInfo.SyncRoot)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null)
					_ordersInfo.Add(order, new OrderInfo { IsOwn = false });
			}
		}

		private void OnConnectorNewMessage(Message message)
		{
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
							//case ProcessStates.Stopped:
							//	break;
							case ProcessStates.Stopping:
							{
								if (ProcessState == ProcessStates.Started)
									ProcessState = ProcessStates.Stopping;
								else
									this.AddDebugLog(LocalizedStrings.Str1392Params, ProcessState);

								break;
							}
							case ProcessStates.Started:
							{
								if (ProcessState == ProcessStates.Stopped)
									ProcessState = ProcessStates.Started;
								else
									this.AddDebugLog(LocalizedStrings.Str1391Params, ProcessState);

								break;
							}
							//default:
							//	throw new ArgumentOutOfRangeException();
						}
					}

					return;
				}

				default:
					return;
			}

			if (msgTime == null || msgTime.Value - _lastPnlRefreshTime < UnrealizedPnLInterval)
				return;

			_lastPnlRefreshTime = msgTime.Value;

			ExchangeBoard board = null;

			if (Security != null && Security.Board != null)
				board = Security.Board;
			else if (Portfolio != null && Portfolio.Board != null)
				board = Portfolio.Board;

			if (board != null)
			{
				var date = _lastPnlRefreshTime.Date;

				if (date != _prevTradeDate)
				{
					_prevTradeDate = date;
					_isPrevDateTradable = board.IsTradeDate(_prevTradeDate);
				}

				if (!_isPrevDateTradable)
					return;

				var period = board.WorkingTime.GetPeriod(date);

				var tod = _lastPnlRefreshTime.TimeOfDay;
				
				if (period != null && !period.Times.IsEmpty() && !period.Times.Any(r => r.Contains(tod)))
					return;
			}

			if (Positions.Any())
				RaisePnLChanged();
		}

		private void OnConnectorOwnTradeReceived(Subscription subscription, MyTrade trade)
		{
			if (_orderSubscription != subscription)
				return;

			if (IsOwnOrder(trade.Order))
				AddMyTrade(trade);

			OwnTradeReceived?.Invoke(subscription, trade);
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

			return order.UserOrderId.CompareIgnoreCase(id) || (RestoreChildOrders && order.StrategyId.CompareIgnoreCase(id));
		}

		private void OnConnectorOrderReceived(Subscription subscription, Order order)
		{
			if (_orderSubscription != subscription)
				return;

			if (!_ordersInfo.ContainsKey(order) && CanAttach(order))
				AttachOrder(order, true);
			else if (IsOwnOrder(order))
				TryInvoke(() => ProcessOrder(order, true));

			OrderReceived?.Invoke(subscription, order);
		}

		private void OnConnectorOrderEditFailed(long transactionId, OrderFail fail)
		{
			if (IsOwnOrder(fail.Order))
				OrderEditFailed?.Invoke(transactionId, fail);
		}

		private void OnConnectorOrderEdited(long transactionId, Order order)
		{
			if (IsOwnOrder(order))
			{
				OrderEdited?.Invoke(transactionId, order);
				ChangeLatency(order.LatencyEdition);
			}
		}

		private string _idStr;

		private string EnsureGetId()
		{
			if (_idStr == null)
				_idStr = Id.To<string>();

			return _idStr;
		}

		private string _rootIdStr;

		private string EnsureGetRootId()
		{
			if (_rootIdStr == null)
			{
				var root = this;

				while (root.ParentStrategy != null)
					root = root.ParentStrategy;

				_rootIdStr = root.Id.To<string>();
			}

			return _rootIdStr;
		}

		private void OnConnectorOrderRegisterFailed(OrderFail fail)
		{
			ProcessRegisterOrderFail(fail, OnOrderRegisterFailed);
		}

		private void OnConnectorValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			ValuesChanged?.Invoke(security, changes, serverTime, localTime);
		}

		private void UpdatePnLManager(Security security)
		{
			var msg = new Level1ChangeMessage { SecurityId = security.ToSecurityId(), ServerTime = CurrentTime }
					.TryAdd(Level1Fields.PriceStep, security.PriceStep)
					.TryAdd(Level1Fields.StepPrice, this.GetSecurityValue<decimal?>(security, Level1Fields.StepPrice) ?? security.StepPrice)
					.TryAdd(Level1Fields.Multiplier, this.GetSecurityValue<decimal?>(security, Level1Fields.Multiplier) ?? security.Multiplier);

			PnLManager.ProcessMessage(msg);
		}

		private void AddMyTrade(MyTrade trade)
		{
			if (!_myTrades.TryAdd(trade))
				return;

			if (WaitAllTrades)
			{
				lock (_ordersInfo.SyncRoot)
				{
					if (_ordersInfo.TryGetValue(trade.Order, out var info) && info.IsOwn)
						info.ReceivedVolume += trade.Trade.Volume;
				}
			}

			TryInvoke(() => OnNewMyTrade(trade));

			var isComChanged = false;
			var isPnLChanged = false;
			var isSlipChanged = false;

			this.AddInfoLog(LocalizedStrings.Str1398Params,
				trade.Order.Direction,
				(trade.Trade.Id == 0 ? trade.Trade.StringId : trade.Trade.Id.To<string>()),
				trade.Trade.Price, trade.Trade.Volume, trade.Order.TransactionId);

			if (trade.Commission != null)
			{
				if (Commission == null)
					Commission = 0;

				Commission += trade.Commission.Value;
				isComChanged = true;
			}

			UpdatePnLManager(trade.Trade.Security);

			var execMsg = trade.ToMessage();

			var tradeInfo = PnLManager.ProcessMessage(execMsg);
			if (tradeInfo != null)
			{
				if (tradeInfo.PnL != 0)
					isPnLChanged = true;

				StatisticManager.AddMyTrade(tradeInfo);
			}

			if (trade.Slippage != null)
			{
				if (Slippage == null)
					Slippage = 0;

				Slippage += trade.Slippage.Value;
				isSlipChanged = true;
			}

			TryInvoke(() =>
			{
				if (isComChanged)
					RaiseCommissionChanged();

				if (isPnLChanged)
					RaisePnLChanged();

				if (isSlipChanged)
					RaiseSlippageChanged();
			});

			ProcessRisk(execMsg);
		}

		private void RaiseSlippageChanged()
		{
			this.Notify(nameof(Slippage));
			SlippageChanged?.Invoke();

			RaiseNewStateMessage(nameof(Slippage), Slippage);
		}

		private void RaiseCommissionChanged()
		{
			this.Notify(nameof(Commission));
			CommissionChanged?.Invoke();

			RaiseNewStateMessage(nameof(Commission), Commission);
		}

		private void RaisePnLChanged()
		{
			this.Notify(nameof(PnL));
			PnLChanged?.Invoke();

			if (_pfSubscription != null)
				PnLReceived?.Invoke(_pfSubscription);

			StatisticManager.AddPnL(_lastPnlRefreshTime, PnL);

			RaiseNewStateMessage(nameof(PnL), PnL);
		}

		private void RaiseLatencyChanged()
		{
			this.Notify(nameof(Latency));
			LatencyChanged?.Invoke();

			RaiseNewStateMessage(nameof(Latency), Latency);
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
		/// To process orders, received for the connection <see cref="Strategy.Connector"/>, and find among them those, belonging to the strategy.
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

			if (parameters == null)
				return;

			//var dict = Parameters.SyncGet(c => c.ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase));

			// в настройках могут быть дополнительные параметры, которые будут добавлены позже
			foreach (var s in parameters)
			{
				var param = Parameters.TryGetValue(s.GetValue<string>(nameof(IStrategyParam.Name)));

				param?.Load(s);
			}

			var pnlStorage = storage.GetValue<SettingsStorage>(nameof(PnLManager));

			if (pnlStorage != null)
				PnLManager.Load(pnlStorage);

			var riskStorage = storage.GetValue<SettingsStorage>(nameof(RiskManager));

			if (riskStorage != null)
				RiskManager.Load(riskStorage);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Parameters), Parameters.CachedValues.Select(p => p.Save()).ToArray());

			storage.SetValue(nameof(PnLManager), PnLManager.Save());
			storage.SetValue(nameof(RiskManager), RiskManager.Save());
			//storage.SetValue(nameof(StatisticManager), StatisticManager.Save());
			//storage.SetValue(nameof(PositionManager), PositionManager.Save());
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
				this.AddWarningLog(LocalizedStrings.Str1400Params, ProcessState);
				return;
			}

			this.AddInfoLog(LocalizedStrings.Str1401);

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
					this.AddWarningLog(LocalizedStrings.Str1390Params, o.TransactionId);
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
			//SlippageManager.RegisterFailed(fail);
			TryInvoke(() => OrderRegisterFailed?.Invoke(fail));
		}

		private void OnChildOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => OrderCancelFailed?.Invoke(fail));
		}

		private void ProcessCancelOrderFail(OrderFail fail)
		{
			var order = fail.Order;

			lock (_ordersInfo.SyncRoot)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null || !info.IsOwn)
					return;

				info.IsCanceled = false;
			}

			this.AddErrorLog(LocalizedStrings.Str1402Params, order.TransactionId, fail.Error);

			OrderCancelFailed?.Invoke(fail);

			StatisticManager.AddFailedOrderCancel(fail);
		}

		private void ProcessRegisterOrderFail(OrderFail fail, Action<OrderFail> evt)
		{
			OrderInfo info;

			lock (_ordersInfo.SyncRoot)
			{
				info = _ordersInfo.TryGetValue(fail.Order);

				if (info == null)
					return;

				info.RegistrationFail = fail;
			}

			this.AddErrorLog(LocalizedStrings.Str1302Params, fail.Order.TransactionId, fail.Error.Message);
			//SlippageManager.RegisterFailed(fail);

			TryInvoke(() => evt?.Invoke(fail));

			if (info.IsOwn && MaxOrderRegisterErrorCount != -1)
			{
				OrderRegisterErrorCount++;

				this.AddInfoLog(LocalizedStrings.Str1297Params, OrderRegisterErrorCount, MaxOrderRegisterErrorCount);
			
				if (OrderRegisterErrorCount >= MaxOrderRegisterErrorCount)
					Stop();
			}
		}

		/// <summary>
		/// Processing of error, occurred as result of strategy operation.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="error">Error.</param>
		protected virtual void OnError(Strategy strategy, Exception error)
		{
			ErrorCount++;
			Error?.Invoke(strategy, error);

			if (!StopOnChildStrategyErrors && !Equals(this, strategy))
				return;

			this.AddErrorLog(error.ToString());

			if (ErrorCount >= MaxErrorCount)
				Stop();
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

			var id = clone.Id;
			clone.Load(this.Save());
			clone.Id = id;

			return clone;
		}

		private void TryInvoke(Action handler)
		{
			//_disposeLock.Read(() =>
			//{
				if (IsDisposed)
					return;

				handler();
			//});
		}

		private bool IsOwnOrder(Order order)
		{
			var info = _ordersInfo.TryGetValue(order);
			return info != null && info.IsOwn;
		}

		private void ProcessRisk(Message message)
		{
			foreach (var rule in RiskManager.ProcessRules(message))
			{
				this.AddWarningLog(LocalizedStrings.Str855Params,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);

				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
						this.ClosePosition();
						break;
					case RiskActions.StopTrading:
						Stop();
						break;
					case RiskActions.CancelOrders:
						CancelActiveOrders();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

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

		/// <summary>
		/// New <see cref="StrategyStateMessage"/> occurred event.
		/// </summary>
		public event Action<StrategyStateMessage> NewStateMessage;

		private void RaiseNewStateMessage<T>(string paramName, T value)
		{
			NewStateMessage?.Invoke(new StrategyStateMessage
			{
				StrategyId = Id,
				Statistics =
				{
					{ paramName, Tuple.Create(typeof(T).FullName, value?.ToString()) }
				}
			});
		}

		/// <summary>
		/// Convert to <see cref="StrategyInfoMessage"/>.
		/// </summary>
		/// <param name="transactionId">ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.</param>
		/// <returns>The message contains information about strategy.</returns>
		public virtual StrategyInfoMessage ToInfoMessage(long transactionId = 0)
		{
			var msg = new StrategyInfoMessage
			{
				StrategyId = Id,
				Name = Name,
				OriginalTransactionId = transactionId,
			};

			foreach (var parameter in Parameters)
			{
				msg.Parameters.Add(parameter.Key, Tuple.Create(parameter.Value.Value.GetType().FullName, parameter.Value.Value?.ToString()));
			}

			return msg;
		}

		/// <summary>
		/// Apply changes.
		/// </summary>
		/// <param name="message">The message contains information about strategy.</param>
		public virtual void ApplyChanges(StrategyInfoMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			foreach (var parameter in message.Parameters)
			{
				if (!Parameters.TryGetValue(parameter.Key, out var param))
				{
					this.AddWarningLog("Unknown parameter '{0}'.", parameter.Key);
					continue;
				}

				if (parameter.Value.Item2 == null)
					param.Value = null;
				else
				{
					param.Value = parameter.Value.Item1 == typeof(Unit).FullName
						? parameter.Value.Item2.ToUnit()
						: parameter.Value.Item2.To(parameter.Value.Item1.To<Type>());
				}
			}
		}

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
					var secId = parameters.TryGetValue(nameof(Order.Security))?.Item2;
					var pfName = parameters.TryGetValue(nameof(Order.Portfolio))?.Item2;
					var side = parameters[nameof(Order.Direction)].Item2.To<Sides>();
					var volume = parameters[nameof(Order.Volume)].Item2.To<decimal>();
					var price = parameters.TryGetValue(nameof(Order.Price))?.Item2.To<decimal?>() ?? 0;
					var comment = parameters.TryGetValue(nameof(Order.Comment))?.Item2;
					var clientCode = parameters.TryGetValue(nameof(Order.ClientCode))?.Item2;
					var tif = parameters.TryGetValue(nameof(Order.TimeInForce))?.Item2.To<TimeInForce?>();

					var order = new Order
					{
						Security = secId.IsEmpty() ? Security : this.LookupById(secId),
						Portfolio = pfName.IsEmpty() ? Portfolio : Connector.LookupByPortfolioName(pfName),
						Direction = side,
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
					var orderId = parameters[nameof(Order.Id)].Item2.To<long>();

					// TODO
#pragma warning disable 618
					CancelOrder(Orders.First(o => o.Id == orderId));
#pragma warning restore 618

					break;
				}

				case CommandTypes.ClosePosition:
				{
					var slippage = parameters.TryGetValue(nameof(Order.Slippage))?.Item2.To<decimal?>();
					
					this.ClosePosition(slippage ?? 0);
					
					break;
				}
			}
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
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			//_disposeLock.WriteAsync(() =>
			//{
			ChildStrategies.ForEach(s => s.Dispose());
			ChildStrategies.Clear();

			UnSubscribe(true);

			Connector = null;
			//});

			base.DisposeManaged();

			_strategyStat.Remove(this);
		}
	}
}