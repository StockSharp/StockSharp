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
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Positions;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Statistics;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The base class for all trade strategies.
	/// </summary>
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 10)]
	[CategoryOrderLoc(LocalizedStrings.Str436Key, 11)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 12)]
	public class Strategy : BaseLogReceiver, INotifyPropertyChangedEx, IMarketRuleContainer, ICloneable<Strategy>, IMarketDataProvider, ISecurityProvider
	{
		private static readonly MemoryStatisticsValue<Strategy> _strategyStat = new MemoryStatisticsValue<Strategy>(LocalizedStrings.Str1355);

		static Strategy()
		{
			MemoryStatistics.Instance.Values.Add(_strategyStat);
		}

		sealed class ChildStrategyList : SynchronizedSet<Strategy>, IStrategyChildStrategyList
		{
			private readonly SynchronizedDictionary<Strategy, IMarketRule> _childStrategyRules = new SynchronizedDictionary<Strategy, IMarketRule>();
			private readonly Strategy _parent;

			public ChildStrategyList(Strategy parent)
				: base(true)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}
			
			protected override void OnAdded(Strategy item)
			{
				//pyh: Нельзя использовать OnAdding тк логирование включается по событию Added которое вызовет base.OnAdded
				base.OnAdded(item);

				if (item.Parent != null)
					throw new ArgumentException(LocalizedStrings.Str1356);

				item.Parent = _parent;

				if (item.Connector == null)
					item.Connector = _parent.Connector;

				if (item.Portfolio == null)
					item.Portfolio = _parent.Portfolio;

				if (item.Security == null)
					item.Security = _parent.Security;

				item.OrderRegistering += _parent.OnOrderRegistering;
				item.OrderRegistered += _parent.ProcessOrder;
				//item.ReRegistering += _parent.ReRegisterSlippage;
				item.OrderChanged += _parent.OnChildOrderChanged;
				item.OrderRegisterFailed += _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed += _parent.OnChildOrderCancelFailed;
				item.StopOrderRegistering += _parent.OnStopOrderRegistering;
				item.StopOrderRegistered += _parent.ProcessOrder;
				item.StopOrderChanged += _parent.OnChildStopOrderChanged;
				item.StopOrderRegisterFailed += _parent.OnChildStopOrderRegisterFailed;
				item.StopOrderCancelFailed += _parent.OnChildStopOrderCancelFailed;
				item.NewMyTrades += _parent.AddMyTrades;
				item.OrderReRegistering += _parent.OnOrderReRegistering;
				item.StopOrderReRegistering += _parent.OnStopOrderReRegistering;
				item.ProcessStateChanged += OnChildProcessStateChanged;

				item.StopOrders.ForEach(_parent.ProcessOrder);
				item.Orders.ForEach(_parent.ProcessOrder);

				if (!item.MyTrades.IsEmpty())
					_parent.AddMyTrades(item.MyTrades);

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

					rule.UpdateName(rule.Name + " (ChildStrategyList.OnChildProcessStateChanged)");

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

				item.OrderRegistering -= _parent.OnOrderRegistering;
				item.OrderRegistered -= _parent.ProcessOrder;
				//item.ReRegistering -= _parent.ReRegisterSlippage;
				item.OrderChanged -= _parent.OnChildOrderChanged;
				item.OrderRegisterFailed -= _parent.OnChildOrderRegisterFailed;
				item.OrderCancelFailed -= _parent.OnChildOrderCancelFailed;
				item.OrderCanceling -= _parent.OnOrderCanceling;
				item.StopOrderRegistering -= _parent.OnStopOrderRegistering;
				item.StopOrderRegistered -= _parent.ProcessOrder;
				item.StopOrderChanged -= _parent.OnChildStopOrderChanged;
				item.StopOrderRegisterFailed -= _parent.OnChildStopOrderRegisterFailed;
				item.StopOrderCancelFailed -= _parent.OnChildStopOrderCancelFailed;
				item.StopOrderCanceling -= _parent.OnStopOrderCanceling;
				item.NewMyTrades -= _parent.AddMyTrades;
				item.OrderReRegistering -= _parent.OnOrderReRegistering;
				item.StopOrderReRegistering -= _parent.OnStopOrderReRegistering;
				item.ProcessStateChanged -= OnChildProcessStateChanged;

				_childStrategyRules.SyncDo(d =>
				{
					var rule = d.TryGetValue(item);

					if (rule == null)
						return;

					// правило могло быть удалено при остановке дочерней стратегии, но перед ее удалением из коллекции у родителя
					if (rule.IsReady)
						_parent.TryRemoveRule(rule);

					d.Remove(item);
				});

				return base.OnRemoving(item);
			}

			public void TryRemoveStoppedRule(IMarketRule rule)
			{
				var child = rule.Token as Strategy;

				if (child != null)
					_childStrategyRules.Remove(child);
			}
		}

		private sealed class StrategyRuleList : MarketRuleList
		{
			private readonly Strategy _strategy;

			public StrategyRuleList(Strategy strategy)
				: base(strategy)
			{
				if (strategy == null)
					throw new ArgumentNullException(nameof(strategy));

				_strategy = strategy;
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

		private DateTimeOffset _firstOrderTime;
		private DateTimeOffset _lastOrderTime;
		private TimeSpan _maxOrdersKeepTime;
		private DateTimeOffset _lastPnlRefreshTime;
		private DateTimeOffset _prevTradeDate;
		private bool _isPrevDateTradable;

		private string _idStr;

		/// <summary>
		/// Initializes a new instance of the <see cref="Strategy"/>.
		/// </summary>
		public Strategy()
		{
			_childStrategies = new ChildStrategyList(this);

			Rules = new StrategyRuleList(this);

			NameGenerator = new StrategyNameGenerator(this);
			NameGenerator.Changed += name => _name.Value = name;

			_id = this.Param("Id", base.Id);
			_volume = this.Param<decimal>("Volume", 1);
			_name = this.Param("Name", new string(GetType().Name.Where(char.IsUpper).ToArray()));
			_maxErrorCount = this.Param("MaxErrorCount", 1);
			_disposeOnStop = this.Param("DisposeOnStop", false);
			_cancelOrdersWhenStopping = this.Param("CancelOrdersWhenStopping", true);
			_waitAllTrades = this.Param<bool>("WaitAllTrades");
			_commentOrders = this.Param<bool>("CommentOrders");
			_ordersKeepTime = this.Param("OrdersKeepTime", TimeSpan.FromDays(1));
			_logLevel = this.Param("LogLevel", LogLevels.Inherit);

			InitMaxOrdersKeepTime();

			_strategyStat.Add(this);
		}

		private readonly StrategyParam<Guid> _id;

		/// <summary>
		/// The unique identifier of the source.
		/// </summary>
		public override Guid Id
		{
			get { return _id.Value; }
			set { _id.Value = value; }
		}

		private readonly StrategyParam<LogLevels> _logLevel;

		/// <summary>
		/// The logging level. The default is set to <see cref="LogLevels.Inherit"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.LoggingKey)]
		//[PropertyOrder(8)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str1358Key)]
		public override LogLevels LogLevel
		{
			get { return _logLevel.Value; }
			set { _logLevel.Value = value; }
		}

		private readonly StrategyParam<string> _name;

		/// <summary>
		/// Strategy name.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[PropertyOrder(0)]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str1359Key)]
		public override string Name
		{
			get { return _name.Value; }
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

		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system.
		/// </summary>
		[Browsable(false)]
		public virtual IConnector Connector
		{
			get { return _connector; }
			set
			{
				if (Connector == value)
					return;

				if (_connector != null)
				{
					_connector.NewOrders -= OnConnectorNewOrders;
					_connector.OrdersChanged -= OnConnectorOrdersChanged;
					_connector.OrdersRegisterFailed -= OnConnectorOrdersRegisterFailed;
					_connector.OrdersCancelFailed -= ProcessCancelOrderFails;
					_connector.NewStopOrders -= OnConnectorNewOrders;
					_connector.StopOrdersChanged -= OnConnectorStopOrdersChanged;
					_connector.StopOrdersRegisterFailed -= OnConnectorStopOrdersRegisterFailed;
					_connector.StopOrdersCancelFailed -= ProcessCancelOrderFails;
					_connector.NewMyTrades -= OnConnectorNewMyTrades;
					_connector.PositionsChanged -= OnConnectorPositionsChanged;
					_connector.NewMessage -= OnConnectorNewMessage;
					_connector.ValuesChanged -= OnConnectorValuesChanged;
					//_connector.NewTrades -= OnConnectorNewTrades;
					//_connector.MarketDepthsChanged -= OnConnectorMarketDepthsChanged;
				}

				_connector = value;

				if (_connector != null)
				{
					_connector.NewOrders += OnConnectorNewOrders;
					_connector.OrdersChanged += OnConnectorOrdersChanged;
					_connector.OrdersRegisterFailed += OnConnectorOrdersRegisterFailed;
					_connector.OrdersCancelFailed += ProcessCancelOrderFails;
					_connector.NewStopOrders += OnConnectorNewOrders;
					_connector.StopOrdersChanged += OnConnectorStopOrdersChanged;
					_connector.StopOrdersRegisterFailed += OnConnectorStopOrdersRegisterFailed;
					_connector.StopOrdersCancelFailed += ProcessCancelOrderFails;
					_connector.NewMyTrades += OnConnectorNewMyTrades;
					_connector.PositionsChanged += OnConnectorPositionsChanged;
					_connector.NewMessage += OnConnectorNewMessage;
					_connector.ValuesChanged += OnConnectorValuesChanged;
					//_connector.NewTrades += OnConnectorNewTrades;
					//_connector.MarketDepthsChanged += OnConnectorMarketDepthsChanged;
				}

				ChildStrategies.SyncDo(c =>
				{
					foreach (var strategy in c)
					{
						if (strategy.Connector == null || value == null)
							strategy.Connector = value;
					}
				});

				ConnectorChanged?.Invoke();
			}
		}

		/// <summary>
		/// To get the strategy getting <see cref="Strategy.Connector"/>. If it is not initialized, the exception will be discarded.
		/// </summary>
		/// <returns>Connection string.</returns>
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
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[PropertyOrder(1)]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str1361Key)]
		public virtual Portfolio Portfolio
		{
			get { return _portfolio; }
			set
			{
				if (_portfolio == value)
					return;

				_portfolio = value;

				ChildStrategies.SyncDo(c =>
				{
					foreach (var strategy in c)
					{
						if (strategy.Portfolio == null)
							strategy.Portfolio = value;
					}
				});

				RaiseParametersChanged("Portfolio");
				PortfolioChanged?.Invoke();
			}
		}

		private Security _security;

		/// <summary>
		/// Security.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[PropertyOrder(2)]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str1362Key)]
		public virtual Security Security
		{
			get { return _security; }
			set
			{
				if (_security == value)
					return;

				_security = value;

				ChildStrategies.SyncDo(c =>
				{
					foreach (var strategy in c)
					{
						if (strategy.Security == null)
							strategy.Security = value;
					}
				});
				
				RaiseParametersChanged("Security");
				SecurityChanged?.Invoke();
			}
		}

		/// <summary>
		/// Total slippage.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(4)]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str1363Key)]
		[ReadOnly(true)]
		public decimal? Slippage { get; private set; }

		/// <summary>
		/// <see cref="Strategy.Slippage"/> change event.
		/// </summary>
		public event Action SlippageChanged;

		private IPnLManager _pnLManager = new PnLManager();

		/// <summary>
		/// The profit-loss manager. It accounts trades of this strategy, as well as of its subsidiary strategies <see cref="Strategy.ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		public IPnLManager PnLManager
		{
			get { return _pnLManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_pnLManager = value;
			}
		}

		/// <summary>
		/// The aggregate value of profit-loss without accounting commission <see cref="Strategy.Commission"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(5)]
		[DisplayNameLoc(LocalizedStrings.PnLKey)]
		[DescriptionLoc(LocalizedStrings.Str1364Key)]
		[ReadOnly(true)]
		public decimal PnL => PnLManager.PnL;

		/// <summary>
		/// <see cref="Strategy.PnL"/> change event.
		/// </summary>
		public event Action PnLChanged;

		/// <summary>
		/// Total commission.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(6)]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str1365Key)]
		[ReadOnly(true)]
		public decimal? Commission { get; private set; }

		/// <summary>
		/// <see cref="Strategy.Commission"/> change event.
		/// </summary>
		public event Action CommissionChanged;

		private IPositionManager _positionManager = new PositionManager(true);

		/// <summary>
		/// The position manager. It accounts trades of this strategy, as well as of its subsidiary strategies <see cref="Strategy.ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		public IPositionManager PositionManager
		{
			get { return _positionManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_positionManager = value;
			}
		}

		/// <summary>
		/// The position aggregate value.
		/// </summary>
		[Browsable(false)]
		public decimal Position
		{
			get { return PositionManager.Position; }
			set
			{
				if (Position == value)
					return;

				PositionManager.Position = value;
				RaisePositionChanged();
			}
		}

		/// <summary>
		/// <see cref="Strategy.Position"/> change event.
		/// </summary>
		public event Action PositionChanged;

		/// <summary>
		/// Total latency.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(8)]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str1366Key)]
		[ReadOnly(true)]
		public TimeSpan? Latency { get; private set; }

		/// <summary>
		/// <see cref="Strategy.Latency"/> change event.
		/// </summary>
		public event Action LatencyChanged;

		private StatisticManager _statisticManager = new StatisticManager();

		/// <summary>
		/// The statistics manager.
		/// </summary>
		[Browsable(false)]
		public StatisticManager StatisticManager
		{
			get { return _statisticManager; }
			protected set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_statisticManager = value;
			}
		}

		private IRiskManager _riskManager = new RiskManager();

		/// <summary>
		/// The risks control manager.
		/// </summary>
		[Browsable(false)]
		public IRiskManager RiskManager
		{
			get { return _riskManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_riskManager = value;
			}
		}

		private readonly SynchronizedSet<IStrategyParam> _parameters = new SynchronizedSet<IStrategyParam>();

		/// <summary>
		/// Strategy parameters.
		/// </summary>
		[Browsable(false)]
		public ISynchronizedCollection<IStrategyParam> Parameters => _parameters;

		/// <summary>
		/// <see cref="Strategy.Parameters"/> change event.
		/// </summary>
		public event Action ParametersChanged;

		/// <summary>
		/// To call events <see cref="Strategy.ParametersChanged"/> and <see cref="Strategy.PropertyChanged"/>.
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
			get { return _maxErrorCount.Value; }
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
			get { return _errorCount; }
			private set
			{
				if (_errorCount == value)
					return;

				_errorCount = value;
				this.Notify("ErrorCount");
			}
		}

		private ProcessStates _processState;

		/// <summary>
		/// The operation state.
		/// </summary>
		[Browsable(false)]
		public virtual ProcessStates ProcessState
		{
			get { return _processState; }
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
					ChildStrategies.SyncDo(c =>
					{
						var child = (IEnumerable<Strategy>)c;

						if (ProcessState == ProcessStates.Stopping)
							child = child.Where(s => s.ProcessState == ProcessStates.Started);

						child.ToArray().ForEach(s => s.ProcessState = ProcessState);
					});

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
							StartedTime = default(DateTimeOffset);
							LogProcessState(value);
							OnStopped();
							break;
						}
					}
				}
				catch (Exception error)
				{
					OnError(error);
				}

				try
				{
					RaiseProcessStateChanged(this);
					this.Notify("ProcessState");
				}
				catch (Exception error)
				{
					OnError(error);
				}
				
				if (ProcessState == ProcessStates.Stopping)
				{
					if (CancelOrdersWhenStopping)
					{
						this.AddInfoLog(LocalizedStrings.Str1370);
						ProcessCancelActiveOrders();
					}

					foreach (var rule in Rules.SyncGet(c => c.ToArray()))
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
					throw new ArgumentOutOfRangeException(nameof(state));
			}

			this.AddInfoLog(LocalizedStrings.Str1374Params, stateStr, ChildStrategies.Count, Parent != null ? ParentStrategy.ChildStrategies.Count : -1, Position);
		}

		private Strategy ParentStrategy => (Strategy)Parent;

		/// <summary>
		/// <see cref="Strategy.ProcessState"/> change event.
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
			get { return _cancelOrdersWhenStopping.Value; }
			set { _cancelOrdersWhenStopping.Value = value; }
		}

		/// <summary>
		/// Orders, registered within the strategy framework.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<Order> Orders
		{
			get { return _ordersInfo.CachedKeys.Where(o => o.Type != OrderTypes.Conditional); }
		}

		/// <summary>
		/// Stop-orders, registered within the strategy framework.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<Order> StopOrders
		{
			get { return _ordersInfo.CachedKeys.Where(o => o.Type == OrderTypes.Conditional); }
		}

		private readonly StrategyParam<TimeSpan> _ordersKeepTime;

		/// <summary>
		/// The time for storing <see cref="Strategy.Orders"/> � <see cref="Strategy.StopOrders"/> orders in memory. By default it equals to 2 days. If value is set in <see cref="TimeSpan.Zero"/>, orders will not be deleted.
		/// </summary>
		[Browsable(false)]
		public TimeSpan OrdersKeepTime
		{
			get { return _ordersKeepTime.Value; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1375);

				_ordersKeepTime.Value = value;
				InitMaxOrdersKeepTime();
				RecycleOrders();
			}
		}

		private void InitMaxOrdersKeepTime()
		{
			_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		}

		private readonly CachedSynchronizedSet<MyTrade> _myTrades = new CachedSynchronizedSet<MyTrade> { ThrowIfDuplicate = true };

		/// <summary>
		/// Trades, matched during the strategy operation.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

		//private readonly CachedSynchronizedSet<OrderFail> _orderFails = new CachedSynchronizedSet<OrderFail> { ThrowIfDuplicate = true };

		/// <summary>
		/// Orders with errors, registered within the strategy.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<OrderFail> OrderFails
		{
			get { return _ordersInfo.CachedValues.Where(i => i.RegistrationFail != null).Select(i => i.RegistrationFail); }
		}

		private readonly StrategyParam<decimal> _volume;

		/// <summary>
		/// Operational volume.
		/// </summary>
		/// <remarks>
		/// If the value is set 0, the parameter is ignored.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[PropertyOrder(3)]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str1376Key)]
		public decimal Volume
		{
			get { return _volume.Value; }
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
			get { return _errorState; }
			private set
			{
				if (_errorState == value)
					return;

				_errorState = value;
				this.Notify("ErrorState");
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
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(3)]
		[DisplayNameLoc(LocalizedStrings.Str1378Key)]
		[DescriptionLoc(LocalizedStrings.Str1379Key)]
		[ReadOnly(true)]
		public DateTimeOffset StartedTime
		{
			get { return _startedTime; }
			private set
			{
				_startedTime = value;
				this.Notify("StartedTime");
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

				if (StartedTime != default(DateTimeOffset) && Connector != null)
					retVal += CurrentTime - StartedTime;

				return retVal;
			}
			private set
			{
				if (_totalWorkingTime == value)
					return;

				_totalWorkingTime = value;
				this.Notify("TotalWorkingTime");
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
			get { return _disposeOnStop.Value; }
			set { _disposeOnStop.Value = value; }
		}

		private readonly StrategyParam<bool> _waitAllTrades;

		/// <summary>
		/// Stop strategy only after getting all trades by registered trades.
		/// </summary>
		/// <remarks>
		/// It is disabled by default.
		/// </remarks>
		[Browsable(false)]
		public bool WaitAllTrades
		{
			get { return _waitAllTrades.Value; }
			set { _waitAllTrades.Value = value; }
		}

		private readonly StrategyParam<bool> _commentOrders;

		/// <summary>
		/// To add to <see cref="Order.Comment"/> the name of the strategy <see cref="Strategy.Name"/>, registering the order.
		/// </summary>
		/// <remarks>
		/// It is disabled by default.
		/// </remarks>
		[Browsable(false)]
		public bool CommentOrders
		{
			get { return _commentOrders.Value; }
			set { _commentOrders.Value = value; }
		}

		/// <summary>
		/// Registered rules.
		/// </summary>
		[Browsable(false)]
		public IMarketRuleList Rules { get; }

		private readonly object _rulesSuspendLock = new object();
		private int _rulesSuspendCount;

		/// <summary>
		/// Is rules execution suspended.
		/// </summary>
		/// <remarks>
		/// Rules suspension is performed through the method <see cref="Strategy.SuspendRules"/>.
		/// </remarks>
		[Browsable(false)]
		public bool IsRulesSuspended => _rulesSuspendCount > 0;

		/// <summary>
		/// The event of sending order for registration.
		/// </summary>
		public event Action<Order> OrderRegistering;

		/// <summary>
		/// The event of order successful registration.
		/// </summary>
		public event Action<Order> OrderRegistered;

		/// <summary>
		/// The event of order registration error.
		/// </summary>
		public event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// The event of sending stop-order for registration.
		/// </summary>
		public event Action<Order> StopOrderRegistering;

		/// <summary>
		/// The event of stop-order successful registration.
		/// </summary>
		public event Action<Order> StopOrderRegistered;

		/// <summary>
		/// The event of stop-order successful registration.
		/// </summary>
		public event Action<OrderFail> StopOrderRegisterFailed;

		/// <summary>
		/// The event of order change.
		/// </summary>
		public event Action<Order> OrderChanged;

		/// <summary>
		/// The event of stop-order change.
		/// </summary>
		public event Action<Order> StopOrderChanged;

		/// <summary>
		/// The event of sending order for cancelling.
		/// </summary>
		public event Action<Order> OrderCanceling;

		/// <summary>
		/// The event of sending stop-order for cancelling.
		/// </summary>
		public event Action<Order> StopOrderCanceling;

		/// <summary>
		/// The event of sending order for re-registration.
		/// </summary>
		public event Action<Order, Order> OrderReRegistering;

		/// <summary>
		/// The event of sending stop-order for re-registration.
		/// </summary>
		public event Action<Order, Order> StopOrderReRegistering;

		/// <summary>
		/// The event of order cancelling order.
		/// </summary>
		public event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// The event of stop-order cancelling order.
		/// </summary>
		public event Action<OrderFail> StopOrderCancelFailed;

		/// <summary>
		/// The event of new trades occurrence.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// The event of strategy connection change.
		/// </summary>
		public event Action ConnectorChanged;

		/// <summary>
		/// The event of strategy instrument change.
		/// </summary>
		public event Action SecurityChanged;

		/// <summary>
		/// The event of strategy portfolio change.
		/// </summary>
		public event Action PortfolioChanged;

		/// <summary>
		/// The event of strategy position change.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// The event of error occurrence in the strategy.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// The method is called when the <see cref="Strategy.Start"/> method has been called and the <see cref="Strategy.ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
		/// </summary>
		protected virtual void OnStarted()
		{
			if (Security == null)
				throw new InvalidOperationException(LocalizedStrings.Str1380);

			if (Portfolio == null)
				throw new InvalidOperationException(LocalizedStrings.Str1381);

			foreach (var parameter in Parameters)
			{
				var unit = parameter.Value as Unit;

				if (unit != null && unit.GetTypeValue == null && (unit.Type == UnitTypes.Point || unit.Type == UnitTypes.Step))
					unit.SetSecurity(Security);
			}

			//pyh: Это сильно замедляет тестирование + объясните зачем это нужно.

			//ProcessNewOrders(Trader.StopOrders, true).ForEach(AddOrder);
			//ProcessNewOrders(Trader.Orders, false).ForEach(AddOrder);

			ErrorCount = 0;
			ErrorState = LogLevels.Info;

			//_pnlUpdateByDepth = false;
			//_pnlUpdateByTrades = false;
		}

		/// <summary>
		/// The method is called when the <see cref="Strategy.ProcessState"/> process state has been taken the <see cref="ProcessStates.Stopping"/> value.
		/// </summary>
		protected virtual void OnStopping()
		{
		}

		/// <summary>
		/// The method is called when the <see cref="Strategy.ProcessState"/> process state has been taken the <see cref="ProcessStates.Stopped"/> value.
		/// </summary>
		protected virtual void OnStopped()
		{
		}

		/// <summary>
		/// To register the order and automatically add to start mechanism of profit-loss and slippage.
		/// </summary>
		/// <param name="order">Order.</param>
		public virtual void RegisterOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			this.AddInfoLog(LocalizedStrings.Str1382Params,
				order.Type, order.Direction, order.Price, order.Volume, order.Comment, order.GetHashCode());

			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.Str1383Params, ProcessState);
				return;
			}

			if (order.Security == null)
				order.Security = Security;

			if (order.Portfolio == null)
				order.Portfolio = Portfolio;

			AddOrder(order);

			if (CommentOrders)
			{
				if (order.Comment.IsEmpty())
					order.Comment = Name;
			}

			ProcessRegisterOrderAction(null, order, (oOrder, nOrder) =>
			{
				if (nOrder.Type == OrderTypes.Conditional)
					OnStopOrderRegistering(nOrder);
				else
					OnOrderRegistering(nOrder);

				SafeGetConnector().RegisterOrder(nOrder);
			});
		}

		/// <summary>
		/// To re-register the order and automatically add to start mechanism of profit-loss and slippage.
		/// </summary>
		/// <param name="oldOrder">Changing order.</param>
		/// <param name="newOrder">New order.</param>
		public virtual void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			this.AddInfoLog(LocalizedStrings.Str1384Params, oldOrder.GetTraceId(), oldOrder.Price, newOrder.Price, oldOrder.Comment);

			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.Str1385Params, ProcessState);
				return;
			}

			AddOrder(newOrder);

			ProcessRegisterOrderAction(oldOrder, newOrder, (oOrder, nOrder) =>
			{
				if (oOrder.Type == OrderTypes.Conditional)
					OnStopOrderReRegistering(oOrder, nOrder);
				else
					OnOrderReRegistering(oOrder, nOrder);

				//ReRegisterSlippage(oOrder, nOrder);

				SafeGetConnector().ReRegisterOrder(oOrder, nOrder);	
			});
		}

		private void AddOrder(Order order)
		{
			_ordersInfo.Add(order, new OrderInfo { IsOwn = true });

			if (order.State != OrderStates.Failed && order.State != OrderStates.Done)
				ApplyMonitorRules(order);

			ProcessRisk(order.CreateRegisterMessage(order.Security.ToSecurityId()));
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

				nOrder.State = nOrder.State.CheckModification(OrderStates.Failed);

				var fails = new[] { new OrderFail { Order = nOrder, Error = excp, ServerTime = CurrentTime } };

				if (nOrder.Type == OrderTypes.Conditional)
					OnConnectorStopOrdersRegisterFailed(fails);
				else
					OnConnectorOrdersRegisterFailed(fails);
			}
		}

		private void ApplyMonitorRules(Order order)
		{
			if (!CancelOrdersWhenStopping)
				return;

			IMarketRule matchedRule = order.WhenMatched(Connector);

			if (WaitAllTrades)
				matchedRule = matchedRule.And(order.WhenAllTrades(Connector));

			var successRule = order
				.WhenCanceled(Connector)
				.Or(matchedRule, order.WhenRegisterFailed(Connector))
				.Do(() => this.AddInfoLog(LocalizedStrings.Str1386Params.Put(order.GetTraceId())))
				.Until(() =>
				{
					if (order.State == OrderStates.Failed)
						return true;

					if (order.State != OrderStates.Done)
						return false;

					if (!WaitAllTrades)
						return true;

					var matchedVolume = order.GetMatchedVolume(Connector);

					if (matchedVolume == 0)
						return true;

					var info = _ordersInfo.TryGetValue(order);

					if (info == null)
						return false;

					return matchedVolume == info.ReceivedVolume;
				})
				.Apply(this);

			var canFinish = false;

			order
				.WhenCancelFailed(Connector)
				.Do(() =>
				{
					if (ProcessState != ProcessStates.Stopping)
						return;

					canFinish = true;
					this.AddInfoLog(LocalizedStrings.Str1387Params.Put(order.GetTraceId()));
				})
				.Until(() => canFinish)
				.Apply(this)
				.Exclusive(successRule);
		}

		/// <summary>
		/// Cancel order.
		/// </summary>
		/// <param name="order">The order for cancelling.</param>
		public virtual void CancelOrder(Order order)
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
					throw new ArgumentException(LocalizedStrings.Str1389Params.Put(order.GetTraceId(), Name));

				if (info.IsCanceled)
				{
					this.AddWarningLog(LocalizedStrings.Str1390Params, order.GetTraceId());
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

			this.AddInfoLog(LocalizedStrings.Str1315Params, order.GetTraceId());

			if (order.Type == OrderTypes.Conditional)
				OnStopOrderCanceling(order);
			else
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
			                   info != null && info.IsOwn && info.PrevState == OrderStates.None && order.State != OrderStates.Pending;

			if (info != null && info.IsOwn)
				info.PrevState = order.State;

			TryInvoke(() =>
			{
				if (isRegistered)
				{
					var isLatChanged = order.LatencyRegistration != null;

					if (isLatChanged)
						Latency += order.LatencyRegistration;

					if (order.Type == OrderTypes.Conditional)
					{
						//_stopOrders.Add(order);
						OnStopOrderRegistered(order);

						StatisticManager.AddNewOrder(order);
					}
					else
					{
						//_orders.Add(order);
						OnOrderRegistered(order);

						//SlippageManager.Registered(order);

						var pos = PositionManager.ProcessMessage(order.ToMessage());

						StatisticManager.AddNewOrder(order);

						if (order.Commission != null)
						{
							Commission += order.Commission;
							RaiseCommissionChanged();
						}

						if (pos != null)
							RaisePositionChanged();
					}

					if (_firstOrderTime == default(DateTimeOffset))
						_firstOrderTime = order.Time;

					_lastOrderTime = order.Time;

					RecycleOrders();

					if (isLatChanged)
						RaiseLatencyChanged();

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
					OnOrderChanged(order);

					var pos = PositionManager.ProcessMessage(order.ToMessage());

					if (pos != null)
						RaisePositionChanged();

					StatisticManager.AddChangedOrder(order);
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
		public virtual void AttachOrder(Order order, IEnumerable<MyTrade> myTrades)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (myTrades == null)
				throw new ArgumentNullException(nameof(myTrades));

			AttachOrder(order);

			OnConnectorNewMyTrades(myTrades);
		}

		private void AttachOrder(Order order)
		{
			AddOrder(order);

			if (order.Type != OrderTypes.Conditional)
				PositionManager.ProcessMessage(order.ToMessage());

			ProcessOrder(order);

			if (order.Type == OrderTypes.Conditional)
			{
				OnStopOrderRegistering(order);
			}
			else
			{
				OnOrderRegistering(order);
			}
		}

		/// <summary>
		/// To set the strategy identifier for the order.
		/// </summary>
		/// <param name="order">The order, for which the strategy identifier shall be set.</param>
		protected virtual void AssignOrderStrategyId(Order order)
		{
			order.UserOrderId = Id.To<string>();
		}

		private void RecycleOrders()
		{
			if (OrdersKeepTime == TimeSpan.Zero)
				return;

			var diff = _lastOrderTime - _firstOrderTime;

			if (diff <= _maxOrdersKeepTime)
				return;

			_firstOrderTime = _lastOrderTime - OrdersKeepTime;

			_ordersInfo.SyncDo(d => d.RemoveWhere(o => o.Key.State == OrderStates.Done && o.Key.Time < _firstOrderTime));
		}

		/// <summary>
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime => Connector == null ? TimeHelper.NowWithOffset : Connector.CurrentTime;

		/// <summary>
		/// To call the event <see cref="ILogSource.Log"/>.
		/// </summary>
		/// <param name="message">A debug message.</param>
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
			if (ProcessState == ProcessStates.Stopped)
				ProcessState = ProcessStates.Started;
			else 
				this.AddDebugLog(LocalizedStrings.Str1391Params, ProcessState);
		}

		/// <summary>
		/// To stop the trade algorithm.
		/// </summary>
		public virtual void Stop()
		{
			if (ProcessState == ProcessStates.Started)
				ProcessState = ProcessStates.Stopping;
			else
				this.AddDebugLog(LocalizedStrings.Str1392Params, ProcessState);
		}

		/// <summary>
		/// The event of the strategy re-initialization.
		/// </summary>
		public event Action Reseted;

		/// <summary>
		/// To call the event <see cref="Strategy.Reseted"/>.
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
			//	throw new InvalidOperationException("Инструмент не инициализирован.");

			//if (Portfolio == null)
			//	throw new InvalidOperationException("Портфель не инициализирован.");

			ChildStrategies.SyncDo(c => c.ForEach(s => s.Reset()));

			StatisticManager.Reset();

			PnLManager.Reset();
			
			Commission = null;
			//CommissionManager.Reset();

			Latency = null;
			//LatencyManager.Reset();

			PositionManager.Reset();

			Slippage = null;
			//SlippageManager.Reset();

			RiskManager.Reset();

			_myTrades.Clear();
			_ordersInfo.Clear();

			ProcessState = ProcessStates.Stopped;
			ErrorState = LogLevels.Info;
			ErrorCount = 0;

			_firstOrderTime = _lastOrderTime = _lastPnlRefreshTime = _prevTradeDate = default(DateTimeOffset);
			_idStr = null;

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
		/// It is called from the <see cref="Strategy.Reset"/> method.
		/// </summary>
		protected virtual void OnReseted()
		{
			RaiseReseted();
		}

		/// <summary>
		/// To suspend rules execution until next restoration through the method <see cref="Strategy.ResumeRules"/>.
		/// </summary>
		public virtual void SuspendRules()
		{
			lock (_rulesSuspendLock)
				_rulesSuspendCount++;

			this.AddDebugLog(LocalizedStrings.Str1394Params, _rulesSuspendCount);
		}

		/// <summary>
		/// To restore rules execution, suspended through the method <see cref="Strategy.SuspendRules"/>.
		/// </summary>
		public virtual void ResumeRules()
		{
			lock (_rulesSuspendLock)
			{
				if (_rulesSuspendCount > 0)
					_rulesSuspendCount--;
			}

			this.AddDebugLog(LocalizedStrings.Str1395Params, _rulesSuspendCount);
		}

		private void TryFinalStop()
		{
			if (!Rules.IsEmpty())
			{
				this.AddLog(LogLevels.Debug,
					() => LocalizedStrings.Str1396Params
						.Put(Rules.Count, Rules.SyncGet(c => c.Select(r => r.Name).Join(", "))));

				return;
			}

			ProcessState = ProcessStates.Stopped;

			if (DisposeOnStop)
			{
				//Trace.WriteLine(Name+" strategy-dispose-on-stop");

				if (Parent != null)
					ParentStrategy.ChildStrategies.Remove(this);

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
				OnError(error);
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
			get { return _unrealizedPnLInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_unrealizedPnLInterval = value;
			}
		}

		/// <summary>
		/// The method, called at strategy positions change.
		/// </summary>
		/// <param name="positions">The strategy positions change.</param>
		protected virtual void OnPositionsChanged(IEnumerable<Position> positions)
		{
			PositionsChanged?.Invoke(positions);
		}

		/// <summary>
		/// The method, called at occurrence of new strategy trades.
		/// </summary>
		/// <param name="trades">New trades of a strategy.</param>
		protected virtual void OnNewMyTrades(IEnumerable<MyTrade> trades)
		{
			NewMyTrades?.Invoke(trades);
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
		/// To call the event <see cref="Strategy.OrderRegistered"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderRegistered(Order order)
		{
			OrderRegistered?.Invoke(order);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.StopOrderRegistering"/>.
		/// </summary>
		/// <param name="order">The stop order.</param>
		protected virtual void OnStopOrderRegistering(Order order)
		{
			StopOrderRegistering?.Invoke(order);
			//SlippageManager.Registering(order);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.StopOrderRegistered"/>.
		/// </summary>
		/// <param name="order">The stop order.</param>
		protected virtual void OnStopOrderRegistered(Order order)
		{
			StopOrderRegistered?.Invoke(order);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.StopOrderRegistered"/>.
		/// </summary>
		/// <param name="order">The stop order.</param>
		protected virtual void OnStopOrderCanceling(Order order)
		{
			StopOrderCanceling?.Invoke(order);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.OrderRegistered"/>.
		/// </summary>
		/// <param name="order">Order.</param>
		protected virtual void OnOrderCanceling(Order order)
		{
			OrderCanceling?.Invoke(order);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.StopOrderReRegistering"/>.
		/// </summary>
		/// <param name="oldOrder">The stop order to be cancelled.</param>
		/// <param name="newOrder">New stop order to be registered.</param>
		protected virtual void OnStopOrderReRegistering(Order oldOrder, Order newOrder)
		{
			StopOrderReRegistering?.Invoke(oldOrder, newOrder);
		}

		/// <summary>
		/// To call the event <see cref="Strategy.OrderReRegistering"/>.
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

			if (order.LatencyCancellation != null)
			{
				Latency += order.LatencyCancellation;
				RaiseLatencyChanged();
			}
		}
	
		/// <summary>
		/// The method, called at strategy stop order change.
		/// </summary>
		/// <param name="order">The changed stop order.</param>
		protected virtual void OnStopOrderChanged(Order order)
		{
			StopOrderChanged?.Invoke(order);
			StatisticManager.AddChangedOrder(order);
		}

		/// <summary>
		/// The method, called at strategy stop orders change.
		/// </summary>
		/// <param name="orders">Changed stop orders.</param>
		protected virtual void OnStopOrdersChanged(IEnumerable<Order> orders)
		{
			foreach (var order in orders)
			{
				OnStopOrderChanged(order);

				if (order.DerivedOrder == null)
					continue;

				lock (_ordersInfo.SyncRoot)
				{
					var derivedOrder = order.DerivedOrder;

					if (_ordersInfo.ContainsKey(derivedOrder))
						continue;

					AssignOrderStrategyId(derivedOrder);
					_ordersInfo.Add(derivedOrder, new OrderInfo { IsOwn = true });
					ProcessOrder(derivedOrder);

					//заявка могла придти позже сделок по ней
					OnConnectorNewMyTrades(SafeGetConnector().MyTrades.Where(t => t.Order == derivedOrder));
				}
			}
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

		/// <summary>
		/// The method, called at strategy stop order registration error.
		/// </summary>
		/// <param name="fail">The stop order registration error.</param>
		protected virtual void OnStopOrderRegisterFailed(OrderFail fail)
		{
			StopOrderRegisterFailed?.Invoke(fail);
			StatisticManager.AddRegisterFailedOrder(fail);
		}

		private void TryAddChildOrder(Order order)
		{
			lock (_ordersInfo)
			{
				var info = _ordersInfo.TryGetValue(order);

				if (info == null)
					_ordersInfo.Add(order, new OrderInfo { IsOwn = false });
			}

			AssignOrderStrategyId(order);
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

					if (execMsg.ExecutionType == ExecutionTypes.Tick || execMsg.HasTradeInfo())
						PnLManager.ProcessMessage(execMsg);

					msgTime = execMsg.ServerTime;
					break;
				}

				case MessageTypes.Time:
					break;

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

			if (PositionManager.Positions.Any())
				RaisePnLChanged();
		}

		private void OnConnectorPositionsChanged(IEnumerable<Position> positions)
		{
			Position[] changedPositions;

			var basketPortfolio = Portfolio as BasketPortfolio;
			if (basketPortfolio != null)
			{
				var innerPfs = basketPortfolio.InnerPortfolios;
				changedPositions = positions.Where(pos => innerPfs.Contains(pos.Portfolio)).ToArray();
			}
			else
			{
				changedPositions = positions.Where(p => p.Portfolio == Portfolio).ToArray();
			}

			if (changedPositions.Length > 0)
				TryInvoke(() => OnPositionsChanged(changedPositions));
		}

		private void OnConnectorNewMyTrades(IEnumerable<MyTrade> trades)
		{
			trades = trades.Where(t => IsOwnOrder(t.Order)).ToArray();

			if (trades.IsEmpty())
				return;
			
			AddMyTrades(trades);
		}

		private void OnConnectorNewOrders(IEnumerable<Order> orders)
		{
			if (_idStr == null)
				_idStr = Id.ToString();

			orders = orders.Where(o => !_ordersInfo.ContainsKey(o) && o.UserOrderId == _idStr).ToArray();

			if (orders.IsEmpty())
				return;

			foreach (var order in orders)
			{
				AttachOrder(order);
			}
		}

		private void OnConnectorOrdersChanged(IEnumerable<Order> orders)
		{
			orders = orders.Where(IsOwnOrder).ToArray();

			if (orders.IsEmpty())
				return;

			TryInvoke(() =>
			{
				foreach (var order in orders)
				{
					ProcessOrder(order, true);
				}
			});
		}

		private void OnConnectorStopOrdersChanged(IEnumerable<Order> orders)
		{
			orders = orders.Where(IsOwnOrder).ToArray();

			if (!orders.IsEmpty())
				TryInvoke(() => OnStopOrdersChanged(orders));
		}

		private void OnConnectorOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			ProcessRegisterOrderFails(fails, OnOrderRegisterFailed);
		}

		private void OnConnectorStopOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			ProcessRegisterOrderFails(fails, OnStopOrderRegisterFailed);
		}

		private void OnConnectorValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			ValuesChanged?.Invoke(security, changes, serverTime, localTime);
		}

		private void UpdatePnLManager(Security security)
		{
			var msg = new Level1ChangeMessage { SecurityId = security.ToSecurityId(), ServerTime = CurrentTime }
					.TryAdd(Level1Fields.PriceStep, security.PriceStep)
					.TryAdd(Level1Fields.StepPrice, this.GetSecurityValue<decimal?>(security, Level1Fields.StepPrice));

			PnLManager.ProcessMessage(msg);
		}

		private void AddMyTrades(IEnumerable<MyTrade> trades)
		{
			var filteredTrades = _myTrades.SyncGet(set =>
			{
				var newTrades = trades.Where(t => !set.Contains(t)).ToArray();
				set.AddRange(newTrades);
				return newTrades;
			});

			if (filteredTrades.IsEmpty())
				return;

			if (WaitAllTrades)
			{
				foreach (var trade in filteredTrades)
				{
					lock (_ordersInfo.SyncRoot)
					{
						var info = _ordersInfo.TryGetValue(trade.Order);
						if (info == null || !info.IsOwn)
							continue;

						info.ReceivedVolume += trade.Trade.Volume;
					}
				}
			}

			TryInvoke(() => OnNewMyTrades(filteredTrades));

			var isComChanged = false;
			var isPnLChanged = false;
			decimal? pos = null;
			var isSlipChanged = false;

			foreach (var trade in filteredTrades)
			{
				this.AddInfoLog(LocalizedStrings.Str1398Params,
					trade.Order.Direction,
					(trade.Trade.Id == 0 ? trade.Trade.StringId : trade.Trade.Id.To<string>()),
					trade.Trade.Price, trade.Trade.Volume, trade.Order.TransactionId);

				if (trade.Commission != null)
				{
					Commission = trade.Commission;
					isComChanged = true;
				}

				UpdatePnLManager(trade.Trade.Security);

				var tradeInfo = PnLManager.ProcessMessage(trade.ToMessage());
				if (tradeInfo.PnL != 0)
					isPnLChanged = true;

				pos = PositionManager.ProcessMessage(trade.ToMessage());

				if (trade.Slippage != null)
				{
					Slippage += trade.Slippage;
					isSlipChanged = true;
				}

				StatisticManager.AddMyTrade(tradeInfo);
			}

			TryInvoke(() =>
			{
				if (isComChanged)
					RaiseCommissionChanged();

				if (isPnLChanged)
					RaisePnLChanged();

				if (pos != null)
					RaisePositionChanged();

				if (isSlipChanged)
					RaiseSlippageChanged();
			});

			foreach (var trade in filteredTrades)
			{
				ProcessRisk(trade.ToMessage());
			}
		}

		private void RaiseSlippageChanged()
		{
			this.Notify("Slippage");
			SlippageChanged?.Invoke();
		}

		private void RaisePositionChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str1399Params, PositionManager.Positions.Select(pos => pos.Key + "=" + pos.Value).Join(", "));

			this.Notify("Position");
			PositionChanged?.Invoke();

			StatisticManager.AddPosition(CurrentTime, Position);
		}

		private void RaiseCommissionChanged()
		{
			this.Notify("Commission");
			CommissionChanged?.Invoke();
		}

		private void RaisePnLChanged()
		{
			this.Notify("PnL");
			PnLChanged?.Invoke();

			StatisticManager.AddPnL(_lastPnlRefreshTime, PnL);
		}

		private void RaiseLatencyChanged()
		{
			this.Notify("Latency");
			LatencyChanged?.Invoke();
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

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			var parameters = storage.GetValue<SettingsStorage[]>("Parameters");

			if (parameters == null)
				return;

			var dict = Parameters.SyncGet(c => c.ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase));

			// в настройках могут быть дополнительные параметры, которые будут добавлены позже
			foreach (var s in parameters)
			{
				var param = dict.TryGetValue(s.GetValue<string>("Name"));

				if (param == null)
					continue;

				param.Load(s);
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Parameters", Parameters.SyncGet(c => c.Select(p => p.Save()).ToArray()));
		}

		/// <summary>
		/// The event of strategy parameters change.
		/// </summary>
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
					this.AddWarningLog(LocalizedStrings.Str1390Params, o.GetTraceId());
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

		private void OnChildStopOrderChanged(Order order)
		{
			TryInvoke(() => StopOrderChanged?.Invoke(order));
		}

		private void OnChildOrderRegisterFailed(OrderFail fail)
		{
			//SlippageManager.RegisterFailed(fail);
			TryInvoke(() => OrderRegisterFailed?.Invoke(fail));
		}

		private void OnChildStopOrderRegisterFailed(OrderFail fail)
		{
			//SlippageManager.RegisterFailed(fail);
			TryInvoke(() => StopOrderRegisterFailed?.Invoke(fail));
		}

		private void OnChildOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => OrderCancelFailed?.Invoke(fail));
		}

		private void OnChildStopOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => StopOrderCancelFailed?.Invoke(fail));
		}

		private void ProcessCancelOrderFails(IEnumerable<OrderFail> fails)
		{
			foreach (var fail in fails)
			{
				var order = fail.Order;

				lock (_ordersInfo.SyncRoot)
				{
					var info = _ordersInfo.TryGetValue(order);

					if (info == null || !info.IsOwn)
						continue;

					info.IsCanceled = false;
				}

				this.AddErrorLog(LocalizedStrings.Str1402Params, order.GetTraceId(), fail.Error);

				if (order.Type == OrderTypes.Conditional)
					StopOrderCancelFailed?.Invoke(fail);
				else
					OrderCancelFailed?.Invoke(fail);

				StatisticManager.AddFailedOrderCancel(fail);
			}
		}

		private void ProcessRegisterOrderFails(IEnumerable<OrderFail> fails, Action<OrderFail> evt)
		{
			var failedOrders = new List<OrderFail>();

			lock (_ordersInfo.SyncRoot)
			{
				foreach (var fail in fails)
				{
					var info = _ordersInfo.TryGetValue(fail.Order);

					if (info == null)
						continue;

					info.RegistrationFail = fail;
					failedOrders.Add(fail);
				}
			}

			foreach (var fail in failedOrders)
			{
				this.AddErrorLog(LocalizedStrings.Str1302Params, fail.Order.GetTraceId(), fail.Error.Message);
				//SlippageManager.RegisterFailed(fail);
			}

			TryInvoke(() => failedOrders.ForEach(evt));
		}

		/// <summary>
		/// Processing of error, occurred as result of strategy operation.
		/// </summary>
		/// <param name="error">Error.</param>
		protected virtual void OnError(Exception error)
		{
			ErrorCount++;
			Error?.Invoke(error);

			this.AddErrorLog(error.ToString());

			if (ErrorCount >= MaxErrorCount)
				Stop();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Create a copy of <see cref="Strategy"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual Strategy Clone()
		{
			var clone = GetType().CreateInstance<Strategy>();
			clone.Connector = Connector;
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

		/// <summary>
		/// Security changed.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			return SafeGetConnector().GetMarketDepth(security);
		}

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return SafeGetConnector().GetSecurityValue(security, field);
		}

		/// <summary>
		/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Possible fields.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return SafeGetConnector().GetLevel1Fields(security);
		}

		int ISecurityProvider.Count => SafeGetConnector().Count;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add { SafeGetConnector().Added += value; }
			remove { SafeGetConnector().Added -= value; }
		}

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add { SafeGetConnector().Removed += value; }
			remove { SafeGetConnector().Removed -= value; }
		}

		event Action ISecurityProvider.Cleared
		{
			add { SafeGetConnector().Cleared += value; }
			remove { SafeGetConnector().Cleared -= value; }
		}

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			return SafeGetConnector().Lookup(criteria);
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return SafeGetConnector().GetNativeId(security);
		}

		//private bool IsChildOrder(Order order)
		//{
		//	var info = _ordersInfo.TryGetValue(order);
		//	return info != null && !info.IsOwn;
		//}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			//_disposeLock.WriteAsync(() =>
			//{
				ChildStrategies.SyncDo(c =>
				{
					c.ForEach(s => s.Dispose());
					c.Clear();
				});

				Connector = null;
			//});

			base.DisposeManaged();

			_strategyStat.Remove(this);
		}
	}
}