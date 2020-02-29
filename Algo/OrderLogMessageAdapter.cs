namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book and tick data from order log flow.
	/// </summary>
	public class OrderLogMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<long, RefTriple<SecurityId, bool, IOrderLogMarketDepthBuilder>> _subscriptionIds = new SynchronizedDictionary<long, RefTriple<SecurityId, bool, IOrderLogMarketDepthBuilder>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderLogMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_subscriptionIds.Clear();
					break;

				case MessageTypes.MarketData:
					message = ProcessMarketDataRequest((MarketDataMessage)message);
					break;
			}

			return base.OnSendInMessage(message);
		}

		private MarketDataMessage ProcessMarketDataRequest(MarketDataMessage message)
		{
			if (message.IsSubscribe)
			{
				if (!InnerAdapter.IsMarketDataTypeSupported(DataType.OrderLog))
					return message;

				var isBuild = message.BuildMode == MarketDataBuildModes.Build && message.BuildFrom == MarketDataTypes.OrderLog;
					
				switch (message.DataType)
				{
					case MarketDataTypes.MarketDepth:
					{
						if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.ToDataType()))
						{
							var secId = GetSecurityId(message.SecurityId);

							IOrderLogMarketDepthBuilder builder = null;

							if (InnerAdapter.IsSecurityRequired(DataType.OrderLog))
								builder = InnerAdapter.CreateOrderLogMarketDepthBuilder(secId);

							_subscriptionIds.Add(message.TransactionId, RefTuple.Create(secId, true, builder));

							message = (MarketDataMessage)message.Clone();
							message.DataType = MarketDataTypes.OrderLog;

							this.AddInfoLog("OL->MD subscribed {0}/{1}.", secId, message.TransactionId);
						}

						break;
					}

					case MarketDataTypes.Trades:
					{
						if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.ToDataType()))
						{
							var secId = GetSecurityId(message.SecurityId);

							_subscriptionIds.Add(message.TransactionId, RefTuple.Create(secId, false, (IOrderLogMarketDepthBuilder)null));

							message = (MarketDataMessage)message.Clone();
							message.DataType = MarketDataTypes.OrderLog;

							this.AddInfoLog("OL->TICK subscribed {0}/{1}.", secId, message.TransactionId);
						}

						break;
					}
				}
			}
			else
				RemoveSubscription(message.OriginalTransactionId);

			return message;
		}

		private void RemoveSubscription(long id)
		{
			if (_subscriptionIds.TryGetAndRemove(id, out var tuple))
				this.AddInfoLog("OL->{0} unsubscribed {1}/{2}.", tuple.Second ? "MD" : "TICK", tuple.First, id);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			base.OnInnerAdapterNewOutMessage(message);

			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;

					if (!responseMsg.IsOk())
						RemoveSubscription(responseMsg.OriginalTransactionId);

					break;
				}
				case MessageTypes.SubscriptionFinished:
				{
					RemoveSubscription(((SubscriptionFinishedMessage)message).OriginalTransactionId);
					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.OrderLog)
						ProcessBuilders(execMsg);

					break;
				}
			}
		}

		private SecurityId GetSecurityId(SecurityId securityId)
		{
			return InnerAdapter.IsSecurityRequired(DataType.OrderLog)
				? securityId
				: default;
		}

		private void ProcessBuilders(ExecutionMessage execMsg)
		{
			if (execMsg.IsSystem == false)
				return;

			foreach (var subscriptionId in execMsg.GetSubscriptionIds())
			{
				if (!_subscriptionIds.TryGetValue(subscriptionId, out var tuple))
				{
					// can be non OL->MB subscription
					//this.AddDebugLog("OL processing {0}/{1} not found.", execMsg.SecurityId, subscriptionId);
					continue;
				}

				var secId = GetSecurityId(execMsg.SecurityId);

				if (tuple.Second)
				{
					var builder = tuple.Third;

					if (builder == null)
						tuple.Third = builder = new OrderLogMarketDepthBuilder(execMsg.SecurityId);

					try
					{
						var updated = builder.Update(execMsg);

						this.AddDebugLog("OL->MD processing {0}={1}.", execMsg.SecurityId, updated);

						if (updated)
						{
							var depth = (QuoteChangeMessage)builder.Depth.Clone();
							depth.SetSubscriptionIds(subscriptionId: subscriptionId);
							base.OnInnerAdapterNewOutMessage(depth);
						}
					}
					catch (Exception ex)
					{
						// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
						// а только выводим сообщение в лог
						base.OnInnerAdapterNewOutMessage(ex.ToErrorMessage());
					}
				}
				else
				{
					this.AddDebugLog("OL->TICK processing {0}.", secId);
					base.OnInnerAdapterNewOutMessage(execMsg.ToTick());
				}
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OrderLogMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderLogMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}