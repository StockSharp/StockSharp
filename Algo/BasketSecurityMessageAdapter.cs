namespace StockSharp.Algo;

/// <summary>
/// The messages adapter builds market data for basket securities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BasketSecurityMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
/// <param name="securityProvider">The provider of information about instruments.</param>
/// <param name="processorProvider">Basket security processors provider.</param>
/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
public class BasketSecurityMessageAdapter(IMessageAdapter innerAdapter, ISecurityProvider securityProvider, IBasketSecurityProcessorProvider processorProvider, IExchangeInfoProvider exchangeInfoProvider) : MessageAdapterWrapper(innerAdapter)
{
	private class SubscriptionInfo(IBasketSecurityProcessor processor, MarketDataMessage subscription)
	{
		public IBasketSecurityProcessor Processor { get; } = processor ?? throw new ArgumentNullException(nameof(processor));
		public MarketDataMessage Subscription { get; } = subscription ?? throw new ArgumentNullException(nameof(subscription));
		public long TransactionId => Subscription.TransactionId;
		public CachedSynchronizedDictionary<long, SubscriptionStates> LegsSubscriptions { get; } = [];
		public HashSet<long> RespondedLegs { get; } = [];

		public bool ResponseSent { get; set; }
		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
		public DateTime? NextFrom { get; set; }
		public byte[] FinishedBody { get; set; } = [];
	}

	private class UnsubscriptionInfo(long transactionId, long[] childIds)
	{
		public long TransactionId { get; } = transactionId;
		public long[] ChildIds { get; } = childIds ?? throw new ArgumentNullException(nameof(childIds));
		public HashSet<long> PendingIds { get; } = [.. childIds];
		public List<Exception> Errors { get; } = [];
		public Lock Sync { get; } = new();
	}

	private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionsByChildId = [];
	private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionsByParentId = [];
	private readonly SynchronizedDictionary<long, UnsubscriptionInfo> _unsubscriptionsByChildId = [];
	private readonly SynchronizedSet<long> _internalIds = [];

	private readonly ISecurityProvider _securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
	private readonly IBasketSecurityProcessorProvider _processorProvider = processorProvider ?? throw new ArgumentNullException(nameof(processorProvider));
	private readonly IExchangeInfoProvider _exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_subscriptionsByChildId.Clear();
				_subscriptionsByParentId.Clear();
				_unsubscriptionsByChildId.Clear();
				_internalIds.Clear();
				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				// An unsubscribe is identified by the original transaction. Its SecurityId is optional,
				// so resolve it before trying to reconstruct the basket from the request fields.
				if (!mdMsg.IsSubscribe && _subscriptionsByParentId.TryGetAndRemove(mdMsg.OriginalTransactionId, out var unsubscribed))
				{
					RemoveChildMappings(unsubscribed);

					var childMessages = CreateLegUnsubscribeMessages(unsubscribed, mdMsg);

					if (childMessages.Length == 0)
					{
						await RaiseNewOutMessageAsync(mdMsg.TransactionId.CreateSubscriptionResponse(), cancellationToken);
						return;
					}

					var unsubscribe = new UnsubscriptionInfo(mdMsg.TransactionId, [.. childMessages.Select(m => m.TransactionId)]);

					foreach (var child in childMessages)
						_unsubscriptionsByChildId.Add(child.TransactionId, unsubscribe);

					await childMessages.Select(child => base.OnSendInMessageAsync(child, cancellationToken)).WhenAll();
					return;
				}

				if (mdMsg.SecurityId == default)
					break;

				var security = _securityProvider.LookupById(mdMsg.SecurityId);

				if (security == null)
				{
					if (!mdMsg.IsBasket())
						break;
			
					security = mdMsg.ToSecurity(_exchangeInfoProvider).ToBasket(_processorProvider);
				}
				else if (!security.IsBasket())
					break;

				if (mdMsg.IsSubscribe)
				{
					var processor = _processorProvider.CreateProcessor(security);
					var info = new SubscriptionInfo(processor, mdMsg.TypedClone());

					_subscriptionsByParentId.Add(mdMsg.TransactionId, info);

					var inners = new MarketDataMessage[processor.BasketLegs.Length];

					for (var i = 0; i < inners.Length; i++)
					{
						var inner = mdMsg.TypedClone();

						inner.TransactionId = TransactionIdGenerator.GetNextId();
						inner.SecurityId = processor.BasketLegs[i];

						inners[i] = inner;

						info.LegsSubscriptions.Add(inner.TransactionId, SubscriptionStates.Stopped);
						_subscriptionsByChildId.Add(inner.TransactionId, info);
						_internalIds.Add(inner.TransactionId);
					}

					await inners.Select(inner => base.OnSendInMessageAsync(inner, cancellationToken)).WhenAll();

					// The client is answered under its own transaction once the venue has answered
					// for the legs, not before: until then it is not known whether the basket can
					// be served at all.
					return;
				}

				break;
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	private MarketDataMessage[] CreateLegUnsubscribeMessages(SubscriptionInfo info, MarketDataMessage template)
	{
		var messages = new MarketDataMessage[info.LegsSubscriptions.Count];
		var index = 0;

		foreach (var childId in info.LegsSubscriptions.CachedKeys)
		{
			var child = template.TypedClone();

			child.TransactionId = TransactionIdGenerator.GetNextId();
			child.OriginalTransactionId = childId;
			child.IsSubscribe = false;

			messages[index++] = child;
			_internalIds.Add(child.TransactionId);
		}

		return messages;
	}

	private void RemoveChildMappings(SubscriptionInfo info)
	{
		foreach (var childId in info.LegsSubscriptions.CachedKeys)
			_subscriptionsByChildId.Remove(childId);
	}

	private void RemoveSubscription(SubscriptionInfo info)
	{
		_subscriptionsByParentId.Remove(info.TransactionId);
		RemoveChildMappings(info);
	}

	private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
	{
		info.State = info.State.ChangeSubscriptionState(state, info.TransactionId, this);
	}

	private List<Message> TryCreateParentMessages(SubscriptionInfo info, out bool isFinished)
	{
		isFinished = false;
		List<Message> messages = null;

		if (!info.ResponseSent && info.RespondedLegs.Count == info.LegsSubscriptions.Count)
		{
			info.ResponseSent = true;
			ChangeState(info, SubscriptionStates.Active);
			(messages ??= []).Add(info.TransactionId.CreateSubscriptionResponse());
		}

		if (!info.ResponseSent || info.State == SubscriptionStates.Error)
			return messages;

		var states = info.LegsSubscriptions.CachedValues;

		if (info.State != SubscriptionStates.Finished && states.All(s => s == SubscriptionStates.Finished))
		{
			ChangeState(info, SubscriptionStates.Finished);
			(messages ??= []).Add(new SubscriptionFinishedMessage
			{
				OriginalTransactionId = info.TransactionId,
				NextFrom = info.NextFrom,
				Body = info.FinishedBody,
			});
			isFinished = true;
		}
		else if (info.State != SubscriptionStates.Online && states.All(s => s == SubscriptionStates.Online))
		{
			ChangeState(info, SubscriptionStates.Online);
			(messages ??= []).Add(new SubscriptionOnlineMessage { OriginalTransactionId = info.TransactionId });
		}

		return messages;
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var id = responseMsg.OriginalTransactionId;

				if (_subscriptionsByChildId.TryGetValue(id, out var info))
				{
					List<Message> parentMessages = null;
					var failed = false;
					var finished = false;

					using (info.LegsSubscriptions.EnterScope())
					{
						if (info.RespondedLegs.Add(id))
						{
							if (responseMsg.IsOk())
							{
								if (info.LegsSubscriptions[id] == SubscriptionStates.Stopped)
									info.LegsSubscriptions[id] = SubscriptionStates.Active;

								parentMessages = TryCreateParentMessages(info, out finished);
							}
							else
							{
								info.LegsSubscriptions[id] = SubscriptionStates.Error;
								info.ResponseSent = true;
								ChangeState(info, SubscriptionStates.Error);
								parentMessages = [new SubscriptionResponseMessage
								{
									OriginalTransactionId = info.TransactionId,
									Error = responseMsg.Error,
								}];
								failed = true;
							}
						}
					}

					if (failed || finished)
						RemoveSubscription(info);

					if (parentMessages != null)
					{
						foreach (var parentMessage in parentMessages)
							await RaiseNewOutMessageAsync(parentMessage, cancellationToken);
					}

					if (failed)
					{
						var cleanup = CreateLegUnsubscribeMessages(info, info.Subscription);
						await cleanup.Select(child => base.OnSendInMessageAsync(child, cancellationToken)).WhenAll();
					}

					return;
				}

				if (_unsubscriptionsByChildId.TryGetValue(id, out var unsubscribe))
				{
					var completed = false;
					Exception error = null;

					using (unsubscribe.Sync.EnterScope())
					{
						if (unsubscribe.PendingIds.Remove(id))
						{
							if (!responseMsg.IsOk())
								unsubscribe.Errors.Add(responseMsg.Error);

							completed = unsubscribe.PendingIds.Count == 0;

							if (completed && unsubscribe.Errors.Count > 0)
								error = unsubscribe.Errors.ToArray().SingleOrAggr();
						}
					}

					if (completed)
					{
						foreach (var childId in unsubscribe.ChildIds)
							_unsubscriptionsByChildId.Remove(childId);

						await RaiseNewOutMessageAsync(unsubscribe.TransactionId.CreateSubscriptionResponse(error), cancellationToken);
					}

					return;
				}

				if (_internalIds.Contains(id))
					return;

				break;
			}

			case MessageTypes.SubscriptionOnline:
			case MessageTypes.SubscriptionFinished:
			{
				var originIdMsg = (IOriginalTransactionIdMessage)message;
				var id = originIdMsg.OriginalTransactionId;

				if (_subscriptionsByChildId.TryGetValue(id, out var info))
				{
					List<Message> parentMessages;
					var finished = false;

					using (info.LegsSubscriptions.EnterScope())
					{
						if (message is SubscriptionFinishedMessage finishedMessage)
						{
							info.LegsSubscriptions[id] = SubscriptionStates.Finished;
							info.NextFrom = finishedMessage.NextFrom;
							info.FinishedBody = finishedMessage.Body;
						}
						else
							info.LegsSubscriptions[id] = SubscriptionStates.Online;

						parentMessages = TryCreateParentMessages(info, out finished);
					}

					if (finished)
						RemoveSubscription(info);

					if (parentMessages != null)
					{
						foreach (var parentMessage in parentMessages)
							await RaiseNewOutMessageAsync(parentMessage, cancellationToken);
					}

					return;
				}

				if (_internalIds.Contains(id))
					return;

				break;
			}

			default:
			{
				if (message is not ISubscriptionIdMessage subscrMsg)
					break;

				var ids = subscrMsg.GetSubscriptionIds();

				if (ids.Length == 0)
					break;

				var publicIds = new List<long>(ids.Length);
				HashSet<SubscriptionInfo> processed = null;
				List<(Message message, SubscriptionInfo info)> basketMessages = null;
				var hasInternalIds = false;

				foreach (var id in ids)
				{
					if (_subscriptionsByChildId.TryGetValue(id, out var info))
					{
						hasInternalIds = true;

						if ((processed ??= []).Add(info))
						{
							foreach (var basketMessage in info.Processor.Process(message))
								(basketMessages ??= []).Add((basketMessage, info));
						}
					}
					else if (_internalIds.Contains(id))
						hasInternalIds = true;
					else
						publicIds.Add(id);
				}

				if (!hasInternalIds)
					break;

				if (basketMessages != null)
				{
					foreach (var (basketMessage, info) in basketMessages)
					{
						if (basketMessage is ISubscriptionIdMessage basketSubscription)
						{
							basketSubscription.SetSubscriptionIds(subscriptionId: info.TransactionId);

							if (_internalIds.Contains(basketSubscription.OriginalTransactionId))
								basketSubscription.OriginalTransactionId = info.TransactionId;
						}

						await base.OnInnerAdapterNewOutMessageAsync(basketMessage, cancellationToken);
					}
				}

				if (publicIds.Count == 0)
					return;

				subscrMsg.SetSubscriptionIds([.. publicIds]);

				if (_internalIds.Contains(subscrMsg.OriginalTransactionId))
					subscrMsg.OriginalTransactionId = publicIds[0];

				break;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="BasketSecurityMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		// A copy is a second adapter and needs a link of its own: sharing the original's would
		// make the two answer each other's legs over one connection.
		return new BasketSecurityMessageAdapter(InnerAdapter.TypedClone(), _securityProvider, _processorProvider, _exchangeInfoProvider);
	}
}
