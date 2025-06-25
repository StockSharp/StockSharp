namespace StockSharp.Algo.Strategies;

/// <summary>
/// Extension class for <see cref="Strategy"/>.
/// </summary>
public static partial class StrategyHelper
{
	/// <summary>
	/// To create initialized object of buy order at market price.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	[Obsolete("Use Strategy.BuyMarket method.")]
	public static Order BuyAtMarket(this Strategy strategy, decimal? volume = null)
	{
		return strategy.CreateOrder(Sides.Buy, default, volume);
	}

	/// <summary>
	/// To create the initialized order object of sell order at market price.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	[Obsolete("Use Strategy.SellMarket method.")]
	public static Order SellAtMarket(this Strategy strategy, decimal? volume = null)
	{
		return strategy.CreateOrder(Sides.Sell, default, volume);
	}

	/// <summary>
	/// To create the initialized order object for buy.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	[Obsolete("Use Strategy.BuyLimit method.")]
	public static Order BuyAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
	{
		return strategy.CreateOrder(Sides.Buy, price, volume);
	}

	/// <summary>
	/// To create the initialized order object for sell.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="price">Price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	[Obsolete("Use Strategy.SellLimit method.")]
	public static Order SellAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
	{
		return strategy.CreateOrder(Sides.Sell, price, volume);
	}

	private const string _isEmulationModeKey = "IsEmulationMode";

	/// <summary>
	/// To get the strategy start-up mode (paper trading or real).
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <returns>If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</returns>
	[Obsolete("Use Strategy.IsBacktesting property.")]
	public static bool GetIsEmulation(this Strategy strategy)
	{
		return strategy.Environment.GetValue(_isEmulationModeKey, false);
	}

	/// <summary>
	/// To set the strategy start-up mode (paper trading or real).
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="isEmulation">If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</param>
	[Obsolete("Use Strategy.IsBacktesting property.")]
	public static void SetIsEmulation(this Strategy strategy, bool isEmulation)
	{
		strategy.Environment.SetValue(_isEmulationModeKey, isEmulation);
	}

	#region Strategy rules

	private abstract class StrategyRule<TArg>(Strategy strategy) : MarketRule<Strategy, TArg>(strategy)
	{
		protected Strategy Strategy { get; } = strategy ?? throw new ArgumentNullException(nameof(strategy));
	}

	private sealed class PnLManagerStrategyRule : StrategyRule<decimal>
	{
		private readonly Func<decimal, bool> _changed;

		public PnLManagerStrategyRule(Strategy strategy)
			: this(strategy, v => true)
		{
			Name = LocalizedStrings.PnLChange;
		}

		public PnLManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
			: base(strategy)
		{
			_changed = changed ?? throw new ArgumentNullException(nameof(changed));

			Strategy.PnLChanged += OnPnLChanged;
		}

		private void OnPnLChanged()
		{
			if (_changed(Strategy.PnL))
				Activate(Strategy.PnL);
		}

		protected override void DisposeManaged()
		{
			Strategy.PnLChanged -= OnPnLChanged;
			base.DisposeManaged();
		}
	}

	private sealed class PositionManagerStrategyRule : StrategyRule<decimal>
	{
		private readonly Func<decimal, bool> _changed;

		public PositionManagerStrategyRule(Strategy strategy)
			: this(strategy, v => true)
		{
			Name = LocalizedStrings.Positions;
		}

		public PositionManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
			: base(strategy)
		{
			_changed = changed ?? throw new ArgumentNullException(nameof(changed));

			((IPositionProvider)Strategy).PositionChanged += OnPositionChanged;
		}

		private void OnPositionChanged(Position position)
		{
			if (Strategy.Security != position.Security || Strategy.Portfolio != position.Portfolio)
				return;

			if (_changed(position.CurrentValue ?? 0))
				Activate(Strategy.Position);
		}

		protected override void DisposeManaged()
		{
			((IPositionProvider)Strategy).PositionChanged -= OnPositionChanged;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private sealed class NewMyTradeStrategyRule : StrategyRule<MyTrade>
	{
		public NewMyTradeStrategyRule(Strategy strategy)
			: base(strategy)
		{
			Name = LocalizedStrings.NewTrades + " " + strategy;
			Strategy.NewMyTrade += OnStrategyNewMyTrade;
		}

		private void OnStrategyNewMyTrade(MyTrade trade)
		{
			Activate(trade);
		}

		protected override void DisposeManaged()
		{
			Strategy.NewMyTrade -= OnStrategyNewMyTrade;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private sealed class OrderRegisteredStrategyRule : StrategyRule<Order>
	{
		public OrderRegisteredStrategyRule(Strategy strategy)
			: base(strategy)
		{
			Name = LocalizedStrings.Orders + " " + strategy;
			Strategy.OrderRegistered += Activate;
		}

		protected override void DisposeManaged()
		{
			Strategy.OrderRegistered -= Activate;
			base.DisposeManaged();
		}
	}

	[Obsolete]
	private sealed class OrderChangedStrategyRule : StrategyRule<Order>
	{
		public OrderChangedStrategyRule(Strategy strategy)
			: base(strategy)
		{
			Name = LocalizedStrings.Orders + " " + strategy;
			Strategy.OrderChanged += Activate;
		}

		protected override void DisposeManaged()
		{
			Strategy.OrderChanged -= Activate;
			base.DisposeManaged();
		}
	}

	private sealed class ProcessStateChangedStrategyRule : StrategyRule<Strategy>
	{
		private readonly Func<ProcessStates, bool> _condition;

		public ProcessStateChangedStrategyRule(Strategy strategy, Func<ProcessStates, bool> condition)
			: base(strategy)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Strategy.ProcessStateChanged += OnProcessStateChanged;
		}

		private void OnProcessStateChanged(Strategy strategy)
		{
			if (_condition(Strategy.ProcessState))
				Activate(Strategy);
		}

		protected override void DisposeManaged()
		{
			Strategy.ProcessStateChanged -= OnProcessStateChanged;
			base.DisposeManaged();
		}
	}

	private sealed class PropertyChangedStrategyRule : StrategyRule<Strategy>
	{
		private readonly Func<Strategy, bool> _condition;

		public PropertyChangedStrategyRule(Strategy strategy, Func<Strategy, bool> condition)
			: base(strategy)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Strategy.PropertyChanged += OnPropertyChanged;
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_condition(Strategy))
				Activate(Strategy);
		}

		protected override void DisposeManaged()
		{
			Strategy.PropertyChanged -= OnPropertyChanged;
			base.DisposeManaged();
		}
	}

	private sealed class ErrorStrategyRule : StrategyRule<Exception>
	{
		private readonly bool _processChildStrategyErrors;

		public ErrorStrategyRule(Strategy strategy, bool processChildStrategyErrors)
			: base(strategy)
		{
			_processChildStrategyErrors = processChildStrategyErrors;

			Name = strategy + LocalizedStrings.Error;
			Strategy.Error += OnError;
		}

		private void OnError(Strategy strategy, Exception error)
		{
			if (!_processChildStrategyErrors && !Equals(Strategy, strategy))
				return;

			Activate(error);
		}

		protected override void DisposeManaged()
		{
			Strategy.Error -= OnError;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of occurrence new strategy trade.
	/// </summary>
	/// <param name="strategy">The strategy, based on which trade occurrence will be traced.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOwnTradeReceived rule.")]
	public static MarketRule<Strategy, MyTrade> WhenNewMyTrade(this Strategy strategy)
	{
		return new NewMyTradeStrategyRule(strategy);
	}

	/// <summary>
	/// To create a rule for event of occurrence of new strategy order.
	/// </summary>
	/// <param name="strategy">The strategy, based on which order occurrence will be traced.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use ISubscriptionProvider overload.")]
	public static MarketRule<Strategy, Order> WhenOrderRegistered(this Strategy strategy)
	{
		return new OrderRegisteredStrategyRule(strategy);
	}

	/// <summary>
	/// To create a rule for event of change of any strategy order.
	/// </summary>
	/// <param name="strategy">The strategy, based on which orders change will be traced.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenOrderReceived rule.")]
	public static MarketRule<Strategy, Order> WhenOrderChanged(this Strategy strategy)
	{
		return new OrderChangedStrategyRule(strategy);
	}

	/// <summary>
	/// To create a rule for the event of strategy position change.
	/// </summary>
	/// <param name="strategy">The strategy, based on which position change will be traced.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPositionChanged(this Strategy strategy)
	{
		return new PositionManagerStrategyRule(strategy);
	}

	/// <summary>
	/// To create a rule for event of position event reduction below the specified level.
	/// </summary>
	/// <param name="strategy">The strategy, based on which position change will be traced.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPositionLess(this Strategy strategy, Unit value)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position - value;

		return new PositionManagerStrategyRule(strategy, pos => pos < finishPosition)
		{
			Name = $"Pos < {value}"
		};
	}

	/// <summary>
	/// To create a rule for event of position event increase above the specified level.
	/// </summary>
	/// <param name="strategy">The strategy, based on which position change will be traced.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPositionMore(this Strategy strategy, Unit value)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position + value;

		return new PositionManagerStrategyRule(strategy, pos => pos > finishPosition)
		{
			Name = $"Pos > {value}"
		};
	}

	/// <summary>
	/// To create a rule for event of profit reduction below the specified level.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPnLLess(this Strategy strategy, Unit value)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL - value;

		return new PnLManagerStrategyRule(strategy, pos => pos < finishPosition)
		{
			Name = $"P&L < {value}"
		};
	}

	/// <summary>
	/// To create a rule for event of profit increase above the specified level.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPnLMore(this Strategy strategy, Unit value)
	{
		if (strategy == null)
			throw new ArgumentNullException(nameof(strategy));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL + value;

		return new PnLManagerStrategyRule(strategy, pos => pos > finishPosition)
		{
			Name = $"P&L > {value}"
		};
	}

	/// <summary>
	/// To create a rule for event of profit change.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, decimal> WhenPnLChanged(this Strategy strategy)
	{
		return new PnLManagerStrategyRule(strategy);
	}

	/// <summary>
	/// To create a rule for event of start of strategy operation.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the start of strategy operation will be expected.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, Strategy> WhenStarted(this Strategy strategy)
	{
		return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Started)
		{
			Name = strategy + LocalizedStrings.Started,
		};
	}

	/// <summary>
	/// To create a rule for event of beginning of the strategy operation stop.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the beginning of stop will be determined.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, Strategy> WhenStopping(this Strategy strategy)
	{
		return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopping)
		{
			Name = strategy + LocalizedStrings.Stopping,
		};
	}

	/// <summary>
	/// To create a rule for event full stop of strategy operation.
	/// </summary>
	/// <param name="strategy">The strategy, based on which the full stop will be expected.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, Strategy> WhenStopped(this Strategy strategy)
	{
		return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopped)
		{
			Name = strategy + LocalizedStrings.Stopped,
		};
	}

	/// <summary>
	/// To create a rule for event of strategy error (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Error"/>).
	/// </summary>
	/// <param name="strategy">The strategy, based on which error will be expected.</param>
	/// <param name="processChildStrategyErrors">Process the child strategies errors.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, Exception> WhenError(this Strategy strategy, bool processChildStrategyErrors = false)
	{
		return new ErrorStrategyRule(strategy, processChildStrategyErrors);
	}

	/// <summary>
	/// To create a rule for event of strategy warning (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Warning"/>).
	/// </summary>
	/// <param name="strategy">The strategy, based on which the warning will be expected.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Strategy, Strategy> WhenWarning(this Strategy strategy)
	{
		return new PropertyChangedStrategyRule(strategy, s => s.ErrorState == LogLevels.Warning)
		{
			Name = strategy + LocalizedStrings.Warning,
		};
	}

	#endregion

	#region Order actions

	/// <summary>
	/// To create an action, registering the order.
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <param name="order">The order to be registered.</param>
	/// <returns>Rule.</returns>
	public static IMarketRule Register(this IMarketRule rule, Order order)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return rule.Do(() => GetRuleStrategy(rule).RegisterOrder(order));
	}

	/// <summary>
	/// To create an action, re-registering the order.
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <param name="oldOrder">The order to be re-registered.</param>
	/// <param name="newOrder">Information about new order.</param>
	/// <returns>Rule.</returns>
	public static IMarketRule ReRegister(this IMarketRule rule, Order oldOrder, Order newOrder)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (oldOrder == null)
			throw new ArgumentNullException(nameof(oldOrder));

		if (newOrder == null)
			throw new ArgumentNullException(nameof(newOrder));

		return rule.Do(() => GetRuleStrategy(rule).ReRegisterOrder(oldOrder, newOrder));
	}

	/// <summary>
	/// To create an action, cancelling the order.
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <param name="order">The order to be cancelled.</param>
	/// <returns>Rule.</returns>
	public static IMarketRule Cancel(this IMarketRule rule, Order order)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return rule.Do(() => GetRuleStrategy(rule).CancelOrder(order));
	}

	#endregion

	private static Strategy GetRuleStrategy(IMarketRule rule)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (rule.Container is not Strategy strategy)
			throw new ArgumentException(LocalizedStrings.RuleNotRegisteredInStrategy.Put(rule), nameof(rule));

		return strategy;
	}

	/// <summary>
	/// Execute strategy.
	/// </summary>
	/// <param name="strategy"><see cref="Strategy"/>.</param>
	/// <param name="extra">Extra action.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public static async ValueTask<(bool completed, Exception error)> ExecAsync(this Strategy strategy, Action extra, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (strategy.ProcessState != ProcessStates.Stopped)
			throw new ArgumentException($"State is {strategy.ProcessState}.", nameof(strategy));

		var tcs = AsyncHelper.CreateTaskCompletionSource<int>();

		const int canceled = 1;
		const int completed = 2;
		const int error = 3;

		var finalResult = 0;

		using var registration = cancellationToken.Register(() =>
		{
			if (Interlocked.CompareExchange(ref finalResult, canceled, 0) == 0)
				tcs.TrySetResult(canceled);
		});

		void OnProcessStateChanged(Strategy s)
		{
			if (s != strategy)
				return;

			if (s.ProcessState == ProcessStates.Stopped)
			{
				if (finalResult != 0)
					return;

				var result = s.LastError is null ? completed : error;

				if (Interlocked.CompareExchange(ref finalResult, result, 0) == 0)
					tcs.TrySetResult(result);
			}
			else if (s.ProcessState == ProcessStates.Started)
			{
				if (!((ISubscriptionProvider)strategy).Subscriptions.Any(s => s.DataType.IsMarketData))
				{
					s.AddErrorLog("No any market data subscription.");
					s.Stop();
				}
			}
		}

		strategy.ProcessStateChanged += OnProcessStateChanged;

		try
		{
			await Task.Yield();

			strategy.Start();

			extra?.Invoke();

			var res = await tcs.Task;

			return (res == completed, strategy.LastError);
		}
		finally
		{
			strategy.ProcessStateChanged -= OnProcessStateChanged;

			if (finalResult == canceled)
				strategy.Stop();
		}
	}
}