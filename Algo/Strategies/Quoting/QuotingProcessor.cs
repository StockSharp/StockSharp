namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// A passive quoting processor that analyzes market data and order state to recommend actions.
/// </summary>
public class QuotingProcessor : BaseLogReceiver
{
	private readonly Security _security;
	private readonly Portfolio _portfolio;
	private readonly Sides _quotingSide;
	private readonly decimal _quotingVolume;
	private readonly decimal _maxOrderVolume;
	private readonly IQuotingBehavior _behavior;
	private readonly TimeSpan _timeOut;
	private readonly IMarketDataProvider _mdProvider;
	private readonly ISubscriptionProvider _subProvider;
	private readonly IMarketRuleContainer _container;
	private readonly ITransactionProvider _transProvider;
	private readonly ITimeProvider _timeProvider;
	private readonly Func<StrategyTradingModes, bool> _isAllowed;
	private readonly List<Subscription> _subscriptions = [];
	private readonly HashSet<IMarketRule> _rules = [];
	private readonly bool _useBidAsk;
	private readonly bool _useTicks;
	private bool _finished;
	private bool _pending;
	private Order _currentOrder;
	private IOrderBookMessage _filteredBook;
	private ITickTradeMessage _lastTrade;
	private DateTimeOffset _startedTime;
	private QuotingEngine _engine;
	private decimal _position;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotingProcessor"/> class.
	/// </summary>
	/// <param name="behavior">The behavior defining the quoting logic.</param>
	/// <param name="security"><see cref="Security"/></param>
	/// <param name="portfolio"><see cref="Portfolio"/></param>
	/// <param name="quotingSide">The direction of quoting (Buy or Sell).</param>
	/// <param name="quotingVolume">The total volume to be quoted.</param>
	/// <param name="maxOrderVolume">Maximum volume of a single order. If the total volume of <paramref name="quotingVolume"/> is greater than this value, the processor will split the quoting into multiple orders.</param>
	/// <param name="timeOut">The time limit during which the quoting should be fulfilled. If the total volume of <paramref name="quotingVolume"/> will not be fulfilled by this time, the strategy will stop operating.</param>
	/// <param name="subProvider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="container"><see cref="IMarketRuleContainer"/></param>
	/// <param name="transProvider"><see cref="ITransactionProvider"/></param>
	/// <param name="timeProvider"><see cref="ITimeProvider"/></param>
	/// <param name="mdProvider"><see cref="IMarketDataProvider"/></param>
	/// <param name="isAllowed">Is the strategy allowed to trade.</param>
	/// <param name="useBidAsk">To use the best bid and ask prices from the order book. If the information in the order book is missed, the processor will not recommend any actions.</param>
	/// <param name="useTicks">To use the last trade price, if the information in the order book is missed.</param>
	public QuotingProcessor(
		IQuotingBehavior behavior,
		Security security, Portfolio portfolio,
		Sides quotingSide, decimal quotingVolume, decimal maxOrderVolume,
		TimeSpan timeOut, ISubscriptionProvider subProvider,
		IMarketRuleContainer container, ITransactionProvider transProvider,
		ITimeProvider timeProvider, IMarketDataProvider mdProvider,
		Func<StrategyTradingModes, bool> isAllowed,
		bool useBidAsk, bool useTicks)
	{
		if (quotingVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(quotingVolume), quotingVolume, LocalizedStrings.InvalidValue);

		if (maxOrderVolume <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxOrderVolume), maxOrderVolume, LocalizedStrings.InvalidValue);

		_security = security ?? throw new ArgumentNullException(nameof(security));
		_portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
		_quotingSide = quotingSide;
		_quotingVolume = quotingVolume;
		_maxOrderVolume = maxOrderVolume;
		_behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
		_timeOut = timeOut;
		_subProvider = subProvider ?? throw new ArgumentNullException(nameof(subProvider));
		_container = container ?? throw new ArgumentNullException(nameof(container));
		_transProvider = transProvider ?? throw new ArgumentNullException(nameof(transProvider));
		_timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
		_mdProvider = mdProvider ?? throw new ArgumentNullException(nameof(mdProvider));
		_isAllowed = isAllowed ?? throw new ArgumentNullException(nameof(isAllowed));
		_useBidAsk = useBidAsk;
		_useTicks = useTicks;

		_container.Rules.Removed += OnRulesRemoved;
	}

	private void OnRulesRemoved(IMarketRule rule)
		=> _rules.Remove(rule);

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_container.Rules.Removed -= OnRulesRemoved;
		Stop();

		base.DisposeManaged();
	}

	/// <summary>
	/// Left volume.
	/// </summary>
	public decimal LeftVolume => _engine?.GetLeftVolume(_position) ?? 0;

	/// <summary>
	/// Occurs when an order is registered.
	/// </summary>
	public event Action<Order> OrderRegistered;

	/// <summary>
	/// Occurs when an order fails to be registered.
	/// </summary>
	public event Action<OrderFail> OrderFailed;

	/// <summary>
	/// Occurs when an own trade is received.
	/// </summary>
	public event Action<MyTrade> OwnTrade;

	/// <summary>
	/// Occurs when the processor is finished. The argument indicates whether the quoting was successful or by timeout.
	/// </summary>
	public event Action<bool> Finished;

	/// <summary>
	/// Start the processor.
	/// </summary>
	public void Start()
	{
		LogInfo(LocalizedStrings.QuotingForVolume, _quotingSide, _quotingVolume);

		_currentOrder = default;
		_pending = default;
		_finished = default;

		_startedTime = _timeProvider.CurrentTime;

		_engine = new QuotingEngine(
			_behavior, _security, _portfolio, _quotingSide, _quotingVolume,
			_maxOrderVolume, _timeOut, _mdProvider, _startedTime);

		_container.SuspendRules(() =>
		{
			SetupSubscriptions();
			SetupTimeoutRule();
			SetupPositionRule();
		});

		if (!_container.IsRulesSuspended)
			ProcessQuoting();
	}

	/// <summary>
	/// Stop the processor.
	/// </summary>
	public void Stop()
	{
		if (_currentOrder != null && !_currentOrder.State.IsFinal())
		{
			_transProvider.CancelOrder(_currentOrder);
		}

		RaiseFinished(false);
	}

	private void SetupSubscriptions()
	{
		if (_useBidAsk)
		{
			var sub = new Subscription(DataType.FilteredMarketDepth, _security);
			AddSubscription(sub);

			AddRule(sub
				.WhenOrderBookReceived(_subProvider)
				.Do(book =>
				{
					_filteredBook = book;
					ProcessQuoting();
				})
				.Apply(_container));

			_subProvider.Subscribe(sub);
		}

		if (_useTicks)
		{
			var sub = new Subscription(DataType.Ticks, _security);
			AddSubscription(sub);

			AddRule(sub
				.WhenTickTradeReceived(_subProvider)
				.Do(trade =>
				{
					_lastTrade = trade;
					ProcessQuoting();
				})
				.Apply(_container));

			_subProvider.Subscribe(sub);
		}
	}

	private void SetupPositionRule()
	{
		AddRule(_subProvider
			.WhenPositionReceived()
			.Do(pos =>
			{
				if (pos.Security != _security || pos.Portfolio != _portfolio)
					return;

				_position = pos.CurrentValue ?? 0;

				LogInfo(LocalizedStrings.PrevPosNewPos, _security, _position, LeftVolume);
				ProcessQuoting();
			})
			.Apply(_container));
	}

	private void SetupTimeoutRule()
	{
		if (_timeOut > TimeSpan.Zero)
		{
			AddRule(_timeProvider
				.WhenIntervalElapsed(_timeOut)
				.Do(() => RaiseFinished(false))
				.Once()
				.Apply(_container));
		}
	}

	private void ProcessQuoting()
	{
		if (_finished || _engine == null)
			return;

		// Get action from engine
		var action = _engine.ProcessQuoting(CreateInput());

		// Execute the action
		ExecuteAction(action);
	}

	private void ExecuteAction(QuotingAction action)
	{
		switch (action.ActionType)
		{
			case QuotingActionType.None:
				if (!string.IsNullOrEmpty(action.Reason))
					LogDebug($"No action: {action.Reason}");
				break;

			case QuotingActionType.Register:
				RegisterNewOrder(action);
				break;

			case QuotingActionType.Cancel:
				CancelCurrentOrder(action.Reason);
				break;

			case QuotingActionType.Modify:
				// For simplicity, treat modify as cancel (will trigger re-registration)
				CancelCurrentOrder("Modifying order");
				break;

			case QuotingActionType.Finish:
				RaiseFinished(action.IsSuccess);
				break;
		}
	}

	private void RegisterNewOrder(QuotingAction action)
	{
		if (_currentOrder != null || _pending)
		{
			LogWarning("Cannot register order - order already exists or pending");
			return;
		}

		var order = new Order
		{
			Portfolio = _portfolio,
			Security = _security,
			Side = _quotingSide,
			Volume = action.Volume.Value,
			Type = action.OrderType.Value
		};

		if (action.Price.HasValue)
			order.Price = action.Price.Value;

		_currentOrder = order;
		_pending = true;

		SetupOrderRules(order);
		_transProvider.RegisterOrder(order);

		LogInfo($"Registering order: {action.Reason}");
	}

	private void CancelCurrentOrder(string reason)
	{
		if (_currentOrder == null || _pending)
		{
			LogWarning("Cannot cancel order - no order exists or already pending");
			return;
		}

		_pending = true;
		_transProvider.CancelOrder(_currentOrder);
		LogInfo($"Cancelling order: {reason}");
	}

	private void SetupOrderRules(Order order)
	{
		var regRule = AddRule(order
			.WhenRegistered(_subProvider)
			.Do(() =>
			{
				LogInfo(LocalizedStrings.OrderAcceptedByExchange, order.TransactionId);
				_pending = false;
				OrderRegistered?.Invoke(order);

				var action = _engine.ProcessOrderResult(true, CreateInput());
				ExecuteAction(action);
			})
			.Once()
			.Apply(_container));

		var regFailRule = AddRule(order
			.WhenRegisterFailed(_subProvider)
			.Do(fail =>
			{
				LogError(LocalizedStrings.ErrorRegOrder, order.TransactionId, fail.Error.Message);
				_currentOrder = null;
				_pending = false;
				OrderFailed?.Invoke(fail);

				var action = _engine.ProcessOrderResult(false, CreateInput());
				ExecuteAction(action);
			})
			.Once()
			.Apply(_container));

		var matchedRule = AddRule(order
			.WhenMatched(_subProvider)
			.Do(() =>
			{
				LogInfo(LocalizedStrings.OrderMatchedRemainBalance, order.TransactionId, LeftVolume);

				if (order.State == OrderStates.Done)
				{
					_currentOrder = null;
					_pending = false;
				}

				ProcessQuoting();
			})
			.Apply(_container));

		var cancelledRule = AddRule(order
			.WhenCanceled(_subProvider)
			.Do(() =>
			{
				LogInfo($"Order {order.TransactionId} cancelled");
				_currentOrder = null;
				_pending = false;

				var action = _engine.ProcessCancellationResult(true, CreateInput());
				ExecuteAction(action);
			})
			.Once()
			.Apply(_container));

		var cancelFailRule = AddRule(order
			.WhenCancelFailed(_subProvider)
			.Do(fail =>
			{
				LogWarning($"Order cancellation failed: {fail.Error.Message}");
				_pending = false;

				var action = _engine.ProcessCancellationResult(false, CreateInput());
				ExecuteAction(action);
			})
			.Once()
			.Apply(_container));

		var tradeRule = AddRule(order
			.WhenNewTrade(_subProvider)
			.Do(trade =>
			{
				_position += trade.GetPosition();
				LogInfo($"Trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");
				OwnTrade?.Invoke(trade);

				var action = _engine.ProcessTrade(trade.Trade.Volume, CreateInput());
				ExecuteAction(action);
			})
			.Apply(_container));

		regRule.Exclusive(regFailRule);
		regFailRule.Exclusive(cancelledRule);
		regFailRule.Exclusive(matchedRule);
		regFailRule.Exclusive(tradeRule);
	}

	private QuotingInput CreateInput()
	{
		return new()
		{
			CurrentTime = _timeProvider.CurrentTime,
			Position = _position,
			BestBidPrice = _filteredBook?.Bids?.FirstOr()?.Price,
			BestAskPrice = _filteredBook?.Asks?.FirstOr()?.Price,
			LastTradePrice = _lastTrade?.Price,
			Bids = _filteredBook?.Bids ?? [],
			Asks = _filteredBook?.Asks ?? [],
			CurrentOrder = _currentOrder != null ? new()
			{
				Price = _currentOrder.Price,
				Volume = _currentOrder.Balance,
				Side = _currentOrder.Side,
				Type = _currentOrder.Type,
				IsPending = _pending
			} : null,
			IsTradingAllowed = _isAllowed(StrategyTradingModes.Full),
			IsCancellationAllowed = _isAllowed(StrategyTradingModes.CancelOrdersOnly)
		};
	}

	private void RaiseFinished(bool success)
	{
		if (_finished)
			return;

		_finished = true;

		LogInfo($"Quoting finished with success: {success}");

		// Clean up subscriptions
		foreach (var sub in _subscriptions.CopyAndClear())
			_subProvider.UnSubscribe(sub);

		// Clean up rules
		foreach (var rule in _rules.CopyAndClear())
			_container.TryRemoveRule(rule, false);

		Finished?.Invoke(success);
	}

	private Subscription AddSubscription(Subscription subscription)
	{
		_subscriptions.Add(subscription);
		return subscription;
	}

	private IMarketRule AddRule(IMarketRule rule)
	{
		if (rule is null)
			throw new ArgumentNullException(nameof(rule));

		_rules.Add(rule);
		return rule;
	}
}