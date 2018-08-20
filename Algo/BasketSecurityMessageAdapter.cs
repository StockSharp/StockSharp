namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Reflection;

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

		private readonly SynchronizedDictionary<SecurityId, SecurityMessage> _basketSecurities = new SynchronizedDictionary<SecurityId, SecurityMessage>();
		private readonly SynchronizedDictionary<MarketDataTypes, SubscriptionLegsInfo> _subscriptions = new SynchronizedDictionary<MarketDataTypes, SubscriptionLegsInfo>();
		
		private readonly IBasketSecurityProcessorProvider _processorProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketSecurityMessageAdapter"/>.
		/// </summary>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public BasketSecurityMessageAdapter(IBasketSecurityProcessorProvider processorProvider, IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
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
						if (_basketSecurities.TryGetValue(mdMsg.SecurityId, out var basket))
						{
							var tuple = Tuple.Create(_processorProvider.GetProcessorType(basket.BasketExpression).CreateInstance<IBasketSecurityProcessor>(basket), mdMsg.TransactionId);

							var info = _subscriptions.SafeAdd(mdMsg.DataType);

							var ids = new long[basket.BasketLegs.Length];

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
				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					
					if (secMsg.BasketLegs.Length > 0)
						_basketSecurities.Add(secMsg.SecurityId, (SecurityMessage)secMsg.Clone());
					
					break;
				}

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
			return new BasketSecurityMessageAdapter(_processorProvider, InnerAdapter);
		}
	}
}