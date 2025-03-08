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
	private class SubscriptionInfo(IBasketSecurityProcessor processor, long transactionId)
	{
		public IBasketSecurityProcessor Processor { get; } = processor ?? throw new ArgumentNullException(nameof(processor));
		public long TransactionId { get; } = transactionId;
		public CachedSynchronizedDictionary<long, SubscriptionStates> LegsSubscriptions { get; } = [];

		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
	}

	private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionsByChildId = [];
	private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionsByParentId = [];

	private readonly ISecurityProvider _securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
	private readonly IBasketSecurityProcessorProvider _processorProvider = processorProvider ?? throw new ArgumentNullException(nameof(processorProvider));
	private readonly IExchangeInfoProvider _exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_subscriptionsByChildId.Clear();
				_subscriptionsByParentId.Clear();
				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

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
					var info = new SubscriptionInfo(processor, mdMsg.TransactionId);

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
					}

					foreach (var inner in inners)
						base.OnSendInMessage(inner);
				}
				else
				{
					if (!_subscriptionsByParentId.TryGetValue(mdMsg.OriginalTransactionId, out var info))
						break;

					// TODO
					//_subscriptionsByParentId.Remove(mdMsg.OriginalTransactionId);

					foreach (var id in info.LegsSubscriptions.CachedKeys)
					{
						base.OnSendInMessage(new MarketDataMessage
						{
							TransactionId = TransactionIdGenerator.GetNextId(),
							IsSubscribe = false,
							OriginalTransactionId = id
						});
					}
				}

				RaiseNewOutMessage(new SubscriptionResponseMessage
				{
					OriginalTransactionId = mdMsg.TransactionId
				});

				return true;
			}
		}

		return base.OnSendInMessage(message);
	}

	private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
	{
		info.State = info.State.ChangeSubscriptionState(state, info.TransactionId, this);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				
				if (_subscriptionsByChildId.TryGetValue(responseMsg.OriginalTransactionId, out var info))
				{
					lock (info.LegsSubscriptions.SyncRoot)
					{
						if (responseMsg.IsOk())
						{
							info.LegsSubscriptions[responseMsg.OriginalTransactionId] = SubscriptionStates.Active;

							if (info.State != SubscriptionStates.Active)
								ChangeState(info, SubscriptionStates.Active);
						}
						else
						{
							info.LegsSubscriptions[responseMsg.OriginalTransactionId] = SubscriptionStates.Error;

							if (info.State != SubscriptionStates.Error)
								ChangeState(info, SubscriptionStates.Error);
						}
					}
				}

				break;
			}

			case MessageTypes.SubscriptionOnline:
			case MessageTypes.SubscriptionFinished:
			{
				var originIdMsg = (IOriginalTransactionIdMessage)message;
				var id = originIdMsg.OriginalTransactionId;

				var state = message.Type == MessageTypes.SubscriptionOnline ? SubscriptionStates.Online : SubscriptionStates.Finished;

				if (_subscriptionsByChildId.TryGetValue(id, out var info))
				{
					lock (info.LegsSubscriptions.SyncRoot)
					{
						info.LegsSubscriptions[id] = state;

						if (info.LegsSubscriptions.CachedValues.All(s => s == state))
							ChangeState(info, state);
					}
				}

				break;
			}

			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg && _subscriptionsByChildId.Count > 0)
				{
					foreach (var id in subscrMsg.GetSubscriptionIds())
					{
						if (_subscriptionsByChildId.TryGetValue(id, out var info))
						{
							var basketMsgs = info.Processor.Process(message);

							foreach (var basketMsg in basketMsgs)
							{
								((ISubscriptionIdMessage)basketMsg).SetSubscriptionIds(subscriptionId: info.TransactionId);
								base.OnInnerAdapterNewOutMessage(basketMsg);
							}
						}
					}
				}

				break;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="BasketSecurityMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new BasketSecurityMessageAdapter(InnerAdapter, _securityProvider, _processorProvider, _exchangeInfoProvider);
	}
}