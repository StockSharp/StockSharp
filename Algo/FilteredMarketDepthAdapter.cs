namespace StockSharp.Algo;

/// <summary>
/// Filtered market depth adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FilteredMarketDepthAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class FilteredMarketDepthAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	// Completion can race data already queued by the inner adapter. Remember enough recently closed
	// subscriptions to consume that tail without retaining every transaction id for the adapter's lifetime.
	private const int MaxInactiveIds = 1_024;

	private sealed class RecentIdSet(int capacity)
	{
		private readonly Dictionary<long, LinkedListNode<long>> _nodes = [];
		private readonly LinkedList<long> _order = [];

		public int Count => _nodes.Count;

		public bool Contains(long id) => _nodes.ContainsKey(id);

		public void Add(long id)
		{
			if (_nodes.TryGetValue(id, out var existing))
			{
				_order.Remove(existing);
				_order.AddLast(existing);
				return;
			}

			var node = _order.AddLast(id);
			_nodes.Add(id, node);

			if (_nodes.Count <= capacity)
				return;

			var oldest = _order.First;
			_order.RemoveFirst();
			_nodes.Remove(oldest.Value);
		}

		public void Remove(long id)
		{
			if (!_nodes.Remove(id, out var node))
				return;

			_order.Remove(node);
		}

		public void Clear()
		{
			_nodes.Clear();
			_order.Clear();
		}
	}

	private class FilteredMarketDepthInfo(long subscribeId, Subscription bookSubscription, Subscription ordersSubscription)
	{
		private readonly Dictionary<ValueTuple<Sides, decimal>, decimal> _totals = [];
		private readonly Dictionary<long, RefTriple<Sides, decimal, decimal?>> _ordersInfo = [];

		private QuoteChangeMessage _lastSnapshot;

		private void ChangeTotal(Sides side, decimal price, decimal delta)
		{
			var key = (side, price);
			_totals.TryGetValue(key, out var total);

			total += delta;

			if (total > 0)
				_totals[key] = total;
			else
				_totals.Remove(key);
		}

		private void SetOrder(long transactionId, Sides side, decimal price, decimal? balance)
		{
			if (_ordersInfo.TryGetValue(transactionId, out var previous) && previous.Third is decimal previousBalance)
				ChangeTotal(previous.First, previous.Second, -previousBalance);

			_ordersInfo[transactionId] = RefTuple.Create(side, price, balance);

			if (balance is decimal currentBalance && currentBalance > 0)
				ChangeTotal(side, price, currentBalance);
		}

		private bool RemoveOrder(long transactionId)
		{
			if (!_ordersInfo.Remove(transactionId, out var previous))
				return false;

			if (previous.Third is decimal previousBalance)
				ChangeTotal(previous.First, previous.Second, -previousBalance);

			return true;
		}

		public long SubscribeId { get; } = subscribeId;
		public long UnSubscribeId { get; set; }
		public bool BookDispatched { get; set; }
		public bool OrdersDispatched { get; set; }
		public bool SubscribeResponseSent { get; set; }
		public bool OnlineSent { get; set; }
		public int PendingUnsubscribeResponses { get; set; }
		public Exception UnsubscribeError { get; set; }
		public bool UnsubscribeResponseSent { get; set; }

		public Subscription BookSubscription { get; } = bookSubscription ?? throw new ArgumentNullException(nameof(bookSubscription));
		public Subscription OrdersSubscription { get; } = ordersSubscription ?? throw new ArgumentNullException(nameof(ordersSubscription));

		public OnlineInfo Online { get; set; }

		//public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;

		private QuoteChange[] Filter(Sides side, IEnumerable<QuoteChange> quotes)
		{
			return [.. quotes
				.Select(quote =>
				{
					if (_totals.TryGetValue((side, quote.Price), out var total))
						quote.Volume -= total;

					return quote;
				})
				.Where(q => q.Volume > 0)];
		}

		private QuoteChangeMessage CreateFilteredBook()
		{
			var book = new QuoteChangeMessage
			{
				SecurityId = _lastSnapshot.SecurityId,
				ServerTime = _lastSnapshot.ServerTime,
				LocalTime = _lastSnapshot.LocalTime,
				BuildFrom = _lastSnapshot.BuildFrom,
				Currency = _lastSnapshot.Currency,
				IsFiltered = true,
				Bids = Filter(Sides.Buy, _lastSnapshot.Bids),
				Asks = Filter(Sides.Sell, _lastSnapshot.Asks),
			};

			if (Online == null)
				book.SetSubscriptionIds(subscriptionId: SubscribeId);
			else
				book.SetSubscriptionIds(Online.Subscribers.Cache);

			return book;
		}

		public QuoteChangeMessage Process(QuoteChangeMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			_lastSnapshot = message.TypedClone();

			return CreateFilteredBook();
		}

		public void AddOrder(OrderRegisterMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			SetOrder(message.TransactionId, message.Side, message.Price, message.Volume);
		}

		public QuoteChangeMessage Process(ExecutionMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (!message.HasOrderInfo)
				return null;

			if (message.TransactionId != 0)
			{
				if (message.OrderState is OrderStates.Done or OrderStates.Failed)
				{
					if (!RemoveOrder(message.TransactionId))
						return null;
				}
				else
				{
					if (message.OrderPrice == 0 || message.Balance is not decimal balance)
						return null;

					// The depth is keyed by side, so a row naming none belongs to neither half of it.
					if (message.Side is not Sides side)
						return null;

					SetOrder(message.TransactionId, side, message.OrderPrice, balance);
				}
			}
			else if (_ordersInfo.TryGetValue(message.OriginalTransactionId, out var key))
			{
				switch (message.OrderState)
				{
					case OrderStates.Done:
					case OrderStates.Failed:
						RemoveOrder(message.OriginalTransactionId);
						break;

					case OrderStates.Active:
					{
						if (message.Balance is not decimal newBalance)
							return null;

						if (key.Third == newBalance)
							return null;

						SetOrder(message.OriginalTransactionId, key.First, key.Second, newBalance);

						break;
					}
				}
			}
			else
				return null;

			return _lastSnapshot is null ? null : CreateFilteredBook();
		}
	}

	private class OnlineInfo
	{
		public readonly CachedSynchronizedSet<long> Subscribers = [];

		public readonly CachedSynchronizedSet<long> BookSubscribers = [];
		public readonly CachedSynchronizedSet<long> OrdersSubscribers = [];
	}

	private readonly Lock _sync = new();

	private readonly Dictionary<long, FilteredMarketDepthInfo> _byId = [];
	private readonly Dictionary<long, FilteredMarketDepthInfo> _byBookId = [];
	private readonly Dictionary<long, FilteredMarketDepthInfo> _byOrderStatusId = [];
	private readonly Dictionary<SecurityId, OnlineInfo> _online = [];
	private readonly Dictionary<long, (FilteredMarketDepthInfo info, bool isOrderBook)> _unsubscribeRequests = [];
	private readonly RecentIdSet _inactiveBookIds = new(MaxInactiveIds);
	private readonly RecentIdSet _inactiveOrderStatusIds = new(MaxInactiveIds);

	private void RegisterUnsubscribe(FilteredMarketDepthInfo info, long requestId, bool isOrderBook)
	{
		_unsubscribeRequests.Add(requestId, (info, isOrderBook));
		info.PendingUnsubscribeResponses++;
	}

	private void Deactivate(FilteredMarketDepthInfo info)
	{
		_byId.Remove(info.SubscribeId);

		var bookId = info.BookSubscription.TransactionId;
		var orderStatusId = info.OrdersSubscription.TransactionId;

		_byBookId.Remove(bookId);
		_byOrderStatusId.Remove(orderStatusId);
		_inactiveBookIds.Add(bookId);
		_inactiveOrderStatusIds.Add(orderStatusId);

		var online = info.Online;

		if (online is null)
			return;

		online.BookSubscribers.Remove(bookId);
		online.OrdersSubscribers.Remove(orderStatusId);
		online.Subscribers.Remove(info.SubscribeId);
		info.Online = null;

		if (online.Subscribers.Count == 0)
			_online.Remove(info.BookSubscription.SecurityId.Value);
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		void AddInfo(OrderRegisterMessage regMsg)
		{
			if (regMsg is null)
				throw new ArgumentNullException(nameof(regMsg));

			if (regMsg.OrderType == OrderTypes.Market || regMsg.Price == 0)
				return;

			if (regMsg.TimeInForce is TimeInForce.MatchOrCancel or TimeInForce.CancelBalance)
				return;

			using (_sync.EnterScope())
			{
				foreach (var info in _byId.Values)
				{
					if (info.BookSubscription.SecurityId == regMsg.SecurityId)
						info.AddOrder(regMsg);
				}
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_sync.EnterScope())
				{
					_byId.Clear();
					_byBookId.Clear();
					_byOrderStatusId.Clear();
					_online.Clear();
					_unsubscribeRequests.Clear();
					_inactiveBookIds.Clear();
					_inactiveOrderStatusIds.Clear();
				}

				break;
			}

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			{
				AddInfo((OrderRegisterMessage)message);
				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.SecurityId == default)
						break;

					if (mdMsg.DataType2 != DataType.FilteredMarketDepth)
						break;

					var transId = mdMsg.TransactionId;

					mdMsg = mdMsg.TypedClone();
					mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
					mdMsg.DataType2 = DataType.MarketDepth;

					var orderStatus = new OrderStatusMessage
					{
						TransactionId = TransactionIdGenerator.GetNextId(),
						IsSubscribe = true,
						States = [OrderStates.Active],
						SecurityId = mdMsg.SecurityId,
					};

					var info = new FilteredMarketDepthInfo(transId, new Subscription(mdMsg, mdMsg), new Subscription(orderStatus, orderStatus))
					{
						BookDispatched = true,
					};

					using (_sync.EnterScope())
					{
						// Transaction ids are normally monotonic, but a custom generator may reuse one after
						// a reset. An active subscription always takes precedence over an old tombstone.
						_inactiveBookIds.Remove(mdMsg.TransactionId);
						_inactiveOrderStatusIds.Remove(orderStatus.TransactionId);
						_byId.Add(transId, info);
						_byBookId.Add(mdMsg.TransactionId, info);
						_byOrderStatusId.Add(orderStatus.TransactionId, info);
					}

					LogInfo("Filtered book {0} started (Book={1} / Orders={2}).", transId, mdMsg.TransactionId, orderStatus.TransactionId);

					await base.OnSendInMessageAsync(mdMsg, cancellationToken);

					var dispatchOrders = false;

					using (_sync.EnterScope())
					{
						dispatchOrders = _byId.TryGetValue(transId, out var activeInfo) && ReferenceEquals(activeInfo, info);

						if (dispatchOrders)
							info.OrdersDispatched = true;
					}

					if (dispatchOrders)
						await base.OnSendInMessageAsync(orderStatus, cancellationToken);

					return;
				}
				else
				{
					MarketDataMessage bookUnsubscribe = null;
					OrderStatusMessage ordersUnsubscribe = null;

					using (_sync.EnterScope())
					{
						if (!_byId.TryGetValue(mdMsg.OriginalTransactionId, out var info))
							break;

						info.UnSubscribeId = mdMsg.TransactionId;

						if (info.BookDispatched && info.BookSubscription.State is not SubscriptionStates.Error and not SubscriptionStates.Finished)
						{
							bookUnsubscribe = new MarketDataMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								OriginalTransactionId = info.BookSubscription.TransactionId,
								IsSubscribe = false,
							};

							RegisterUnsubscribe(info, bookUnsubscribe.TransactionId, true);
						}

						if (info.OrdersDispatched && info.OrdersSubscription.State is not SubscriptionStates.Error and not SubscriptionStates.Finished)
						{
							ordersUnsubscribe = new OrderStatusMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								OriginalTransactionId = info.OrdersSubscription.TransactionId,
								IsSubscribe = false,
							};

							RegisterUnsubscribe(info, ordersUnsubscribe.TransactionId, false);
						}

						if (bookUnsubscribe != null || ordersUnsubscribe != null)
							Deactivate(info);
					}

					if (bookUnsubscribe == null && ordersUnsubscribe == null)
					{
						await RaiseNewOutMessageAsync(new SubscriptionResponseMessage
						{
							OriginalTransactionId = mdMsg.TransactionId,
							Error = new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(mdMsg.OriginalTransactionId)),
						}, cancellationToken);
					}
					else
					{
						LogInfo("Filtered book {0} unsubscribing.", mdMsg.OriginalTransactionId);

						if (bookUnsubscribe != null)
							await base.OnSendInMessageAsync(bookUnsubscribe, cancellationToken);

						if (ordersUnsubscribe != null)
							await base.OnSendInMessageAsync(ordersUnsubscribe, cancellationToken);
					}

					return;
				}
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		Message cleanupRequest = null;
		Message additionalStateMessage = null;

		Message TryApplyState(IOriginalTransactionIdMessage msg, SubscriptionStates state)
		{
			Message TryCompleteStart(FilteredMarketDepthInfo info)
			{
				var book = info.BookSubscription;
				var orders = info.OrdersSubscription;
				Message result = null;

				if (!info.SubscribeResponseSent && book.State.IsActive() && orders.State.IsActive())
				{
					info.SubscribeResponseSent = true;
					result = new SubscriptionResponseMessage { OriginalTransactionId = info.SubscribeId };
				}

				if (!info.OnlineSent && book.State == SubscriptionStates.Online && orders.State == SubscriptionStates.Online)
				{
					var online = _online.SafeAdd(book.SecurityId.Value);

					online.Subscribers.Add(info.SubscribeId);
					online.BookSubscribers.Add(book.TransactionId);
					online.OrdersSubscribers.Add(orders.TransactionId);

					info.Online = online;
					info.OnlineSent = true;

					var onlineMessage = new SubscriptionOnlineMessage { OriginalTransactionId = info.SubscribeId };

					if (result is null)
						result = onlineMessage;
					else
						additionalStateMessage = onlineMessage;
				}

				return result;
			}

			Message CompleteParent(FilteredMarketDepthInfo info, bool failedBook)
			{
				var counterpart = failedBook ? info.OrdersSubscription : info.BookSubscription;
				var counterpartDispatched = failedBook ? info.OrdersDispatched : info.BookDispatched;

				if (counterpartDispatched && counterpart.State is not SubscriptionStates.Error and not SubscriptionStates.Finished)
				{
					if (failedBook)
					{
						var unsubscribe = new OrderStatusMessage
						{
							TransactionId = TransactionIdGenerator.GetNextId(),
							OriginalTransactionId = counterpart.TransactionId,
							IsSubscribe = false,
						};

						RegisterUnsubscribe(info, unsubscribe.TransactionId, false);
						cleanupRequest = unsubscribe;
					}
					else
					{
						var unsubscribe = new MarketDataMessage
						{
							TransactionId = TransactionIdGenerator.GetNextId(),
							OriginalTransactionId = counterpart.TransactionId,
							IsSubscribe = false,
						};

						RegisterUnsubscribe(info, unsubscribe.TransactionId, true);
						cleanupRequest = unsubscribe;
					}
				}

				Deactivate(info);

				return state switch
				{
					SubscriptionStates.Error => new SubscriptionResponseMessage { OriginalTransactionId = info.SubscribeId, Error = (msg as IErrorMessage)?.Error },
					SubscriptionStates.Finished => new SubscriptionFinishedMessage { OriginalTransactionId = info.SubscribeId },
					_ => null,
				};
			}

			var id = msg.OriginalTransactionId;

			using (_sync.EnterScope())
			{
				if (_byBookId.TryGetValue(id, out var info))
				{
					var book = info.BookSubscription;

					book.State = book.State.ChangeSubscriptionState(state, id, this);

					if (!state.IsActive())
						return CompleteParent(info, true);

					return TryCompleteStart(info);
				}
				else if (_byOrderStatusId.TryGetValue(id, out info))
				{
					info.OrdersSubscription.State = info.OrdersSubscription.State.ChangeSubscriptionState(state, id, this);

					if (!state.IsActive())
						return CompleteParent(info, false);

					return TryCompleteStart(info);
				}
				else if (_unsubscribeRequests.TryGetAndRemove(id, out var tuple))
				{
					info = tuple.info;
					var error = (msg as IErrorMessage)?.Error;

					if (tuple.isOrderBook)
					{
						var book = info.BookSubscription;
						book.State = book.State.ChangeSubscriptionState(SubscriptionStates.Stopped, book.TransactionId, this);
						_inactiveBookIds.Add(id);
					}
					else
					{
						var orders = info.OrdersSubscription;
						orders.State = orders.State.ChangeSubscriptionState(SubscriptionStates.Stopped, orders.TransactionId, this);
						_inactiveOrderStatusIds.Add(id);
					}

					info.UnsubscribeError ??= error;
					info.PendingUnsubscribeResponses--;

					if (info.UnSubscribeId == 0 || info.PendingUnsubscribeResponses > 0 || info.UnsubscribeResponseSent)
						return null;

					info.UnsubscribeResponseSent = true;

					return new SubscriptionResponseMessage
					{
						OriginalTransactionId = info.UnSubscribeId,
						Error = info.UnsubscribeError,
					};
				}
				else if (_inactiveBookIds.Contains(id) || _inactiveOrderStatusIds.Contains(id))
					return null;
				else
					return (Message)msg;
			}
		}

		List<QuoteChangeMessage> filtered = null;

		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				message = TryApplyState(responseMsg, responseMsg.IsOk() ? SubscriptionStates.Active : SubscriptionStates.Error);
				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				message = TryApplyState((SubscriptionFinishedMessage)message, SubscriptionStates.Finished);
				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				message = TryApplyState((SubscriptionOnlineMessage)message, SubscriptionStates.Online);
				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					break;

				HashSet<long> leftIds = null;

				using (_sync.EnterScope())
				{
					if (_byBookId.Count == 0 && _inactiveBookIds.Count == 0)
						break;

					var ids = quoteMsg.GetSubscriptionIds();
					HashSet<long> processed = null;

					foreach (var id in ids)
					{
						if (_inactiveBookIds.Contains(id))
						{
							leftIds ??= [.. ids];
							leftIds.Remove(id);
							continue;
						}

						if (processed != null && processed.Contains(id))
							continue;

						if (!_byBookId.TryGetValue(id, out var info))
							continue;

						var book = info.Process(quoteMsg);

						leftIds ??= [.. ids];

						if (info.Online is null)
							leftIds.Remove(id);
						else
						{
							processed ??= [];

							processed.AddRange(info.Online.BookSubscribers.Cache);
							leftIds.RemoveRange(info.Online.BookSubscribers.Cache);
						}

						filtered ??= [];

						filtered.Add(book);
					}
				}

				if (leftIds is null)
					break;
				else if (leftIds.Count == 0)
					message = null;
				else
					quoteMsg.SetSubscriptionIds([.. leftIds]);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					break;

				HashSet<long> leftIds = null;

				using (_sync.EnterScope())
				{
					if (_byOrderStatusId.Count == 0 && _inactiveOrderStatusIds.Count == 0)
						break;

					var ids = execMsg.GetSubscriptionIds();
					HashSet<long> processed = null;

					foreach (var id in ids)
					{
						if (_inactiveOrderStatusIds.Contains(id))
						{
							leftIds ??= [.. ids];
							leftIds.Remove(id);
							continue;
						}

						if (processed != null && processed.Contains(id))
							continue;

						if (!_byOrderStatusId.TryGetValue(id, out var info))
							continue;

						leftIds ??= [.. ids];

						if (info.Online is null)
							leftIds.Remove(id);
						else
						{
							processed ??= [];

							processed.AddRange(info.Online.OrdersSubscribers.Cache);
							leftIds.RemoveRange(info.Online.OrdersSubscribers.Cache);
						}

						filtered ??= [];

						var book = info.Process(execMsg);

						if (book is not null)
							filtered.Add(book);
					}
				}

				if (leftIds is null)
					break;
				else if (leftIds.Count == 0)
					message = null;
				else
					execMsg.SetSubscriptionIds([.. leftIds]);

				break;
			}
		}

		if (message != null)
			await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (additionalStateMessage != null)
			await base.OnInnerAdapterNewOutMessageAsync(additionalStateMessage, cancellationToken);

		if (cleanupRequest != null)
			await base.OnSendInMessageAsync(cleanupRequest, cancellationToken);

		if (filtered != null)
		{
			foreach (var book in filtered)
				await base.OnInnerAdapterNewOutMessageAsync(book, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="FilteredMarketDepthAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new FilteredMarketDepthAdapter(InnerAdapter.TypedClone());
}
