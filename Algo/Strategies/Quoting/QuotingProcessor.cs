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

	private decimal Position { get; set; }

	/// <summary>
	/// Left volume.
	/// </summary>
	public decimal LeftVolume => GetLeftVolume(Position);

	private bool IsTimeOut(DateTimeOffset currentTime) => _timeOut != TimeSpan.Zero && (currentTime - _startedTime) >= _timeOut;
	private bool NeedFinish() => _finished || LeftVolume <= 0;

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

		_container.SuspendRules(() =>
		{
			Subscription addSub(Subscription subscription)
			{
				_subscriptions.Add(subscription);
				return subscription;
			}

			if (_useBidAsk)
			{
				var sub = new Subscription(DataType.FilteredMarketDepth, _security);

				AddRule(addSub(sub)
					.WhenOrderBookReceived(_subProvider)
					.Do(book =>
					{
						_filteredBook = book;
						ProcessQuoting(book.ServerTime);
					})
					.Apply(_container));

				_subProvider.Subscribe(sub);
			}

			if (_useTicks)
			{
				var sub = new Subscription(DataType.Ticks, _security);

				AddRule(addSub(sub)
					.WhenTickTradeReceived(_subProvider)
					.Do(t =>
					{
						_lastTrade = t;
						ProcessQuoting(t.ServerTime);
					})
					.Apply(_container));

				_subProvider.Subscribe(sub);
			}

			AddRule(_subProvider
				.WhenPositionReceived()
				.Do(() =>
				{
					LogInfo(LocalizedStrings.PrevPosNewPos, _security, Position, LeftVolume);

					if (NeedFinish())
						RaiseSuccess();
				})
				.Apply(_container));

			if (_timeOut > TimeSpan.Zero)
			{
				AddRule(_timeProvider
					.WhenIntervalElapsed(_timeOut)
					.Do(RaiseTimeOut)
					.Once()
					.Apply(_container));
			}
		});

		if (!_container.IsRulesSuspended)
			ProcessQuoting(_startedTime);
	}

	private void RaiseSuccess()
	{
		LogInfo(LocalizedStrings.Stopped);
		RaiseFinished(true);
	}

	private void RaiseTimeOut()
	{
		LogWarning(LocalizedStrings.TimeOut);
		RaiseFinished(false);
	}

	private void RaiseFinished(bool res)
	{
		if (_finished)
			return;

		_finished = true;

		foreach (var sub in _subscriptions.CopyAndClear())
			_subProvider.UnSubscribe(sub);

		foreach (var rule in _rules.CopyAndClear())
			_container.TryRemoveRule(rule, false);

		Finished?.Invoke(res);
	}

	private IMarketRule AddRule(IMarketRule rule)
	{
		if (rule is null)
			throw new ArgumentNullException(nameof(rule));

		_rules.Add(rule);

		return rule;
	}

	/// <summary>
	/// Stop the processor.
	/// </summary>
	public void Stop()
	{
		if (_pending || _currentOrder is null)
			return;

		_transProvider.CancelOrder(_currentOrder);
		_currentOrder = null;
	}

	private void ProcessRegisteredOrder(Order order)
	{
		if (order == _currentOrder)
		{
			LogInfo(LocalizedStrings.OrderAcceptedByExchange, order.TransactionId);

			_pending = default;

			OrderRegistered?.Invoke(order);
		}
		else
			LogWarning(LocalizedStrings.OrderOutOfDate, order.TransactionId);

		ProcessQuoting(order.ServerTime);
	}

	private void AddOrderRules(Order order)
	{
		var regRule = AddRule(order
			.WhenRegistered(_subProvider)
			.Do(ProcessRegisteredOrder)
			.Once()
			.Apply(_container));

		var regFailRule = AddRule(order
			.WhenRegisterFailed(_subProvider)
			.Do(fail =>
			{
				var o = fail.Order;

				LogError(LocalizedStrings.ErrorRegOrder, o.TransactionId, fail.Error.Message);

				var canProcess = false;

				if (o == _currentOrder)
				{
					_currentOrder = default;
					_pending = default;

					canProcess = true;

					OrderFailed?.Invoke(fail);
				}
				else
					LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);

				if (canProcess)
					ProcessQuoting(fail.ServerTime);
			})
			.Once()
			.Apply(_container));

		regRule.Exclusive(regFailRule);

		var matchedRule = AddRule(order
			.WhenMatched(_subProvider)
			.Do((r, o) =>
			{
				LogInfo(LocalizedStrings.OrderMatchedRemainBalance, o.TransactionId, LeftVolume);

				if (NeedFinish())
				{
					RaiseSuccess();
				}
				else
				{
					if (_currentOrder == o)
					{
						_currentOrder = default;
						_pending = default;

						ProcessQuoting(o.ServerTime);
					}
					else
						LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);
				}
			})
			.Once()
			.Apply(_container));

		var cancelledRule = AddRule(order
			.WhenCanceled(_subProvider)
			.Do((r, o) =>
			{
				if (NeedFinish())
				{
					RaiseSuccess();
				}
				else
				{
					if (_currentOrder == o)
					{
						_currentOrder = default;
						_pending = default;

						ProcessQuoting(o.ServerTime);
					}
					else
						LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);
				}
			})
			.Once()
			.Apply(_container));

		var cancelFailRule = AddRule(order
			.WhenCancelFailed(_subProvider)
			.Do((r, f) =>
			{
				if (NeedFinish())
				{
					RaiseSuccess();
				}
				else
				{
					if (_currentOrder == f.Order)
					{
					}
					else
						LogWarning(LocalizedStrings.OrderOutOfDate, f.Order.TransactionId);
				}
			})
			.Apply(_container));

		var tradeRule = AddRule(order
			.WhenNewTrade(_subProvider)
			.Do((r, t) =>
			{
				Position += t.GetPosition();

				if (_currentOrder == t.Order)
					OwnTrade?.Invoke(t);
				else
					LogWarning(LocalizedStrings.OrderOutOfDate, t.Order.TransactionId);
			})
			.Apply(_container));

		regFailRule.Exclusive(cancelledRule);
		regFailRule.Exclusive(matchedRule);
		regFailRule.Exclusive(tradeRule);
	}

	private void ProcessQuoting(DateTimeOffset currentTime)
	{
		if (_finished)
			return;

		if (_container.ProcessState != ProcessStates.Started)
		{
			LogWarning(LocalizedStrings.StrategyInState, _container.ProcessState);
			return;
		}

		if (IsTimeOut(currentTime))
		{
			RaiseTimeOut();
			return;
		}

		if (_pending)
			return;

		var bids = _filteredBook?.Bids ?? [];
		var asks = _filteredBook?.Asks ?? [];
		var bestBidPrice = bids.FirstOr()?.Price;
		var bestAskPrice = asks.FirstOr()?.Price;
		var lastTradePrice = _lastTrade?.Price;
		
		var (isRegister, price, volume) = Process(
			_currentOrder,
			bestBidPrice,
			bestAskPrice,
			lastTradePrice,
			bids,
			asks,
			Position
		);

		switch (isRegister)
		{
			case true:
				if (_currentOrder == null && _isAllowed(StrategyTradingModes.Full))
				{
					var order = new Order
					{
						Portfolio = _portfolio,
						Security = _security,
						Side = _quotingSide,
						Volume = volume.Value,
					};

					if (price is null)
						order.Type = OrderTypes.Market;
					else
						order.Price = price.Value;

					_currentOrder = order;
					AddOrderRules(_currentOrder);

					_pending = true;
					_transProvider.RegisterOrder(_currentOrder);
					LogInfo($"Registering order at price {price} with volume {volume}");
				}

				break;

			case false:
				if (_currentOrder != null && _isAllowed(StrategyTradingModes.CancelOrdersOnly))
				{
					_pending = true;
					_transProvider.CancelOrder(_currentOrder);
					LogInfo($"Cancelling order {_currentOrder.TransactionId}");
				}

				break;

			case null:
				break;
		}
	}

	private decimal GetLeftVolume(decimal position)
	{
		var sign = _quotingSide == Sides.Buy ? 1 : -1;
		return (_quotingVolume - position * sign).Max(0);
	}

	private (bool? isRegister, decimal? price, decimal? volume) Process(
		Order currentOrder,
		decimal? bestBidPrice,
		decimal? bestAskPrice,
		decimal? lastTradePrice,
		QuoteChange[] bids,
		QuoteChange[] asks,
		decimal currentPosition)
	{
		// Calculate the desired volume based on the current position
		var newVolume = GetLeftVolume(currentPosition);
		if (newVolume <= 0)
			return default;

		newVolume = _maxOrderVolume.Min(newVolume);

		// Delegate best price calculation to the behavior
		var bestPrice = _behavior.CalculateBestPrice(
			_security, _mdProvider, _quotingSide, bestBidPrice, bestAskPrice, lastTradePrice, bids, asks);

		if (bestPrice == null)
			return default;

		// Delegate quoting necessity check to the behavior
		var currentPrice = currentOrder?.Price;
		var currentVolume = currentOrder?.Balance;
		var quotingPrice = _behavior.NeedQuoting(_security, _mdProvider, _timeProvider.CurrentTime, currentPrice, currentVolume, newVolume, bestPrice);

		if (quotingPrice == null)
			return default;

		if (currentOrder == null)
		{
			// If no order exists, recommend registration
			return new(true, quotingPrice, newVolume);
		}
		else
		{
			// If an order exists, check if it needs to be canceled
			if (currentPrice != quotingPrice || currentVolume != newVolume)
				return new(false, default, default);

			return default;
		}
	}
}