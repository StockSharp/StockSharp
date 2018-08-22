namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter builds market data for basket securities.
	/// </summary>
	public class BasketSecurityMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionLegsInfo
		{
			public readonly SynchronizedDictionary<long, Tuple<IBasketSecurityProcessor, long>> ByTransactionIds = new SynchronizedDictionary<long, Tuple<IBasketSecurityProcessor, long>>();
			public readonly SynchronizedDictionary<SecurityId, Tuple<IBasketSecurityProcessor, long>> BySecurityIds = new SynchronizedDictionary<SecurityId, Tuple<IBasketSecurityProcessor, long>>();
		}

		private readonly SynchronizedDictionary<MarketDataTypes, SubscriptionLegsInfo> _subscriptions = new SynchronizedDictionary<MarketDataTypes, SubscriptionLegsInfo>();
		
		private readonly ISecurityProvider _securityProvider;
		private readonly IBasketSecurityProcessorProvider _processorProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketSecurityMessageAdapter"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public BasketSecurityMessageAdapter(ISecurityProvider securityProvider, IBasketSecurityProcessorProvider processorProvider, IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_processorProvider = processorProvider ?? throw new ArgumentNullException(nameof(processorProvider));
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptions.Clear();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.BasketExpression.IsEmpty())
							break;

						if (_securityProvider.LookupById(mdMsg.SecurityId) is BasketSecurity basket)
						{
							var processor = _processorProvider.CreateProcessor(basket);
							var tuple = Tuple.Create(processor, mdMsg.TransactionId);

							var info = _subscriptions.SafeAdd(mdMsg.DataType);

							var ids = new long[processor.BasketLegs.Length];

							for (var i = 0; i < ids.Length; i++)
							{
								ids[i] = TransactionIdGenerator.GetNextId();
								info.ByTransactionIds.Add(ids[i], tuple);
							}

							foreach (var id in ids)
							{
								var clone = (MarketDataMessage)mdMsg.Clone();
								clone.TransactionId = id;
								SendInMessage(clone);
							}

							return;
						}
					}
					else
					{

					}

					
					
					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRenko:
				{
					var candleMsg = (CandleMessage)message;

					if (_subscriptions.TryGetValue(message.Type.ToCandleMarketDataType(), out var info))
					{
						if (info.ByTransactionIds.TryGetValue(candleMsg.OriginalTransactionId, out var tuple))
						{
							var basketMsgs = (IEnumerable<CandleMessage>)tuple.Item1.Process(candleMsg);

							foreach (var basketMsg in basketMsgs)
							{
								basketMsg.OriginalTransactionId = tuple.Item2;
								RaiseNewOutMessage(basketMsg);
							}

							return;
						}
					}

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

					SubscriptionLegsInfo info;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
							info = _subscriptions.TryGetValue(MarketDataTypes.Trades);
							break;
						case ExecutionTypes.Transaction:
							info = null;
							break;
						case ExecutionTypes.OrderLog:
							info = _subscriptions.TryGetValue(MarketDataTypes.OrderLog);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (info != null)
					{
						if (execMsg.OriginalTransactionId == 0)
						{

						}
						else
						{
							if (info.ByTransactionIds.TryGetValue(execMsg.OriginalTransactionId, out var tuple))
							{
								var basketMsgs = (IEnumerable<ExecutionMessage>)tuple.Item1.Process(execMsg);

								foreach (var basketMsg in basketMsgs)
								{
									basketMsg.OriginalTransactionId = tuple.Item2;
									RaiseNewOutMessage(basketMsg);
								}

								return;
							}
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <inheritdoc />
		public override IMessageChannel Clone()
		{
			return new BasketSecurityMessageAdapter(_securityProvider, _processorProvider, InnerAdapter);
		}
	}
}