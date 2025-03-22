namespace StockSharp.Algo;

partial class Connector
{
	/// <inheritdoc />
	[Obsolete("Use OwnTradeReceived event.")]
	public event Action<MyTrade> NewMyTrade;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> NewOrder;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderChanged;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<long, Order> OrderEdited;

	/// <inheritdoc />
	[Obsolete("Use OrderRegisterFailReceived event.")]
	public event Action<OrderFail> OrderRegisterFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderCancelFailReceived event.")]
	public event Action<OrderFail> OrderCancelFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderEditFailReceived event.")]
	public event Action<long, OrderFail> OrderEditFailed;

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<long, Exception, DateTimeOffset> OrderStatusFailed2;

	/// <inheritdoc />
	public event Action<long> MassOrderCanceled;

	/// <inheritdoc />
	public event Action<long, DateTimeOffset> MassOrderCanceled2;

	/// <inheritdoc />
	public event Action<long, Exception> MassOrderCancelFailed;

	/// <inheritdoc />
	public event Action<long, Exception, DateTimeOffset> MassOrderCancelFailed2;

	/// <inheritdoc />
	[Obsolete("Use PortfolioReceived event.")]
	public event Action<Portfolio> NewPortfolio;

	/// <inheritdoc />
	[Obsolete("Use PortfolioReceived event.")]
	public event Action<Portfolio> PortfolioChanged;

	/// <inheritdoc />
	[Obsolete("Use PositionReceived event.")]
	public event Action<Position> NewPosition;

	/// <inheritdoc />
	[Obsolete("Use PositionReceived event.")]
	public event Action<Position> PositionChanged;

	/// <inheritdoc />
	public event Action<Message> NewMessage;

	/// <inheritdoc />
	[Obsolete("Use CurrentTimeChanged event.")]
	public event Action<TimeSpan> MarketTimeChanged;

	/// <inheritdoc />
	public event Action<TimeSpan> CurrentTimeChanged;

	/// <inheritdoc />
	public event Action Connected;

	/// <inheritdoc />
	public event Action Disconnected;

	/// <inheritdoc />
	public event Action<Exception> ConnectionError;

	/// <inheritdoc />
	public event Action<IMessageAdapter> ConnectedEx;

	/// <inheritdoc />
	public event Action<IMessageAdapter> DisconnectedEx;

	/// <inheritdoc />
	public event Action<IMessageAdapter, Exception> ConnectionErrorEx;

	/// <inheritdoc cref="IConnector" />
	public event Action<Exception> Error;

	/// <inheritdoc />
	// TODO
	//[Obsolete("Use SecurityReceived and SubscriptionStopped events.")]
	public event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult;

	/// <inheritdoc />
	// TODO
	//[Obsolete("Use PortfolioReceived and SubscriptionStopped events.")]
	public event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> LookupPortfoliosResult;

	/// <inheritdoc />
	[Obsolete("Use BoardReceived and SubscriptionStopped events.")]
	public event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult;

	/// <inheritdoc />
	// TODO
	[Obsolete("Use SecurityReceived and SubscriptionStopped events.")]
	public event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2;

	/// <inheritdoc />
	[Obsolete("Use PortfolioReceived and SubscriptionStopped events.")]
	public event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> LookupPortfoliosResult2;

	/// <inheritdoc />
	[Obsolete("Use BoardReceived and SubscriptionStopped events.")]
	public event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult2;

	/// <inheritdoc />
	[Obsolete("Use DataTypeReceived and SubscriptionStopped events.")]
	public event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult;
	
	/// <inheritdoc />
	[Obsolete("Use DataTypeReceived and SubscriptionStopped events.")]
	public event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult2;

	/// <inheritdoc />
	[Obsolete("Use ISubscriptionProvider.BoardReceived event.")]
	public event Action<ExchangeBoard, SessionStates> SessionStateChanged;

	/// <inheritdoc />
	public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

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
	public event Action<Subscription> SubscriptionOnline;

	/// <inheritdoc />
	public event Action<Subscription> SubscriptionStarted;

	/// <inheritdoc />
	public event Action<Subscription, Exception> SubscriptionStopped;

	/// <inheritdoc />
	public event Action<Subscription, Exception, bool> SubscriptionFailed;

	/// <inheritdoc />
	public event Action<Subscription, object> SubscriptionReceived;

	/// <inheritdoc />
	public event Action<IMessageAdapter> ConnectionRestored;

	/// <inheritdoc />
	public event Action<IMessageAdapter> ConnectionLost;

	/// <inheritdoc />
	public event Action<long, Exception> ChangePasswordResult;

	private void RaiseNewMyTrade(MyTrade trade)
	{
		LogInfo("New own trade: {0}", trade);

		NewMyTrade?.Invoke(trade);
	}

	private void RaiseNewOrder(Order order)
	{
		NewOrder?.Invoke(order);
	}

	private void RaiseOrderChanged(Order order)
	{
		OrderChanged?.Invoke(order);
	}

	private void RaiseOrderEdited(long transactionId, Order order)
	{
		LogDebug("Order {0} edited by transaction {1}.", order, transactionId);
		OrderEdited?.Invoke(transactionId, order);
	}

	private void RaiseOrderFailed(string name, long transactionId, OrderFail fail, Action<long, OrderFail> failed)
	{
		this.AddErrorLog(() => name + Environment.NewLine + fail.Order + Environment.NewLine + fail.Error);
		failed?.Invoke(transactionId, fail);
	}

	private void RaiseOrderRegisterFailed(long transactionId, OrderFail fail)
	{
		RaiseOrderFailed(nameof(OrderRegisterFailed), transactionId, fail, (id, f) => OrderRegisterFailed?.Invoke(f));
	}

	private void RaiseOrderCancelFailed(long transactionId, OrderFail fail)
	{
		RaiseOrderFailed(nameof(OrderCancelFailed), transactionId, fail, (id, f) => OrderCancelFailed?.Invoke(f));
	}

	private void RaiseOrderEditFailed(long transactionId, OrderFail fail)
	{
		RaiseOrderFailed(nameof(OrderEditFailed), transactionId, fail, OrderEditFailed);
	}

	private void RaiseMassOrderCanceled(long transactionId, DateTimeOffset time)
	{
		MassOrderCanceled?.Invoke(transactionId);
		MassOrderCanceled2?.Invoke(transactionId, time);
	}

	private void RaiseMassOrderCancelFailed(long transactionId, Exception error, DateTimeOffset time)
	{
		MassOrderCancelFailed?.Invoke(transactionId, error);
		MassOrderCancelFailed2?.Invoke(transactionId, error, time);
	}

	private void RaiseOrderStatusFailed(long transactionId, Exception error, DateTimeOffset time)
	{
		OrderStatusFailed2?.Invoke(transactionId, error, time);
	}

	private void RaiseNewPortfolio(Portfolio portfolio)
	{
		NewPortfolio?.Invoke(portfolio);
	}

	private void RaisePortfolioChanged(Portfolio portfolio)
	{
		PortfolioChanged?.Invoke(portfolio);
	}

	private void RaiseNewPosition(Position position)
	{
		NewPosition?.Invoke(position);
	}

	private void RaisePositionChanged(Position position)
	{
		PositionChanged?.Invoke(position);
	}

	/// <summary>
	/// To call the event <see cref="Connected"/>.
	/// </summary>
	private void RaiseConnected()
	{
		ConnectionState = ConnectionStates.Connected;
		Connected?.Invoke();
	}

	/// <summary>
	/// To call the event <see cref="ConnectedEx"/>.
	/// </summary>
	/// <param name="adapter">Adapter, initiated event.</param>
	private void RaiseConnectedEx(IMessageAdapter adapter)
	{
		ConnectedEx?.Invoke(adapter);
	}

	/// <summary>
	/// To call the event <see cref="Disconnected"/>.
	/// </summary>
	private void RaiseDisconnected()
	{
		ConnectionState = ConnectionStates.Disconnected;
		Disconnected?.Invoke();
	}

	/// <summary>
	/// To call the event <see cref="DisconnectedEx"/>.
	/// </summary>
	/// <param name="adapter">Adapter, initiated event.</param>
	private void RaiseDisconnectedEx(IMessageAdapter adapter)
	{
		DisconnectedEx?.Invoke(adapter);
	}

	/// <summary>
	/// To call the event <see cref="ConnectionError"/>.
	/// </summary>
	/// <param name="exception">Error connection.</param>
	private void RaiseConnectionError(Exception exception)
	{
		if (exception == null)
			throw new ArgumentNullException(nameof(exception));

		ConnectionState = ConnectionStates.Failed;
		ConnectionError?.Invoke(exception);

		LogError(exception);
	}

	/// <summary>
	/// To call the event <see cref="ConnectionErrorEx"/>.
	/// </summary>
	/// <param name="adapter">Adapter, initiated event.</param>
	/// <param name="exception">Error connection.</param>
	private void RaiseConnectionErrorEx(IMessageAdapter adapter, Exception exception)
	{
		if (exception == null)
			throw new ArgumentNullException(nameof(exception));

		ConnectionErrorEx?.Invoke(adapter, exception);
	}

	/// <summary>
	/// To call the event <see cref="Error"/>.
	/// </summary>
	/// <param name="exception">Data processing error.</param>
	protected void RaiseError(Exception exception)
	{
		if (exception is null)
			throw new ArgumentNullException(nameof(exception));

		ErrorCount++;
		Error?.Invoke(exception);

		LogError(exception);
	}

	/// <summary>
	/// To call the event <see cref="CurrentTimeChanged"/>.
	/// </summary>
	/// <param name="diff">The difference in the time since the last call of the event. The first time the event passes the <see cref="TimeSpan.Zero"/> value.</param>
	private void RaiseCurrentTimeChanged(TimeSpan diff)
	{
		MarketTimeChanged?.Invoke(diff);
		CurrentTimeChanged?.Invoke(diff);
	}

	/// <summary>
	/// To call the event <see cref="LookupSecuritiesResult"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="error">An error of lookup operation. The value will be <see langword="null"/> if operation complete successfully.</param>
	/// <param name="newSecurities">Found instruments.</param>
	private void RaiseLookupSecuritiesResult(SecurityLookupMessage message, Exception error, Security[] newSecurities)
	{
		LookupSecuritiesResult?.Invoke(message, newSecurities, error);
		LookupSecuritiesResult2?.Invoke(message, [], newSecurities, error);
	}

	/// <summary>
	/// To call the event <see cref="LookupBoardsResult"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="error">An error of lookup operation. The value will be <see langword="null"/> if operation complete successfully.</param>
	/// <param name="newBoards">Found boards.</param>
	private void RaiseLookupBoardsResult(BoardLookupMessage message, Exception error, ExchangeBoard[] newBoards)
	{
		LookupBoardsResult?.Invoke(message, newBoards, error);
		LookupBoardsResult2?.Invoke(message, [], newBoards, error);
	}

	/// <summary>
	/// To call the event <see cref="LookupTimeFramesResult"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="error">An error of lookup operation. The value will be <see langword="null"/> if operation complete successfully.</param>
	/// <param name="newDataTypes">Found data types.</param>
	private void RaiseLookupDataTypesResult(DataTypeLookupMessage message, Exception error, DataType[] newDataTypes)
	{
		var newTimeFrames = newDataTypes.FilterTimeFrames().ToArray();

		LookupTimeFramesResult?.Invoke(message, newTimeFrames, error);
		LookupTimeFramesResult2?.Invoke(message, [], newTimeFrames, error);
	}

	/// <summary>
	/// To call the event <see cref="LookupPortfoliosResult"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="error">An error of lookup operation. The value will be <see langword="null"/> if operation complete successfully.</param>
	/// <param name="newPortfolios">Found portfolios.</param>
	private void RaiseLookupPortfoliosResult(PortfolioLookupMessage message, Exception error, Portfolio[] newPortfolios)
	{
		LookupPortfoliosResult?.Invoke(message, newPortfolios, error);
		LookupPortfoliosResult2?.Invoke(message, [], newPortfolios, error);
	}

	private void RaiseMarketDataSubscriptionSucceeded(MarketDataMessage message, Subscription subscription)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;

		var msg = LocalizedStrings.SubscribedOk.Put(securityId, message.DataType2);

		if (message.From != null && message.To != null)
			msg += LocalizedStrings.FromTill.Put(message.From.Value, message.To.Value);

		LogDebug(msg + ".");

		RaiseSubscriptionStarted(subscription);
	}

	private void RaiseMarketDataSubscriptionFailed(MarketDataMessage origin, SubscriptionResponseMessage reply, Subscription subscription)
	{
		if (origin == null)
			throw new ArgumentNullException(nameof(origin));

		if (reply == null)
			throw new ArgumentNullException(nameof(reply));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;
		var error = reply.Error ?? new NotSupportedException(LocalizedStrings.SubscriptionNotSupported.Put(origin));

		if (reply.IsNotSupported())
			LogWarning(LocalizedStrings.SubscriptionNotSupported, origin);
		else
			LogError(LocalizedStrings.SubscribedError, securityId, origin.DataType2, error.Message);

		RaiseSubscriptionFailed(subscription, error, true);
	}

	private void RaiseMarketDataUnSubscriptionSucceeded(MarketDataMessage message, Subscription subscription)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;

		var msg = LocalizedStrings.UnSubscribedOk.Put(securityId,	message.DataType2);

		if (message.From != null && message.To != null)
			msg += LocalizedStrings.FromTill.Put(message.From.Value, message.To.Value);

		LogDebug(msg + ".");

		RaiseSubscriptionStopped(subscription, null);
	}

	private void RaiseMarketDataUnSubscriptionFailed(MarketDataMessage origin, SubscriptionResponseMessage reply, Subscription subscription)
	{
		if (origin == null)
			throw new ArgumentNullException(nameof(origin));

		if (reply == null)
			throw new ArgumentNullException(nameof(reply));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;
		var error = reply.Error ?? new NotSupportedException();

		LogError(LocalizedStrings.UnSubscribedError, securityId, origin.DataType2, error.Message);

		RaiseSubscriptionFailed(subscription, error, false);
	}

	private void RaiseMarketDataSubscriptionFinished(SubscriptionFinishedMessage message, Subscription subscription)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;

		LogDebug(LocalizedStrings.SubscriptionFinished, securityId, message);

		RaiseSubscriptionStopped(subscription, null);
	}

	private void RaiseMarketDataUnexpectedCancelled(MarketDataMessage message, Exception error, Subscription subscription)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (error == null)
			throw new ArgumentNullException(nameof(error));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;

		LogError(LocalizedStrings.SubscriptionUnexpectedCancelled, securityId, message.DataType2, error.Message);

		RaiseSubscriptionStopped(subscription, error);
	}

	private void RaiseSubscriptionOnline(Subscription subscription)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		SubscriptionOnline?.Invoke(subscription);
	}

	private void RaiseSubscriptionStarted(Subscription subscription)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		SubscriptionStarted?.Invoke(subscription);
	}

	private void RaiseSubscriptionStopped(Subscription subscription, Exception error)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		SubscriptionStopped?.Invoke(subscription, error);
	}

	/// <summary>
	/// </summary>
	protected virtual void RaiseSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		if (error == null)
			throw new ArgumentNullException(nameof(error));

		SubscriptionFailed?.Invoke(subscription, error, isSubscribe);
	}

	private void RaiseMarketDataSubscriptionOnline(Subscription subscription)
	{
		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		var securityId = subscription.SecurityId;

		LogDebug(LocalizedStrings.SubscriptionOnline, securityId, subscription.SubscriptionMessage);

		RaiseSubscriptionOnline(subscription);
	}

	/// <summary>
	/// To call the event <see cref="NewMessage"/>.
	/// </summary>
	/// <param name="message">A new message.</param>
	private void RaiseNewMessage(Message message)
	{
		NewMessage?.Invoke(message);
		_newOutMessage?.Invoke(message);
	}

	private void RaiseValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
	{
		ValuesChanged?.Invoke(security, changes, serverTime, localTime);
	}

	private void RaiseConnectionLost(IMessageAdapter adapter)
	{
		ConnectionLost?.Invoke(adapter);
	}

	private void RaiseConnectionRestored(IMessageAdapter adapter)
	{
		ConnectionRestored?.Invoke(adapter);
	}

	private void RaiseSessionStateChanged(ExchangeBoard board, SessionStates state)
	{
		SessionStateChanged?.Invoke(board, state);
	}

	private void RaiseChangePassword(long transactionId, Exception error)
	{
		ChangePasswordResult?.Invoke(transactionId, error);
	}

	private bool? RaiseReceived<TEntity>(TEntity entity, ISubscriptionIdMessage message, Action<Subscription, TEntity> evt)
	{
		return RaiseReceived(entity, message, evt, out _);
	}

	private bool? RaiseReceived<TEntity>(TEntity entity, ISubscriptionIdMessage message, Action<Subscription, TEntity> evt, out bool? anyCanOnline)
	{
		return RaiseReceived(entity, _subscriptionManager.GetSubscriptions(message), evt, out anyCanOnline);
	}

	private void RaiseReceived<TEntity>(TEntity entity, IEnumerable<Subscription> subscriptions, Action<Subscription, TEntity> evt)
	{
		RaiseReceived(entity, subscriptions, evt, out _);
	}

	private bool? RaiseReceived<TEntity>(TEntity entity, IEnumerable<Subscription> subscriptions, Action<Subscription, TEntity> evt, out bool? anyCanOnline)
	{
		if (subscriptions is null)
			throw new ArgumentNullException(nameof(subscriptions));

		bool? anyOnline = null;
		anyCanOnline = null;

		foreach (var subscription in subscriptions)
		{
			anyOnline = anyOnline == true || subscription.State == SubscriptionStates.Online;
			anyCanOnline = anyCanOnline == true || (subscription.State == SubscriptionStates.Active && subscription.To is null);

			evt?.Invoke(subscription, entity);
			RaiseSubscriptionReceived(subscription, entity);
		}

		return anyOnline;
	}

	private void RaiseSubscriptionReceived(Subscription subscription, object arg)
	{
		SubscriptionReceived?.Invoke(subscription, arg);
	}

	private void RaiseLevel1Received(Subscription subscription, Level1ChangeMessage message)
	{
		Level1Received?.Invoke(subscription, message);
	}
}