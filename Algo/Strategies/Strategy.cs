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
	/// Базовый класс для всех торговых стратегий.
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
					throw new ArgumentNullException("parent");

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
				item.Parent = null;

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
					throw new ArgumentNullException("strategy");

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
		private readonly StrategyNameGenerator _nameGenerator;

		private DateTimeOffset _firstOrderTime;
		private DateTimeOffset _lastOrderTime;
		private TimeSpan _maxOrdersKeepTime;
		private DateTimeOffset _lastPnlRefreshTime;
		private DateTimeOffset _prevTradeDate;
		private bool _isPrevDateTradable;

		private string _idStr;

		/// <summary>
		/// Создать <see cref="Strategy"/>.
		/// </summary>
		public Strategy()
		{
			_childStrategies = new ChildStrategyList(this);

			Rules = new StrategyRuleList(this);

			_nameGenerator = new StrategyNameGenerator(this);
			_nameGenerator.Changed += name => _name.Value = name;

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
		/// Уникальный идентификатор источника.
		/// </summary>
		public override Guid Id
		{
			get { return _id.Value; }
			set { _id.Value = value; }
		}

		private readonly StrategyParam<LogLevels> _logLevel;

		/// <summary>
		/// Уровень логирования. По-умолчанию установлено в <see cref="LogLevels.Inherit"/>.
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
		/// Название стратегии.
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

				_nameGenerator.Value = value;
				_name.Value = value;
			}
		}

		/// <summary>
		/// Генератор имени стратегии.
		/// </summary>
		[Browsable(false)]
		public StrategyNameGenerator NameGenerator { get { return _nameGenerator; } }

		private IConnector _connector;

		/// <summary>
		/// Подключение к торговой системе.
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

				ConnectorChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Получить получение стратегии <see cref="Connector"/>. Если оно не инициализивано, то будет выбрашено исключение.
		/// </summary>
		/// <returns>Подключение.</returns>
		public IConnector SafeGetConnector()
		{
			var connector = Connector;

			if (connector == null)
				throw new InvalidOperationException(LocalizedStrings.Str1360);

			return connector;
		}

		private Portfolio _portfolio;

		/// <summary>
		/// Портфель.
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
				PortfolioChanged.SafeInvoke();
			}
		}

		private Security _security;

		/// <summary>
		/// Инструмент.
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
				SecurityChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Суммарное значение проскальзывания.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(4)]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str1363Key)]
		[ReadOnly(true)]
		public decimal? Slippage { get; private set; }

		/// <summary>
		/// Событие изменения <see cref="Slippage"/>.
		/// </summary>
		public event Action SlippageChanged;

		private IPnLManager _pnLManager = new PnLManager();

		/// <summary>
		/// Менеджер прибыли-убытка. Учитывает сделки данной стратегии, а так же ее дочерних стратегий <see cref="ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		public IPnLManager PnLManager
		{
			get { return _pnLManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_pnLManager = value;
			}
		}

		/// <summary>
		/// Суммарное значение прибыли-убытка без учета комиссии <see cref="Commission"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(5)]
		[DisplayName("P&L")]
		[DescriptionLoc(LocalizedStrings.Str1364Key)]
		[ReadOnly(true)]
		public decimal PnL
		{
			get { return PnLManager.PnL; }
		}

		/// <summary>
		/// Событие изменения <see cref="PnL"/>.
		/// </summary>
		public event Action PnLChanged;

		/// <summary>
		/// Суммарное значение комиссии.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(6)]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str1365Key)]
		[ReadOnly(true)]
		public decimal? Commission { get; private set; }

		/// <summary>
		/// Событие изменения <see cref="Commission"/>.
		/// </summary>
		public event Action CommissionChanged;

		private IPositionManager _positionManager = new PositionManager(true);

		/// <summary>
		/// Менеджер позиции. Учитывает сделки данной стратегии, а так же ее дочерних стратегий <see cref="ChildStrategies"/>.
		/// </summary>
		[Browsable(false)]
		public IPositionManager PositionManager
		{
			get { return _positionManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_positionManager = value;
			}
		}

		/// <summary>
		/// Суммарное значение позиции.
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
		/// Событие изменения <see cref="Position"/>.
		/// </summary>
		public event Action PositionChanged;

		/// <summary>
		/// Суммарное значение задержки.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(8)]
		[DisplayNameLoc(LocalizedStrings.Str161Key)]
		[DescriptionLoc(LocalizedStrings.Str1366Key)]
		[ReadOnly(true)]
		public TimeSpan? Latency { get; private set; }

		/// <summary>
		/// Событие изменения <see cref="Latency"/>.
		/// </summary>
		public event Action LatencyChanged;

		private StatisticManager _statisticManager = new StatisticManager();

		/// <summary>
		/// Менеджер статистики.
		/// </summary>
		[Browsable(false)]
		public StatisticManager StatisticManager
		{
			get { return _statisticManager; }
			protected set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_statisticManager = value;
			}
		}

		private IRiskManager _riskManager = new RiskManager();

		/// <summary>
		/// Менеджер контроля рисков.
		/// </summary>
		[Browsable(false)]
		public IRiskManager RiskManager
		{
			get { return _riskManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_riskManager = value;
			}
		}

		private readonly SynchronizedSet<IStrategyParam> _parameters = new SynchronizedSet<IStrategyParam>();

		/// <summary>
		/// Параметры стратегии.
		/// </summary>
		[Browsable(false)]
		public ISynchronizedCollection<IStrategyParam> Parameters
		{
			get { return _parameters; }
		}

		/// <summary>
		/// Событие изменения <see cref="Parameters"/>.
		/// </summary>
		public event Action ParametersChanged;

		/// <summary>
		/// Вызвать события <see cref="ParametersChanged"/> и <see cref="PropertyChanged"/>.
		/// </summary>
		/// <param name="name">Название параметра.</param>
		protected internal void RaiseParametersChanged(string name)
		{
			ParametersChanged.SafeInvoke();
			this.Notify(name);
		}

		private readonly SettingsStorage _environment = new SettingsStorage();

		/// <summary>
		/// Параметры окружения стратегии. 
		/// </summary>
		[Browsable(false)]
		public SettingsStorage Environment
		{
			get { return _environment; }
		}

		private readonly StrategyParam<int> _maxErrorCount;

		/// <summary>
		/// Максимальное количество ошибок, которое должна получить стратегия прежде, чем она остановил работу.
		/// </summary>
		/// <remarks>Значение по умолчанию равно 1.</remarks>
		[Browsable(false)]
		public int MaxErrorCount
		{
			get { return _maxErrorCount.Value; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1367);

				_maxErrorCount.Value = value;
			}
		}

		private int _errorCount;

		/// <summary>
		/// Текущее количество ошибок.
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
		/// Состояние работы.
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

				ChildStrategies.SyncDo(c =>
				{
					var child = (IEnumerable<Strategy>)c;

					if (ProcessState == ProcessStates.Stopping)
						child = child.Where(s => s.ProcessState == ProcessStates.Started);

					child.ToArray().ForEach(s => s.ProcessState = ProcessState);
				});

				try
				{
					switch (value)
					{
						case ProcessStates.Started:
						{
							StartedTime = CurrentTime.LocalDateTime;
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
							StartedTime = default(DateTime);
							LogProcessState(value);
							OnStopped();
							break;
						}
					}

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
					throw new ArgumentOutOfRangeException("state");
			}

			this.AddInfoLog(LocalizedStrings.Str1374Params, stateStr, ChildStrategies.Count, Parent != null ? ParentStrategy.ChildStrategies.Count : -1, Position);
		}

		private Strategy ParentStrategy
		{
			get { return (Strategy)Parent; }
		}

		/// <summary>
		/// Событие изменения <see cref="ProcessState"/>.
		/// </summary>
		public event Action<Strategy> ProcessStateChanged;

		/// <summary>
		/// Вызвать событие <see cref="ProcessStateChanged"/>.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		protected void RaiseProcessStateChanged(Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			ProcessStateChanged.SafeInvoke(strategy);
		}

		private readonly StrategyParam<bool> _cancelOrdersWhenStopping;

		/// <summary>
		/// Снимать активные заявки при остановке. По-умолчанию включено.
		/// </summary>
		[Browsable(false)]
		public virtual bool CancelOrdersWhenStopping
		{
			get { return _cancelOrdersWhenStopping.Value; }
			set { _cancelOrdersWhenStopping.Value = value; }
		}

		/// <summary>
		/// Заявки, зарегистрированные в рамках стратегии.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<Order> Orders
		{
			get { return _ordersInfo.CachedKeys.Where(o => o.Type != OrderTypes.Conditional); }
		}

		/// <summary>
		/// Стоп-заявки, зарегистрированные в рамках стратегии.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<Order> StopOrders
		{
			get { return _ordersInfo.CachedKeys.Where(o => o.Type == OrderTypes.Conditional); }
		}

		private readonly StrategyParam<TimeSpan> _ordersKeepTime;

		/// <summary>
		/// Время хранения заявок <see cref="Orders"/> и <see cref="StopOrders"/> в памяти.
		/// По-умолчанию равно 2-ум дням. Если значение установлено в <see cref="TimeSpan.Zero"/>, то заявки не будут удаляться.
		/// </summary>
		[Browsable(false)]
		public TimeSpan OrdersKeepTime
		{
			get { return _ordersKeepTime.Value; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1375);

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
		/// Сделки, прошедшие в течении работы стратегии.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<MyTrade> MyTrades
		{
			get { return _myTrades.Cache; }
		}

		//private readonly CachedSynchronizedSet<OrderFail> _orderFails = new CachedSynchronizedSet<OrderFail> { ThrowIfDuplicate = true };

		/// <summary>
		/// Заявки с ошибками, зарегистрированные в рамках стратегии.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<OrderFail> OrderFails
		{
			get { return _ordersInfo.CachedValues.Where(i => i.RegistrationFail != null).Select(i => i.RegistrationFail); }
		}

		private readonly StrategyParam<decimal> _volume;

		/// <summary>
		/// Объем, которым необходимо оперировать.
		/// </summary>
		/// <remarks>
		/// Если значение установлено в 0, то параметр игнорируется.
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
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1377);

				_volume.Value = value;
			}
		}

		private LogLevels _errorState;

		/// <summary>
		/// Состояние ошибки.
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
		/// Дочерние торговые стратегии.
		/// </summary>
		[Browsable(false)]
		public IStrategyChildStrategyList ChildStrategies
		{
			get { return _childStrategies; }
		}

		private DateTime _startedTime;

		/// <summary>
		/// Время запуска стратегии.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str436Key)]
		[PropertyOrder(3)]
		[DisplayNameLoc(LocalizedStrings.Str1378Key)]
		[DescriptionLoc(LocalizedStrings.Str1379Key)]
		[ReadOnly(true)]
		public DateTime StartedTime
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
		/// Общее время работы стратегии с вычетом временных отрезков, когда стратегия останавливалась.
		/// </summary>
		[Browsable(false)]
		public TimeSpan TotalWorkingTime
		{
			get
			{
				var retVal = _totalWorkingTime;

				if (StartedTime != default(DateTime) && Connector != null)
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
		/// Автоматически освобождать занятые ресурсы стратерии при ее остановке
		/// (состояние <see cref="ProcessState"/> стало равным <see cref="ProcessStates.Stopped"/>)
		/// и удалять ее из родительской через <see cref="ChildStrategies"/>.
		/// </summary>
		/// <remarks>Режим используется только для одноразовых стратегий, тоесть для тех, что не будут запущены повторно (например, котирование).</remarks>
		/// <remarks>По умолчанию выключено.</remarks>
		[Browsable(false)]
		public bool DisposeOnStop
		{
			get { return _disposeOnStop.Value; }
			set { _disposeOnStop.Value = value; }
		}

		private readonly StrategyParam<bool> _waitAllTrades;

		/// <summary>
		/// Останавливать стратегию только после получения всех сделок по зарегистрированным заявкам.
		/// </summary>
		/// <remarks>По умолчанию выключено.</remarks>
		[Browsable(false)]
		public bool WaitAllTrades
		{
			get { return _waitAllTrades.Value; }
			set { _waitAllTrades.Value = value; }
		}

		private readonly StrategyParam<bool> _commentOrders;

		/// <summary>
		/// Добавлять в <see cref="Order.Comment"/> название стратегии <see cref="Name"/>, выставившая заявку.
		/// </summary>
		/// <remarks>По умолчанию выключено.</remarks>
		[Browsable(false)]
		public bool CommentOrders
		{
			get { return _commentOrders.Value; }
			set { _commentOrders.Value = value; }
		}

		/// <summary>
		/// Зарегистрированные правила.
		/// </summary>
		[Browsable(false)]
		public IMarketRuleList Rules { get; private set; }

		private readonly object _rulesSuspendLock = new object();
		private int _rulesSuspendCount;

		/// <summary>
		/// Приостановлено ли исполнение правил.
		/// </summary>
		/// <remarks>
		/// Приостановка правил происходит через метод <see cref="SuspendRules()"/>.
		/// </remarks>
		[Browsable(false)]
		public bool IsRulesSuspended
		{
			get { return _rulesSuspendCount > 0; }
		}

		/// <summary>
		/// Событие отправки заявки на регистрацию.
		/// </summary>
		public event Action<Order> OrderRegistering;

		/// <summary>
		/// Событие об успешной регистрации заявки.
		/// </summary>
		public event Action<Order> OrderRegistered;

		/// <summary>
		/// Событие об ошибке регистрации заявки.
		/// </summary>
		public event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// Событие отправки стоп-заявки на регистрацию.
		/// </summary>
		public event Action<Order> StopOrderRegistering;

		/// <summary>
		/// Событие об успешной регистрации стоп-заявки.
		/// </summary>
		public event Action<Order> StopOrderRegistered;

		/// <summary>
		/// Событие об ошибке регистрации стоп-заявки.
		/// </summary>
		public event Action<OrderFail> StopOrderRegisterFailed;

		/// <summary>
		/// Событие об изменении заявки.
		/// </summary>
		public event Action<Order> OrderChanged;

		/// <summary>
		/// Событие об изменении стоп-заявки.
		/// </summary>
		public event Action<Order> StopOrderChanged;

		/// <summary>
		/// Событие отправки заявки на отмену.
		/// </summary>
		public event Action<Order> OrderCanceling;

		/// <summary>
		/// Событие отправки стоп-заявки на отмену.
		/// </summary>
		public event Action<Order> StopOrderCanceling;

		/// <summary>
		/// Событие отправки заявки на перерегистрацию.
		/// </summary>
		public event Action<Order, Order> OrderReRegistering;

		/// <summary>
		/// Событие отправки стоп-заявки на перерегистрацию.
		/// </summary>
		public event Action<Order, Order> StopOrderReRegistering;

		/// <summary>
		/// Событие об ошибке отмены заявки.
		/// </summary>
		public event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// Событие об ошибке отмены стоп-заявки.
		/// </summary>
		public event Action<OrderFail> StopOrderCancelFailed;

		/// <summary>
		/// Событие о появлении новых сделок.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Событие изменения подключения стратегии.
		/// </summary>
		public event Action ConnectorChanged;

		/// <summary>
		/// Событие изменения инструмента стратегии.
		/// </summary>
		public event Action SecurityChanged;

		/// <summary>
		/// Событие изменения портфеля стратегии.
		/// </summary>
		public event Action PortfolioChanged;

		/// <summary>
		/// Событие изменения позиций стратегии.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Событие возникновения ошибки в стратегии.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// Метод вызывается тогда, когда вызвался метод <see cref="Start"/>, и состояние <see cref="ProcessState"/> перешло в значение <see cref="ProcessStates.Started"/>.
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
		/// Метод вызывается тогда, когда состояние процесса <see cref="ProcessState"/> перешло в значение <see cref="ProcessStates.Stopping"/>.
		/// </summary>
		protected virtual void OnStopping()
		{
		}

		/// <summary>
		/// Метод вызывается тогда, когда состояние процесса <see cref="ProcessState"/> перешло в значение <see cref="ProcessStates.Stopped"/>.
		/// </summary>
		protected virtual void OnStopped()
		{
		}

		/// <summary>
		/// Зарегистрировать заявку и автоматически добавить для запуска механизмов расчета прибыли-убытка и проскальзывания.
		/// </summary>
		/// <param name="order">Заявка.</param>
		public virtual void RegisterOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

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
		/// Перерегистрировать заявку и автоматически добавить для запуска механизмов расчета прибыли-убытка и проскальзывания.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять и на основе нее зарегистрировать новую.</param>
		/// <param name="newOrder">Новая заявка.</param>
		public virtual void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			if (newOrder == null)
				throw new ArgumentNullException("newOrder");

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

				nOrder.State = OrderStates.Failed;

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

			IMarketRule matchedRule = order.WhenMatched();

			if (WaitAllTrades)
				matchedRule = matchedRule.And(order.WhenAllTrades());

			var successRule = order
				.WhenCanceled()
				.Or(matchedRule, order.WhenRegisterFailed())
				.Do(() => this.AddInfoLog(LocalizedStrings.Str1386Params.Put(order.GetTraceId())))
				.Until(() =>
				{
					if (order.State == OrderStates.Failed)
						return true;

					if (order.State != OrderStates.Done)
						return false;

					if (!WaitAllTrades)
						return true;

					var matchedVolume = order.GetMatchedVolume();

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
				.WhenCancelFailed()
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
		/// Отменить заявку.
		/// </summary>
		/// <param name="order">Заявка для отмены.</param>
		public virtual void CancelOrder(Order order)
		{
			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.Str1388Params, ProcessState);
				return;
			}

			if (order == null)
				throw new ArgumentNullException("order");

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
				throw new ArgumentNullException("order");

			this.AddInfoLog(LocalizedStrings.Str1315Params, order.GetTraceId());

			if (order.Type == OrderTypes.Conditional)
				OnStopOrderCanceling(order);
			else
				OnOrderCanceling(order);

			SafeGetConnector().CancelOrder(order);
		}

		/// <summary>
		/// Добавить заявку в стратегию.
		/// </summary>
		/// <param name="order">Заявка.</param>
		private void ProcessOrder(Order order)
		{
			ProcessOrder(order, false);
		}

		/// <summary>
		/// Добавить заявку в стратегию.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="isChanging">Заявка пришла из события изменения.</param>
		private void ProcessOrder(Order order, bool isChanging)
		{
			if (order == null)
				throw new ArgumentNullException("order");

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

						var isPosChanged = PositionManager.ProcessOrder(order) != 0;

						StatisticManager.AddNewOrder(order);

						if (order.Commission != null)
						{
							Commission += order.Commission;
							RaiseCommissionChanged();
						}

						if (isPosChanged)
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

					if (PositionManager.ProcessOrder(order) != 0)
						RaisePositionChanged();

					StatisticManager.AddChangedOrder(order);
				}
			});
		}

		/// <summary>
		/// Добавить активную заявку в стратегию и обработать сделки по заявке.
		/// </summary>
		/// <remarks>
		/// Используется для восстановления состояния стратегии, когда необходимо
		/// подписаться на получение данных по заявкам, зарегистрированным ранее.
		/// </remarks>
		/// <param name="order">Заявка.</param>
		/// <param name="myTrades">Сделки по заявке.</param>
		public virtual void AttachOrder(Order order, IEnumerable<MyTrade> myTrades)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (myTrades == null)
				throw new ArgumentNullException("myTrades");

			AttachOrder(order);

			OnConnectorNewMyTrades(myTrades);
		}

		private void AttachOrder(Order order)
		{
			AddOrder(order);

			if (order.Type != OrderTypes.Conditional)
				PositionManager.ProcessOrder(order);

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
		/// Установить идентификатор стратегии для заявки.
		/// </summary>
		/// <param name="order">Заявка, для которой необходимо установить идентификатор стратегии.</param>
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
		/// Текущее время, которое будет передано в <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get
			{
				return Connector == null ? TimeHelper.Now : Connector.CurrentTime;
			}
		}

		/// <summary>
		/// Вызвать событие <see cref="ILogSource.Log"/>.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected override void RaiseLog(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

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
		/// Запустить торговый алгоритм.
		/// </summary>
		public virtual void Start()
		{
			if (ProcessState == ProcessStates.Stopped)
				ProcessState = ProcessStates.Started;
			else 
				this.AddDebugLog(LocalizedStrings.Str1391Params, ProcessState);
		}

		/// <summary>
		/// Остановить торговый алгоритм.
		/// </summary>
		public virtual void Stop()
		{
			if (ProcessState == ProcessStates.Started)
				ProcessState = ProcessStates.Stopping;
			else
				this.AddDebugLog(LocalizedStrings.Str1392Params, ProcessState);
		}

		/// <summary>
		/// Событие переинициализации стратегии.
		/// </summary>
		public event Action Reseted;

		/// <summary>
		/// Вызвать событие <see cref="Reseted"/>.
		/// </summary>
		private void RaiseReseted()
		{
			Reseted.SafeInvoke();
		}

		/// <summary>
		/// Переинициализировать торговый алгоритм.
		/// Вызывается после инициализации объекта стратегии и загрузки сохраненных параметров.
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
		/// Вызывается из метода <see cref="Reset"/>. 
		/// </summary>
		protected virtual void OnReseted()
		{
			RaiseReseted();
		}

		/// <summary>
		/// Приостановить исполнение правил до следующего восстановления через метод <see cref="ResumeRules"/>.
		/// </summary>
		public virtual void SuspendRules()
		{
			lock (_rulesSuspendLock)
				_rulesSuspendCount++;

			this.AddDebugLog(LocalizedStrings.Str1394Params, _rulesSuspendCount);
		}

		/// <summary>
		/// Восстановить исполнение правил, остановленное через метод <see cref="SuspendRules()"/>.
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
		/// Интервал пересчета нереализованной прибыли. Значение по-умолчанию равно 1 минуте.
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
		/// Метод, который вызывается при изменении позиций стратегии.
		/// </summary>
		/// <param name="positions">Измененные позиции стратегии.</param>
		protected virtual void OnPositionsChanged(IEnumerable<Position> positions)
		{
			PositionsChanged.SafeInvoke(positions);
		}

		/// <summary>
		/// Метод, который вызывается при появлении новых сделок стратегии.
		/// </summary>
		/// <param name="trades">Новые сделки стратегии.</param>
		protected virtual void OnNewMyTrades(IEnumerable<MyTrade> trades)
		{
			NewMyTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrderRegistering"/>.
		/// </summary>
		/// <param name="order">Заявка.</param>
		protected virtual void OnOrderRegistering(Order order)
		{
			TryAddChildOrder(order);

			OrderRegistering.SafeInvoke(order);
			//SlippageManager.Registering(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrderRegistered"/>.
		/// </summary>
		/// <param name="order">Заявка.</param>
		protected virtual void OnOrderRegistered(Order order)
		{
			OrderRegistered.SafeInvoke(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrderRegistering"/>.
		/// </summary>
		/// <param name="order">Стоп-заявка.</param>
		protected virtual void OnStopOrderRegistering(Order order)
		{
			StopOrderRegistering.SafeInvoke(order);
			//SlippageManager.Registering(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrderRegistered"/>.
		/// </summary>
		/// <param name="order">Стоп-заявка.</param>
		protected virtual void OnStopOrderRegistered(Order order)
		{
			StopOrderRegistered.SafeInvoke(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrderRegistered"/>.
		/// </summary>
		/// <param name="order">Стоп-заявка.</param>
		protected virtual void OnStopOrderCanceling(Order order)
		{
			StopOrderCanceling.SafeInvoke(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrderRegistered"/>.
		/// </summary>
		/// <param name="order">Заявка.</param>
		protected virtual void OnOrderCanceling(Order order)
		{
			OrderCanceling.SafeInvoke(order);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrderReRegistering"/>.
		/// </summary>
		/// <param name="oldOrder">Стоп-заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая стоп-заявка, которую нужно зарегистрировать.</param>
		protected virtual void OnStopOrderReRegistering(Order oldOrder, Order newOrder)
		{
			StopOrderReRegistering.SafeInvoke(oldOrder, newOrder);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrderReRegistering"/>.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		protected virtual void OnOrderReRegistering(Order oldOrder, Order newOrder)
		{
			TryAddChildOrder(newOrder);
			
			OrderReRegistering.SafeInvoke(oldOrder, newOrder);
		}

		/// <summary>
		/// Метод, который вызывается при изменении заявки стратегии.
		/// </summary>
		/// <param name="order">Измененная заявка.</param>
		protected virtual void OnOrderChanged(Order order)
		{
			OrderChanged.SafeInvoke(order);

			if (order.LatencyCancellation != null)
			{
				Latency += order.LatencyCancellation;
				RaiseLatencyChanged();
			}
		}
	
		/// <summary>
		/// Метод, который вызывается при изменении стоп-заявки стратегии.
		/// </summary>
		/// <param name="order">Измененная стоп-заявка.</param>
		protected virtual void OnStopOrderChanged(Order order)
		{
			StopOrderChanged.SafeInvoke(order);
			StatisticManager.AddChangedOrder(order);
		}

		/// <summary>
		/// Метод, который вызывается при изменении стоп-заявок стратегии.
		/// </summary>
		/// <param name="orders">Измененные стоп-заявки.</param>
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
		/// Метод, который вызывается при ошибке регистрации заявки стратегии.
		/// </summary>
		/// <param name="fail">Ошибка регистрации заявки.</param>
		protected virtual void OnOrderRegisterFailed(OrderFail fail)
		{
			OrderRegisterFailed.SafeInvoke(fail);
			StatisticManager.AddRegisterFailedOrder(fail);
		}

		/// <summary>
		/// Метод, который вызывается при ошибке регистрации стоп-заявки стратегии.
		/// </summary>
		/// <param name="fail">Ошибка регистрации стоп-заявки.</param>
		protected virtual void OnStopOrderRegisterFailed(OrderFail fail)
		{
			StopOrderRegisterFailed.SafeInvoke(fail);
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

		private void OnConnectorNewMessage(Message message, MessageDirections messageDirections)
		{
			if (messageDirections != MessageDirections.Out)
				return;

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

					break;
				}

				case MessageTypes.Level1Change:
					PnLManager.ProcessMessage(message);
					break;

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.Trade:
							PnLManager.ProcessMessage(execMsg);
							break;

						default:
							return;
					}

					break;
				}

				case MessageTypes.Time:
					break;

				default:
					return;
			}

			if (message.LocalTime - _lastPnlRefreshTime < UnrealizedPnLInterval)
				return;

			_lastPnlRefreshTime = message.LocalTime;

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
					_isPrevDateTradable = board.WorkingTime.IsTradeDate(_prevTradeDate.Date);
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

		private void OnConnectorValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			ValuesChanged.SafeInvoke(security, changes, serverTime, localTime);
		}

		private void UpdatePnLManager(Security security)
		{
			var msg = new Level1ChangeMessage { SecurityId = security.ToSecurityId(), ServerTime = CurrentTime }
					.Add(Level1Fields.PriceStep, security.PriceStep)
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
			var isPosChanged = false;
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

				var tradeInfo = PnLManager.ProcessMyTrade(trade.ToMessage());
				if (tradeInfo.PnL != 0)
					isPnLChanged = true;

				if (PositionManager.ProcessMyTrade(trade) != 0)
					isPosChanged = true;

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

				if (isPosChanged)
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
			SlippageChanged.SafeInvoke();
		}

		private void RaisePositionChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str1399Params, PositionManager.Positions.Select(pos => pos + "=" + pos.CurrentValue).Join(", "));

			this.Notify("Position");
			PositionChanged.SafeInvoke();

			StatisticManager.AddPosition(CurrentTime, Position);
		}

		private void RaiseCommissionChanged()
		{
			this.Notify("Commission");
			CommissionChanged.SafeInvoke();
		}

		private void RaisePnLChanged()
		{
			this.Notify("PnL");
			PnLChanged.SafeInvoke();

			StatisticManager.AddPnL(_lastPnlRefreshTime, PnL);
		}

		private void RaiseLatencyChanged()
		{
			this.Notify("Latency");
			LatencyChanged.SafeInvoke();
		}

		/// <summary>
		/// Обработать поступившие от подключения <see cref="Connector"/> заявки, и найти из них те, что принадлежат стратегии.
		/// </summary>
		/// <param name="newOrders">Новые заявки.</param>
		/// <returns>Заявки, принадлежащие стратегии.</returns>
		protected virtual IEnumerable<Order> ProcessNewOrders(IEnumerable<Order> newOrders)
		{
			return _ordersInfo.SyncGet(d => newOrders.Where(IsOwnOrder).ToArray());
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
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
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Parameters", Parameters.SyncGet(c => c.Select(p => p.Save()).ToArray()));
		}

		/// <summary>
		/// Событие изменения параметров стратегии.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string info)
		{
			PropertyChanged.SafeInvoke(this, info);
		}

		/// <summary>
		/// Отменить все активные заявки (стоп и обычные).
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
		/// Отменить все активные заявки (стоп и обычные).
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
			TryInvoke(() => StopOrderChanged.SafeInvoke(order));
		}

		private void OnChildOrderRegisterFailed(OrderFail fail)
		{
			//SlippageManager.RegisterFailed(fail);
			TryInvoke(() => OrderRegisterFailed.SafeInvoke(fail));
		}

		private void OnChildStopOrderRegisterFailed(OrderFail fail)
		{
			//SlippageManager.RegisterFailed(fail);
			TryInvoke(() => StopOrderRegisterFailed.SafeInvoke(fail));
		}

		private void OnChildOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => OrderCancelFailed.SafeInvoke(fail));
		}

		private void OnChildStopOrderCancelFailed(OrderFail fail)
		{
			TryInvoke(() => StopOrderCancelFailed.SafeInvoke(fail));
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
					StopOrderCancelFailed.SafeInvoke(fail);
				else
					OrderCancelFailed.SafeInvoke(fail);

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
		/// Обработка ошибки, полученной в результате работы стратегии.
		/// </summary>
		/// <param name="error">Ошибка.</param>
		protected virtual void OnError(Exception error)
		{
			ErrorCount++;
			Error.SafeInvoke(error);

			this.AddErrorLog(error.ToString());

			if (ErrorCount >= MaxErrorCount)
				Stop();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Создать копию стратегии со всеми настройками.
		/// </summary>
		/// <returns>Копия стратегии.</returns>
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
		/// Событие изменения инструмента.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		/// <summary>
		/// Получить стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Стакан котировок.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			return SafeGetConnector().GetMarketDepth(security);
		}

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return SafeGetConnector().GetSecurityValue(security, field);
		}

		/// <summary>
		/// Получить набор доступных полей <see cref="Level1Fields"/>, для которых есть маркет-данные для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Набор доступных полей.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return SafeGetConnector().GetLevel1Fields(security);
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
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
		/// Освободить занятые ресурсы.
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