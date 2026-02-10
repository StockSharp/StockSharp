namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Standalone manager that drives position to a target value using configurable algorithms.
/// </summary>
public class PositionTargetManager : BaseLogReceiver
{
	private class TargetState
	{
		public decimal Target;
		public IPositionModifyAlgo ActiveAlgo;
		public Order ActiveOrder;
		public int RetryCount;
		public bool Canceled;
	}

	private readonly ISubscriptionProvider _subProvider;
	private readonly ITransactionProvider _transProvider;
	private readonly Func<Security, Portfolio, decimal?> _getPosition;
	private readonly Func<Order> _orderFactory;
	private readonly Func<bool> _canTrade;
	private readonly Func<Sides, decimal, IPositionModifyAlgo> _algoFactory;
	private readonly IMarketRuleContainer _container;
	private readonly SynchronizedDictionary<(Security, Portfolio), TargetState> _targets = [];
	private readonly HashSet<IMarketRule> _rules = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionTargetManager"/>.
	/// </summary>
	/// <param name="subProvider">Subscription provider.</param>
	/// <param name="transProvider">Transaction provider.</param>
	/// <param name="container">Market rule container.</param>
	/// <param name="getPosition">Function to get current position for a security/portfolio pair.</param>
	/// <param name="orderFactory">Factory to create new orders with desired properties.</param>
	/// <param name="canTrade">Function that returns whether trading is allowed.</param>
	/// <param name="algoFactory">Factory to create position modify algorithms. Parameters: side, volume.</param>
	public PositionTargetManager(
		ISubscriptionProvider subProvider,
		ITransactionProvider transProvider,
		IMarketRuleContainer container,
		Func<Security, Portfolio, decimal?> getPosition,
		Func<Order> orderFactory,
		Func<bool> canTrade,
		Func<Sides, decimal, IPositionModifyAlgo> algoFactory)
	{
		_subProvider = subProvider ?? throw new ArgumentNullException(nameof(subProvider));
		_transProvider = transProvider ?? throw new ArgumentNullException(nameof(transProvider));
		_container = container ?? throw new ArgumentNullException(nameof(container));
		_getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
		_orderFactory = orderFactory ?? throw new ArgumentNullException(nameof(orderFactory));
		_canTrade = canTrade ?? throw new ArgumentNullException(nameof(canTrade));
		_algoFactory = algoFactory ?? throw new ArgumentNullException(nameof(algoFactory));

		_container.Rules.Removed += OnRulesRemoved;
	}

	private void OnRulesRemoved(IMarketRule rule)
		=> _rules.Remove(rule);

	/// <summary>
	/// Order type to use. Default is <see cref="OrderTypes.Market"/>.
	/// </summary>
	public OrderTypes OrderType { get; set; } = OrderTypes.Market;

	/// <summary>
	/// Maximum number of retries on order failure.
	/// </summary>
	public int MaxRetries { get; set; } = 3;

	/// <summary>
	/// Tolerance for considering position target reached.
	/// </summary>
	public decimal PositionTolerance { get; set; }

	/// <summary>
	/// Occurs when a target position is reached.
	/// </summary>
	public event Action<Security, Portfolio> TargetReached;

	/// <summary>
	/// Occurs when an error happens during target management.
	/// </summary>
	public event Action<Security, Portfolio, Exception> Error;

	/// <summary>
	/// Occurs when an order is registered by the manager.
	/// </summary>
	public event Action<Order> OrderRegistered;

	/// <summary>
	/// Set target position for a security/portfolio pair.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="target">Target position value.</param>
	public void SetTarget(Security security, Portfolio portfolio, decimal target)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		var key = (security, portfolio);

		using (_targets.EnterScope())
		{
			if (_targets.TryGetValue(key, out var existing))
			{
				existing.Target = target;

				// cancel active algo if direction changed
				if (existing.ActiveAlgo is not null && !existing.ActiveAlgo.IsFinished)
				{
					existing.Canceled = true;

					if (existing.ActiveOrder is not null && !existing.ActiveOrder.State.IsFinal())
						_transProvider.CancelOrder(existing.ActiveOrder);

					existing.ActiveAlgo.Cancel();
					existing.ActiveAlgo.Dispose();
					existing.ActiveAlgo = null;
					existing.ActiveOrder = null;
					existing.RetryCount = 0;
					existing.Canceled = false;
				}
			}
			else
			{
				_targets[key] = new TargetState { Target = target };
			}
		}

		ProcessTarget(security, portfolio);
	}

	/// <summary>
	/// Cancel target for a security/portfolio pair.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	public void CancelTarget(Security security, Portfolio portfolio)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		var key = (security, portfolio);

		using (_targets.EnterScope())
		{
			if (_targets.TryGetValue(key, out var state))
			{
				state.Canceled = true;

				if (state.ActiveOrder is not null && !state.ActiveOrder.State.IsFinal())
					_transProvider.CancelOrder(state.ActiveOrder);

				state.ActiveAlgo?.Cancel();
				state.ActiveAlgo?.Dispose();
				_targets.Remove(key);
			}
		}
	}

	/// <summary>
	/// Get target position for a security/portfolio pair.
	/// </summary>
	/// <returns>Target position, or null if not set.</returns>
	public decimal? GetTarget(Security security, Portfolio portfolio)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		return _targets.TryGetValue((security, portfolio), out var state) ? state.Target : null;
	}

	/// <summary>
	/// Check if target position is reached.
	/// </summary>
	public bool IsTargetReached(Security security, Portfolio portfolio)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		if (!_targets.TryGetValue((security, portfolio), out var state))
			return false;

		var currentPos = _getPosition(security, portfolio) ?? 0;
		return Math.Abs(currentPos - state.Target) <= PositionTolerance;
	}

	private void ProcessTarget(Security security, Portfolio portfolio)
	{
		var key = (security, portfolio);

		TargetState state;
		using (_targets.EnterScope())
		{
			if (!_targets.TryGetValue(key, out state))
				return;

			if (state.Canceled)
				return;
		}

		var currentPos = _getPosition(security, portfolio) ?? 0;
		var delta = state.Target - currentPos;

		if (Math.Abs(delta) <= PositionTolerance)
		{
			// target reached
			using (_targets.EnterScope())
			{
				state.ActiveAlgo?.Dispose();
				state.ActiveAlgo = null;
				state.ActiveOrder = null;
			}

			TargetReached?.Invoke(security, portfolio);
			return;
		}

		if (!_canTrade())
			return;

		// already has an active algo running
		if (state.ActiveAlgo is not null && !state.ActiveAlgo.IsFinished)
			return;

		var side = delta > 0 ? Sides.Buy : Sides.Sell;
		var volume = Math.Abs(delta);

		var algo = _algoFactory(side, volume);
		state.ActiveAlgo = algo;
		state.RetryCount = 0;

		ExecuteAlgo(security, portfolio, state);
	}

	private void ExecuteAlgo(Security security, Portfolio portfolio, TargetState state)
	{
		if (state.Canceled)
			return;

		var algo = state.ActiveAlgo;
		if (algo is null || algo.IsFinished)
		{
			ProcessTarget(security, portfolio);
			return;
		}

		var action = algo.GetNextAction();

		switch (action.ActionType)
		{
			case PositionModifyAction.ActionTypes.None:
				break;

			case PositionModifyAction.ActionTypes.Register:
			{
				if (!_canTrade())
					return;

				var order = _orderFactory();
				order.Security = security;
				order.Portfolio = portfolio;
				order.Side = action.Side.Value;
				order.Volume = action.Volume.Value;
				order.Type = action.OrderType ?? OrderType;

				if (action.Price is decimal price)
					order.Price = price;

				state.ActiveOrder = order;
				SetupOrderRules(order, security, portfolio, state);
				_transProvider.RegisterOrder(order);
				OrderRegistered?.Invoke(order);
				break;
			}

			case PositionModifyAction.ActionTypes.Cancel:
			{
				if (state.ActiveOrder is not null && !state.ActiveOrder.State.IsFinal())
					_transProvider.CancelOrder(state.ActiveOrder);
				break;
			}

			case PositionModifyAction.ActionTypes.Finished:
			{
				state.ActiveAlgo?.Dispose();
				state.ActiveAlgo = null;
				state.ActiveOrder = null;
				ProcessTarget(security, portfolio);
				break;
			}
		}
	}

	private void SetupOrderRules(Order order, Security security, Portfolio portfolio, TargetState state)
	{
		var matchedRule = AddRule(order
			.WhenMatched(_subProvider)
			.Do(() =>
			{
				var matchedVol = order.Volume;

				state.ActiveOrder = null;
				state.ActiveAlgo?.OnOrderMatched(matchedVol);

				ExecuteAlgo(security, portfolio, state);
			})
			.Apply(_container));

		var regFailRule = AddRule(order
			.WhenRegisterFailed(_subProvider)
			.Do(fail =>
			{
				state.ActiveOrder = null;
				state.ActiveAlgo?.OnOrderFailed();
				state.RetryCount++;

				if (state.RetryCount >= MaxRetries)
				{
					state.ActiveAlgo?.Dispose();
					state.ActiveAlgo = null;
					Error?.Invoke(security, portfolio, fail.Error);
				}
				else
				{
					ExecuteAlgo(security, portfolio, state);
				}
			})
			.Once()
			.Apply(_container));

		var canceledRule = AddRule(order
			.WhenCanceled(_subProvider)
			.Do(() =>
			{
				var matchedVol = order.GetMatchedVolume() ?? 0;

				state.ActiveOrder = null;
				state.ActiveAlgo?.OnOrderCanceled(matchedVol);

				if (!state.Canceled)
					ExecuteAlgo(security, portfolio, state);
			})
			.Once()
			.Apply(_container));

		matchedRule.Exclusive(regFailRule);
		regFailRule.Exclusive(canceledRule);
	}

	private IMarketRule AddRule(IMarketRule rule)
	{
		if (rule is null)
			throw new ArgumentNullException(nameof(rule));

		_rules.Add(rule);
		return rule;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_container.Rules.Removed -= OnRulesRemoved;

		foreach (var pair in _targets.CopyAndClear())
		{
			pair.Value.ActiveAlgo?.Dispose();
		}

		foreach (var rule in _rules.CopyAndClear())
			_container.TryRemoveRule(rule, false);

		base.DisposeManaged();
	}
}
