namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		private ISubscriptionProvider SubscriptionProvider => (ISubscriptionProvider)SafeGetConnector();

		IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => _subscriptions.CachedKeys;

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
		[Obsolete("Use OrderBookReceived event.")]
		public event Action<Subscription, MarketDepth> MarketDepthReceived;

		/// <inheritdoc />
		[Obsolete("Use OrderLogReceived event.")]
		public event Action<Subscription, OrderLogItem> OrderLogItemReceived;

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
					var subscrMsg = subscription.SubscriptionMessage;

					if (subscrMsg.From is null)
					{
						var dataType = subscrMsg.DataType;

						if (dataType.IsMarketData && dataType.IsSecurityRequired)
							subscrMsg.From = DateTimeOffset.Now - history;
					}
				}
			}

			Subscribe(subscription, false);
		}

		private void Subscribe(Subscription subscription, bool isGlobal)
		{
			_subscriptions.Add(subscription, isGlobal);

			if (subscription.TransactionId == default)
				subscription.TransactionId = Connector.TransactionIdGenerator.GetNextId();

			_subscriptionsById.Add(subscription.TransactionId, subscription);

			if (_rulesSuspendCount > 0)
			{
				_suspendSubscriptions.Add(subscription);
				return;
			}

			SubscriptionProvider.Subscribe(subscription);
		}

		/// <inheritdoc />
		public void UnSubscribe(Subscription subscription)
		{
			if (_rulesSuspendCount > 0 && _suspendSubscriptions.Remove(subscription))
			{
				_subscriptions.Remove(subscription);
				_subscriptionsById.Remove(subscription.TransactionId);
				return;
			}

			SubscriptionProvider.UnSubscribe(subscription);
		}

		private void OnConnectorSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
		{
			if (IsDisposeStarted || !_subscriptions.ContainsKey(subscription))
				return;

			SubscriptionFailed?.Invoke(subscription, error, isSubscribe);
			CheckRefreshOnlineState();
		}

		private void OnConnectorSubscriptionStopped(Subscription subscription, Exception error)
		{
			if (IsDisposeStarted || !_subscriptions.ContainsKey(subscription))
				return;

			SubscriptionStopped?.Invoke(subscription, error);
			CheckRefreshOnlineState();
		}

		private void OnConnectorSubscriptionStarted(Subscription subscription)
		{
			if (IsDisposeStarted || !_subscriptions.ContainsKey(subscription))
				return;

			SubscriptionStarted?.Invoke(subscription);
		}

		private void OnConnectorSubscriptionOnline(Subscription subscription)
		{
			if (IsDisposeStarted || !_subscriptions.ContainsKey(subscription))
				return;

			SubscriptionOnline?.Invoke(subscription);
			CheckRefreshOnlineState();
		}

		private void OnConnectorSubscriptionReceived(Subscription subscription, object arg)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				SubscriptionReceived?.Invoke(subscription, arg);
		}

		private void OnConnectorCandleReceived(Subscription subscription, ICandleMessage candle)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				CandleReceived?.Invoke(subscription, candle);
		}

		private void OnConnectorNewsReceived(Subscription subscription, News news)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				NewsReceived?.Invoke(subscription, news);
		}

		private void OnConnectorBoardReceived(Subscription subscription, ExchangeBoard board)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				BoardReceived?.Invoke(subscription, board);
		}

		private void OnConnectorSecurityReceived(Subscription subscription, Security security)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				SecurityReceived?.Invoke(subscription, security);
		}

		private void OnConnectorTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				TickTradeReceived?.Invoke(subscription, trade);
		}

		private void OnConnectorOrderBookReceived(Subscription subscription, IOrderBookMessage message)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
			{
				OrderBookReceived?.Invoke(subscription, message);

				var legacy = MarketDepthReceived;

				if (legacy is not null && subscription.SecurityId is not null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					if (message is not MarketDepth md)
						md = ((QuoteChangeMessage)message).ToMarketDepth(LookupById(subscription.SecurityId.Value));
#pragma warning restore CS0618 // Type or member is obsolete

					legacy(subscription, md);
				}
			}
		}

		private void OnConnectorOrderLogReceived(Subscription subscription, IOrderLogMessage message)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
			{
				OrderLogReceived?.Invoke(subscription, message);

				var legacy = OrderLogItemReceived;

				if (legacy is not null && subscription.SecurityId is not null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					if (message is not OrderLogItem ol)
						ol = ((ExecutionMessage)message).ToOrderLog(LookupById(subscription.SecurityId.Value));
#pragma warning restore CS0618 // Type or member is obsolete

					legacy(subscription, ol);
				}
			}
		}

		private void OnConnectorLevel1Received(Subscription subscription, Level1ChangeMessage message)
		{
			if (!IsDisposeStarted && _subscriptions.ContainsKey(subscription))
				Level1Received?.Invoke(subscription, message);
		}
	}
}
