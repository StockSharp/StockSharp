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
	private class FilteredMarketDepthInfo(long subscribeId, Subscription bookSubscription, Subscription ordersSubscription)
	{
		private readonly Dictionary<ValueTuple<Sides, decimal>, decimal> _totals = [];
		private readonly Dictionary<long, RefTriple<Sides, decimal, decimal?>> _ordersInfo = [];

		private QuoteChangeMessage _lastSnapshot;

		public long SubscribeId { get; } = subscribeId;
		public long UnSubscribeId { get; set; }

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

			_ordersInfo[message.TransactionId] = RefTuple.Create(message.Side, message.Price, (decimal?)message.Volume);

			var valKey = (message.Side, message.Price);

			_totals.TryGetValue(valKey, out var total);
			total += message.Volume;
			_totals[valKey] = total;
		}

		public QuoteChangeMessage Process(ExecutionMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (!message.HasOrderInfo)
				return null;

			if (message.TransactionId != 0)
			{
				if (message.OrderState is not OrderStates.Done and not OrderStates.Failed)
				{
					if (message.OrderPrice == 0)
						return null;

					var balance = message.Balance;

					_ordersInfo[message.TransactionId] = RefTuple.Create(message.Side, message.OrderPrice, balance);

					if (balance == null)
						return null;

					var valKey = (message.Side, message.OrderPrice);

					if (_totals.TryGetValue(valKey, out var total))
					{
						total += balance.Value;
						_totals[valKey] = total;
					}
					else
						_totals.Add(valKey, balance.Value);
				}
			}
			else if (_ordersInfo.TryGetValue(message.OriginalTransactionId, out var key))
			{
				var valKey = (key.First, key.Second);

				switch (message.OrderState)
				{
					case OrderStates.Done:
					case OrderStates.Failed:
					{
						_ordersInfo.Remove(message.OriginalTransactionId);

						var balance = key.Third;

						if (balance == null)
							return null;

						if (!_totals.TryGetValue(valKey, out var total))
							return null;

						total -= balance.Value;

						if (total > 0)
							_totals[valKey] = total;
						else
							_totals.Remove(valKey);

						break;
					}

					case OrderStates.Active:
					{
						var newBalance = message.Balance;

						if (newBalance == null)
							return null;

						var prevBalance = key.Third;
						key.Third = newBalance;

						if (prevBalance == null)
						{
							if (_totals.TryGetValue(valKey, out var total))
							{
								total += newBalance.Value;
								_totals[valKey] = total;
							}
							else
								_totals.Add(valKey, newBalance.Value);
						}
						else
						{
							if (_totals.TryGetValue(valKey, out var total))
							{
								var delta = prevBalance.Value - newBalance.Value;

								if (delta == 0)
									return null;

								total -= delta;

								if (total > 0)
									_totals[valKey] = total;
								else
									_totals.Remove(valKey);
							}
							else if (newBalance > 0)
								_totals.Add(valKey, newBalance.Value);
						}

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

					var info = new FilteredMarketDepthInfo(transId, new Subscription(mdMsg, mdMsg), new Subscription(orderStatus, orderStatus));

					using (_sync.EnterScope())
					{
						_byId.Add(transId, info);
						_byBookId.Add(mdMsg.TransactionId, info);
						_byOrderStatusId.Add(orderStatus.TransactionId, info);
					}

					LogInfo("Filtered book {0} started (Book={1} / Orders={2}).", transId, mdMsg.TransactionId, orderStatus.TransactionId);

					await base.OnSendInMessageAsync(mdMsg, cancellationToken);
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

						if (info.BookSubscription.State.IsActive())
						{
							bookUnsubscribe = new MarketDataMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								OriginalTransactionId = info.BookSubscription.TransactionId,
								IsSubscribe = false,
							};

							_unsubscribeRequests.Add(bookUnsubscribe.TransactionId, (info, true));
						}

						if (info.OrdersSubscription.State.IsActive())
						{
							ordersUnsubscribe = new OrderStatusMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								OriginalTransactionId = info.OrdersSubscription.TransactionId,
								IsSubscribe = false,
							};

							_unsubscribeRequests.Add(ordersUnsubscribe.TransactionId, (info, false));
						}
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
		Message TryApplyState(IOriginalTransactionIdMessage msg, SubscriptionStates state)
		{
			void TryCheckOnline(FilteredMarketDepthInfo info)
			{
				if (state != SubscriptionStates.Online)
					return;

				var book = info.BookSubscription;
				var orders = info.OrdersSubscription;

				if (info.BookSubscription.State == SubscriptionStates.Online && orders.State == SubscriptionStates.Online)
				{
					var online = _online.SafeAdd(book.SecurityId.Value);

					online.Subscribers.Add(info.SubscribeId);
					online.BookSubscribers.Add(book.TransactionId);
					online.OrdersSubscribers.Add(orders.TransactionId);

					info.Online = online;
				}
			}

			var id = msg.OriginalTransactionId;

			using (_sync.EnterScope())
			{
				if (_byBookId.TryGetValue(id, out var info))
				{
					var book = info.BookSubscription;

					book.State = book.State.ChangeSubscriptionState(state, id, this);

					var subscribeId = info.SubscribeId;

					if (!state.IsActive())
					{
						if (info.Online != null)
						{
							info.Online.BookSubscribers.Remove(id);
							info.Online.OrdersSubscribers.Remove(info.OrdersSubscription.TransactionId);

							info.Online.Subscribers.Remove(subscribeId);
							info.Online = null;
						}
					}
					else
						TryCheckOnline(info);

					switch (book.State)
					{
						case SubscriptionStates.Stopped:
						case SubscriptionStates.Active:
						case SubscriptionStates.Error:
							return new SubscriptionResponseMessage { OriginalTransactionId = subscribeId, Error = (msg as IErrorMessage)?.Error };
						case SubscriptionStates.Finished:
							return new SubscriptionFinishedMessage { OriginalTransactionId = subscribeId };
						case SubscriptionStates.Online:
							return new SubscriptionOnlineMessage { OriginalTransactionId = subscribeId };
						default:
							throw new ArgumentOutOfRangeException(book.State.ToString());
					}
				}
				else if (_byOrderStatusId.TryGetValue(id, out info))
				{
					info.OrdersSubscription.State = info.OrdersSubscription.State.ChangeSubscriptionState(state, id, this);

					if (!state.IsActive())
						info.Online?.OrdersSubscribers.Remove(id);
					else
						TryCheckOnline(info);

					return null;
				}
				else if (_unsubscribeRequests.TryGetAndRemove(id, out var tuple))
				{
					info = tuple.info;

					if (tuple.isOrderBook)
					{
						var book = info.BookSubscription;
						book.State = book.State.ChangeSubscriptionState(SubscriptionStates.Stopped, book.TransactionId, this);

						return new SubscriptionResponseMessage
						{
							OriginalTransactionId = info.UnSubscribeId,
							Error = (msg as IErrorMessage)?.Error,
						};
					}
					else
					{
						var orders = info.OrdersSubscription;
						orders.State = orders.State.ChangeSubscriptionState(SubscriptionStates.Stopped, orders.TransactionId, this);
						return null;
					}
				}
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
					if (_byBookId.Count == 0)
						break;

					var ids = quoteMsg.GetSubscriptionIds();
					HashSet<long> processed = null;

					foreach (var id in ids)
					{
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
					if (_byOrderStatusId.Count == 0)
						break;

					var ids = execMsg.GetSubscriptionIds();
					HashSet<long> processed = null;

					foreach (var id in ids)
					{
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
	public override IMessageAdapter Clone() => new FilteredMarketDepthAdapter(InnerAdapter);
}