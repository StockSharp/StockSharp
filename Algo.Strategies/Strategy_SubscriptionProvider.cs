namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private ISubscriptionProvider SubscriptionProvider => SafeGetConnector();

	IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => _subscriptions.CachedKeys;

	Subscription ISubscriptionProvider.SecurityLookup { get; }
	Subscription ISubscriptionProvider.BoardLookup { get; }
	Subscription ISubscriptionProvider.DataTypeLookup { get; }

	private Subscription ToSubscription<TLookupMessage>()
		where TLookupMessage : IStrategyIdMessage, ISubscriptionMessage, new()
		=> new(new TLookupMessage
		{
			StrategyId = EnsureGetId(),
		});

	private Subscription _portfolioLookup;
	/// <inheritdoc />
	public Subscription PortfolioLookup => _portfolioLookup ??= ToSubscription<PortfolioLookupMessage>();

	private Subscription _orderLookup;
	/// <inheritdoc />
	public Subscription OrderLookup => _orderLookup ??= ToSubscription<OrderStatusMessage>();

	/// <inheritdoc />
	public event Action<Subscription, Level1ChangeMessage> Level1Received;

	/// <inheritdoc />
	public event Action<Subscription, IOrderBookMessage> OrderBookReceived;

	/// <inheritdoc />
	public event Action<Subscription, ITickTradeMessage> TickTradeReceived;

	/// <inheritdoc />
	public event Action<Subscription, IOrderLogMessage> OrderLogReceived;

	/// <inheritdoc />
	public event Action<Subscription, Security> SecurityReceived;

	/// <inheritdoc />
	public event Action<Subscription, ExchangeBoard> BoardReceived;

	/// <inheritdoc />
	public event Action<Subscription, News> NewsReceived;

	/// <inheritdoc />
	public event Action<Subscription, ICandleMessage> CandleReceived;

	/// <inheritdoc />
	public event Action<Subscription, MyTrade> OwnTradeReceived;

	/// <inheritdoc />
	public event Action<Subscription, Order> OrderReceived;

	/// <inheritdoc />
	public event Action<Subscription, OrderFail> OrderRegisterFailReceived;

	/// <inheritdoc />
	public event Action<Subscription, OrderFail> OrderCancelFailReceived;

	/// <inheritdoc />
	public event Action<Subscription, OrderFail> OrderEditFailReceived;

	/// <inheritdoc />
	public event Action<Subscription, Portfolio> PortfolioReceived;

	/// <inheritdoc />
	public event Action<Subscription, Position> PositionReceived;

	/// <inheritdoc />
	public event Action<Subscription, DataType> DataTypeReceived;

	/// <inheritdoc />
	public event Action<Subscription, object> SubscriptionReceived;

	/// <inheritdoc />
	public event Action<Subscription> SubscriptionOnline;

	/// <inheritdoc />
	public event Action<Subscription> SubscriptionStarted;

	/// <inheritdoc />
	public event Action<Subscription, Exception> SubscriptionStopped;

	/// <inheritdoc />
	public event Action<Subscription, Exception, bool> SubscriptionFailed;

	/// <inheritdoc />
	public void Subscribe(Subscription subscription)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (!IsBacktesting)
		{
			var history = HistorySize ?? TimeSpan.Zero;

			if (history < HistoryCalculated)
				history = HistoryCalculated.Value;

			if (history > TimeSpan.Zero)
			{
				if (subscription.From is null)
				{
					var dataType = subscription.DataType;

					if (dataType.IsMarketData && dataType.IsSecurityRequired)
						subscription.From = CurrentTime - history;
				}
			}
		}

		Subscribe(subscription, false);
	}

	private void Subscribe(Subscription subscription, bool isGlobal)
	{
		var connector = Connector;

		if (connector is null)
			return;

		_subscriptions.Add(subscription, isGlobal);

		if (subscription.TransactionId == default)
			subscription.TransactionId = connector.TransactionIdGenerator.GetNextId();

		_subscriptionsById.Add(subscription.TransactionId, subscription);

		if (_rulesSuspendCount > 0)
		{
			_suspendSubscriptions.Add(subscription);
			return;
		}

		connector.Subscribe(subscription);
	}

	/// <inheritdoc />
	public void UnSubscribe(Subscription subscription)
	{
		ArgumentNullException.ThrowIfNull(subscription);

		if (subscription.TransactionId == 0)
			return;

		var connector = Connector;

		if (connector is null)
			return;

		if (ProcessState != ProcessStates.Started && IsBacktesting)
		{
			_subscriptions.Remove(subscription);
			_subscriptionsById.Remove(subscription.TransactionId);

			connector.UnSubscribe(subscription);
			return;
		}

		if (_rulesSuspendCount > 0 && _suspendSubscriptions.Remove(subscription))
		{
			_subscriptions.Remove(subscription);
			_subscriptionsById.Remove(subscription.TransactionId);
			return;
		}

		connector.UnSubscribe(subscription);
	}

	private void OnConnectorSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
	{
		if (!CanProcess(subscription))
			return;

		SubscriptionFailed?.Invoke(subscription, error, isSubscribe);
		CheckRefreshOnlineState();
	}

	private void OnConnectorSubscriptionStopped(Subscription subscription, Exception error)
	{
		if (!CanProcess(subscription))
			return;

		SubscriptionStopped?.Invoke(subscription, error);
		CheckRefreshOnlineState();
	}

	private void OnConnectorSubscriptionStarted(Subscription subscription)
	{
		if (!CanProcess(subscription))
			return;

		SubscriptionStarted?.Invoke(subscription);
	}

	private void OnConnectorSubscriptionOnline(Subscription subscription)
	{
		if (!CanProcess(subscription))
			return;

		SubscriptionOnline?.Invoke(subscription);
		CheckRefreshOnlineState();
	}

	private void OnConnectorSubscriptionReceived(Subscription subscription, object arg)
	{
		if (CanProcess(subscription))
			SubscriptionReceived?.Invoke(subscription, arg);
	}

	private void OnConnectorDataTypeReceived(Subscription subscription, DataType dt)
	{
		if (CanProcess(subscription))
			DataTypeReceived?.Invoke(subscription, dt);
	}

	private void OnConnectorCandleReceived(Subscription subscription, ICandleMessage candle)
	{
		if (CanProcess(subscription))
			CandleReceived?.Invoke(subscription, candle);
	}

	private void OnConnectorNewsReceived(Subscription subscription, News news)
	{
		if (CanProcess(subscription))
			NewsReceived?.Invoke(subscription, news);
	}

	private void OnConnectorBoardReceived(Subscription subscription, ExchangeBoard board)
	{
		if (CanProcess(subscription))
			BoardReceived?.Invoke(subscription, board);
	}

	private void OnConnectorSecurityReceived(Subscription subscription, Security security)
	{
		if (CanProcess(subscription))
			SecurityReceived?.Invoke(subscription, security);
	}

	private void OnConnectorTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
	{
		if (CanProcess(subscription))
			TickTradeReceived?.Invoke(subscription, trade);
	}

	private void OnConnectorOrderBookReceived(Subscription subscription, IOrderBookMessage message)
	{
		if (CanProcess(subscription))
			OrderBookReceived?.Invoke(subscription, message);
	}

	private void OnConnectorOrderLogReceived(Subscription subscription, IOrderLogMessage message)
	{
		if (CanProcess(subscription))
			OrderLogReceived?.Invoke(subscription, message);
	}

	private void OnConnectorLevel1Received(Subscription subscription, Level1ChangeMessage message)
	{
		if (CanProcess(subscription))
			Level1Received?.Invoke(subscription, message);
	}

	private bool CanProcess(Subscription subscription)
		=> !IsDisposeStarted && _subscriptions.ContainsKey(subscription);
}
