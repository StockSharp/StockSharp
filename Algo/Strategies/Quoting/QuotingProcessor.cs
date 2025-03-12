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
	private readonly Func<Security, Portfolio, decimal?> _getPosition;
	private readonly Func<StrategyTradingModes, bool> _isAllowed;
	private readonly bool _useLastTradePrice;
	private readonly Action _stop;
	private Order _currentOrder;
	private IOrderBookMessage _filteredBook;
	private ITickTradeMessage _lastTrade;
	private DateTimeOffset _startedTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotingProcessor"/> class.
	/// </summary>
	/// <param name="security"><see cref="Security"/></param>
	/// <param name="portfolio"><see cref="Portfolio"/></param>
	/// <param name="quotingSide">The direction of quoting (Buy or Sell).</param>
	/// <param name="quotingVolume">The total volume to be quoted.</param>
	/// <param name="maxOrderVolume">Maximum volume of a single order. If the total volume of <paramref name="quotingVolume"/> is greater than this value, the processor will split the quoting into multiple orders.</param>
	/// <param name="behavior">The behavior defining the quoting logic.</param>
	/// <param name="timeOut">The time limit during which the quoting should be fulfilled. If the total volume of <paramref name="quotingVolume"/> will not be fulfilled by this time, the strategy will stop operating.</param>
	/// <param name="subProvider"><see cref="ISubscriptionProvider"/></param>
	/// <param name="container"><see cref="IMarketRuleContainer"/></param>
	/// <param name="transProvider"><see cref="ITransactionProvider"/></param>
	/// <param name="timeProvider"><see cref="ITimeProvider"/></param>
	/// <param name="mdProvider"><see cref="IMarketDataProvider"/></param>
	/// <param name="getPosition">Get the current position of the security.</param>
	/// <param name="isAllowed">Is the strategy allowed to trade.</param>
	/// <param name="useLastTradePrice">To use the last trade price, if the information in the order book is missed.</param>
	/// <param name="stop">Stop action. The action is called when the quoting is finished.</param>
	public QuotingProcessor(Security security, Portfolio portfolio,
		Sides quotingSide, decimal quotingVolume, decimal maxOrderVolume,
		IQuotingBehavior behavior, TimeSpan timeOut, ISubscriptionProvider subProvider,
		IMarketRuleContainer container, ITransactionProvider transProvider,
		ITimeProvider timeProvider, IMarketDataProvider mdProvider,
		Func<Security, Portfolio, decimal?> getPosition, Func<StrategyTradingModes, bool> isAllowed,
		bool useLastTradePrice, Action stop)
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
		_getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
		_isAllowed = isAllowed ?? throw new ArgumentNullException(nameof(isAllowed));
		_useLastTradePrice = useLastTradePrice;
		_stop = stop ?? throw new ArgumentNullException(nameof(stop));
	}

	private decimal Position => _getPosition(_security, _portfolio) ?? 0;

	/// <summary>
	/// Left volume.
	/// </summary>
	public decimal LeftVolume => _quotingVolume - Position.Abs();

	private bool IsTimeOut(DateTimeOffset currentTime) => _timeOut != TimeSpan.Zero && (currentTime - _startedTime) >= _timeOut;
	private bool NeedFinish() => LeftVolume <= 0;

	/// <summary>
	/// Initializes the processor.
	/// </summary>
	public void Init()
	{
		LogInfo(LocalizedStrings.QuotingForVolume, _quotingSide, _quotingVolume);

		_startedTime = _timeProvider.CurrentTime;

		_container.SuspendRules(() =>
		{
			_subProvider
				.SubscribeFilteredMarketDepth(_security)
				.WhenOrderBookReceived(_subProvider)
				.Do(book =>
				{
					_filteredBook = book;
					ProcessQuoting(book.ServerTime);
				})
				.Apply(_container);

			_subProvider
				.SubscribeTrades(_security)
				.WhenTickTradeReceived(_subProvider)
				.Do(t =>
				{
					_lastTrade = t;
					ProcessQuoting(t.ServerTime);
				})
				.Apply(_container);

			_subProvider
				.WhenPositionReceived()
				.Do(() =>
				{
					LogInfo(LocalizedStrings.PrevPosNewPos, _security, Position, LeftVolume);

					if (NeedFinish())
					{
						LogInfo(LocalizedStrings.Stopped);
						_stop();
					}
				})
				.Apply(_container);

			if (_timeOut > TimeSpan.Zero)
			{
				_timeProvider
					.WhenIntervalElapsed(_timeOut)
					.Do(_stop)
					.Once()
					.Apply(_container);
			}
		});

		if (!_container.IsRulesSuspended)
			ProcessQuoting(_startedTime);
	}

	private void ProcessRegisteredOrder(Order o)
	{
		if (o == _currentOrder)
			LogInfo(LocalizedStrings.OrderAcceptedByExchange, o.TransactionId);
		else
			LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);

		ProcessQuoting(o.ServerTime);
	}

	private void AddOrderRules(Order order)
	{
		var regRule = order
			.WhenRegistered(_subProvider)
			.Do(ProcessRegisteredOrder)
			.Once()
			.Apply(_container);

		var regFailRule = order
			.WhenRegisterFailed(_subProvider)
			.Do(fail =>
			{
				var o = fail.Order;

				LogError(LocalizedStrings.ErrorRegOrder, o.TransactionId, fail.Error.Message);

				var canProcess = false;

				if (o == _currentOrder)
				{
					_currentOrder = null;
					canProcess = true;
				}
				else
					LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);

				if (canProcess)
					ProcessQuoting(fail.ServerTime);
			})
			.Once()
			.Apply(_container);

		regRule.Exclusive(regFailRule);

		var matchedRule = order
			.WhenMatched(_subProvider)
			.Do((r, o) =>
			{
				LogInfo(LocalizedStrings.OrderMatchedRemainBalance, o.TransactionId, LeftVolume);

				_container.Rules.RemoveRulesByToken(o, r);

				if (NeedFinish())
				{
					LogInfo(LocalizedStrings.Stopped);
					_stop();
				}
				else
				{
					if (_currentOrder == o)
					{
						_currentOrder = default;

						ProcessQuoting(o.ServerTime);
					}
					else
						LogWarning(LocalizedStrings.OrderOutOfDate, o.TransactionId);
				}
			})
			.Once()
			.Apply(_container);

		regFailRule.Exclusive(matchedRule);
	}

	private void ProcessQuoting(DateTimeOffset currentTime)
	{
		if (_container.ProcessState != ProcessStates.Started)
		{
			LogWarning(LocalizedStrings.StrategyInState, _container.ProcessState);
			return;
		}

		if (IsTimeOut(currentTime))
		{
			_stop();
			return;
		}

		var bestBidPrice = _filteredBook?.Bids?.FirstOr()?.Price;
		var bestAskPrice = _filteredBook?.Asks?.FirstOr()?.Price;
		var lastTradePrice = _useLastTradePrice ? _lastTrade?.Price : null;
		var bids = _filteredBook?.Bids ?? [];
		var asks = _filteredBook?.Asks ?? [];

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

					_transProvider.RegisterOrder(_currentOrder);
					LogInfo($"Registering order at price {price} with volume {volume}");
				}

				break;

			case false:
				if (_currentOrder != null && _isAllowed(StrategyTradingModes.CancelOrdersOnly))
				{
					_transProvider.CancelOrder(_currentOrder);
					LogInfo($"Cancelling order {_currentOrder.TransactionId}");
				}

				break;

			case null:
				break;
		}
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
		var newVolume = _quotingVolume - currentPosition.Abs();
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
		var quotingPrice = _behavior.NeedQuoting(_security, _mdProvider, currentPrice, currentVolume, newVolume, bestPrice);

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