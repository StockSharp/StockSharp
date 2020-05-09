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
		private readonly SynchronizedDictionary<long, RefTriple<bool, IOrderLogMarketDepthBuilder, SyncObject>> _subscriptionIds = new SynchronizedDictionary<long, RefTriple<bool, IOrderLogMarketDepthBuilder, SyncObject>>();

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
				if (message.SecurityId == default || !InnerAdapter.IsMarketDataTypeSupported(DataType.OrderLog))
					return message;

				var isBuild = message.BuildMode == MarketDataBuildModes.Build && message.BuildFrom == DataType.OrderLog;
					
				if (message.DataType2 == DataType.MarketDepth)
				{
					if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType2))
					{
						var builder = message.DepthBuilder ?? InnerAdapter.CreateOrderLogMarketDepthBuilder(message.SecurityId);

						_subscriptionIds.Add(message.TransactionId, RefTuple.Create(true, builder, new SyncObject()));

						message = message.TypedClone();
						message.DataType2 = DataType.OrderLog;

						this.AddInfoLog("OL->MD subscribed {0}/{1}.", message.SecurityId, message.TransactionId);
					}
				}
				else if (message.DataType2 == DataType.Ticks)
				{
					if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType2))
					{
						_subscriptionIds.Add(message.TransactionId, RefTuple.Create(false, (IOrderLogMarketDepthBuilder)null, new SyncObject()));

						message = message.TypedClone();
						message.DataType2 = DataType.OrderLog;

						this.AddInfoLog("OL->TICK subscribed {0}/{1}.", message.SecurityId, message.TransactionId);
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
				this.AddInfoLog("OL->{0} unsubscribed {1}/{2}.", tuple.First ? "MD" : "TICK", tuple.First, id);
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

				if (tuple.First)
				{
					var sync = tuple.Third;

					IOrderLogMarketDepthBuilder builder = tuple.Second;

					try
					{
						QuoteChangeMessage depth;

						lock (sync)
							depth = builder.Update(execMsg)?.TypedClone();

						this.AddDebugLog("OL->MD processing {0}={1}.", execMsg.SecurityId, depth != null);

						if (depth != null)
						{
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
					this.AddDebugLog("OL->TICK processing {0}.", execMsg.SecurityId);
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
			return new OrderLogMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}