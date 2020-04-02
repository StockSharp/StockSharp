namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter builds market data for basket securities.
	/// </summary>
	public class BasketSecurityMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionInfo
		{
			public IBasketSecurityProcessor Processor { get; }
			public long TransactionId { get; }
			public HashSet<long> LegsSubscriptions { get; } = new HashSet<long>();

			public SubscriptionInfo(IBasketSecurityProcessor processor, long transactionId)
			{
				Processor = processor ?? throw new ArgumentNullException(nameof(processor));
				TransactionId = transactionId;
			}
		}

		private readonly SynchronizedDictionary<MarketDataTypes, SynchronizedDictionary<long, SubscriptionInfo>> _subscriptions = new SynchronizedDictionary<MarketDataTypes, SynchronizedDictionary<long, SubscriptionInfo>>();
		private readonly SynchronizedDictionary<long, SubscriptionInfo> _subscriptionsById = new SynchronizedDictionary<long, SubscriptionInfo>();

		private readonly ISecurityProvider _securityProvider;
		private readonly IBasketSecurityProcessorProvider _processorProvider;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketSecurityMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public BasketSecurityMessageAdapter(IMessageAdapter innerAdapter, ISecurityProvider securityProvider, IBasketSecurityProcessorProvider processorProvider, IExchangeInfoProvider exchangeInfoProvider)
			: base(innerAdapter)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_processorProvider = processorProvider ?? throw new ArgumentNullException(nameof(processorProvider));
			_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptions.Clear();
					_subscriptionsById.Clear();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.SecurityId.IsDefault())
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

						var dict = _subscriptions.SafeAdd(mdMsg.DataType);
						_subscriptionsById.Add(mdMsg.TransactionId, info);

						var inners = new MarketDataMessage[processor.BasketLegs.Length];

						for (var i = 0; i < inners.Length; i++)
						{
							var inner = (MarketDataMessage)mdMsg.Clone();

							inner.TransactionId = TransactionIdGenerator.GetNextId();
							inner.SecurityId = processor.BasketLegs[i];

							inners[i] = inner;

							info.LegsSubscriptions.Add(inner.TransactionId);
							dict.Add(inner.TransactionId, info);
						}

						foreach (var inner in inners)
							base.OnSendInMessage(inner);
					}
					else
					{
						if (!_subscriptionsById.TryGetValue(mdMsg.OriginalTransactionId, out var info))
							break;

						_subscriptionsById.Remove(mdMsg.OriginalTransactionId);

						foreach (var id in info.LegsSubscriptions)
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

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;
					var info = _subscriptions.TryGetValue(MarketDataTypes.MarketDepth);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					SynchronizedDictionary<long, SubscriptionInfo> dict;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
							dict = _subscriptions.TryGetValue(MarketDataTypes.Trades);
							break;
						case ExecutionTypes.Transaction:
							dict = null;
							break;
						case ExecutionTypes.OrderLog:
							dict = _subscriptions.TryGetValue(MarketDataTypes.OrderLog);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (dict != null)
					{
						if (execMsg.OriginalTransactionId == 0)
						{

						}
						else
						{
							if (dict.TryGetValue(execMsg.OriginalTransactionId, out var info))
							{
								var basketMsgs = info.Processor.Process(execMsg).Cast<ExecutionMessage>();

								foreach (var basketMsg in basketMsgs)
								{
									basketMsg.OriginalTransactionId = info.TransactionId;
									base.OnInnerAdapterNewOutMessage(basketMsg);
								}

								return;
							}
						}
					}

					break;
				}

				default:
				{
					if (message is CandleMessage candleMsg && _subscriptions.TryGetValue(message.Type.ToCandleMarketDataType(), out var dict))
					{
						if (dict.TryGetValue(candleMsg.OriginalTransactionId, out var info))
						{
							var basketMsgs = info.Processor.Process(candleMsg).Cast<CandleMessage>();

							foreach (var basketMsg in basketMsgs)
							{
								basketMsg.OriginalTransactionId = info.TransactionId;
								base.OnInnerAdapterNewOutMessage(basketMsg);
							}

							return;
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
}