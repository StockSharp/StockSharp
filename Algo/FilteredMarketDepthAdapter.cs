namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Filtered market depth data type.
	/// </summary>
	public class FilteredMarketDepthMessage : MarketDataMessage
	{
		/// <summary>
		/// Create a copy of <see cref="FilteredMarketDepthMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new FilteredMarketDepthMessage();
			CopyTo(clone);
			return clone;
		}
	}

	/// <summary>
	/// Filtered market depth adapter.
	/// </summary>
	public class FilteredMarketDepthAdapter : MessageAdapterWrapper
	{
		private class FilteredMarketDepthInfo
		{
			private readonly Dictionary<Tuple<Sides, decimal>, RefPair<Dictionary<long, decimal>, decimal>> _executions = new Dictionary<Tuple<Sides, decimal>, RefPair<Dictionary<long, decimal>, decimal>>();

			public FilteredMarketDepthInfo(long transactionId, IEnumerable<ExecutionMessage> orders)
			{
				if (orders == null)
					throw new ArgumentNullException(nameof(orders));

				TransactionId = transactionId;

				orders.ForEach(Process);
			}

			public long TransactionId { get; }
			public HashSet<long> Subscriptions { get; } = new HashSet<long>();

			private QuoteChange[] Filter(Sides side, IEnumerable<QuoteChange> quotes)
			{
				return quotes
					.Select(quote =>
					{
						var res = quote.Clone();
						var key = Tuple.Create(side, res.Price);

						var own = _executions.TryGetValue(key)?.Second;
						if (own != null)
							res.Volume -= own.Value;

						return res.Volume <= 0 ? null : res;
					})
					.Where(q => q != null)
					.ToArray();
			}

			public QuoteChangeMessage Process(QuoteChangeMessage message)
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));

				return new QuoteChangeMessage
				{
					SecurityId = message.SecurityId,
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					IsSorted = message.IsSorted,
					IsByLevel1 = message.IsByLevel1,
					Currency = message.Currency,
					IsFiltered = true,
					Bids = Filter(Sides.Buy, message.Bids),
					Asks = Filter(Sides.Sell, message.Asks),
				};
			}

			public void Process(ExecutionMessage message)
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));

				var key = Tuple.Create(message.Side, message.OrderPrice);

				switch (message.OrderState)
				{
					case OrderStates.Done:
					case OrderStates.Failed:
					{
						var pair = _executions.TryGetValue(key);

						if (pair == null)
							break;

						var balance = pair.First.TryGetAndRemove(message.OriginalTransactionId);

						if (pair.First.Count == 0)
							_executions.Remove(key);
						else
							pair.Second -= balance;

						break;
					}

					case OrderStates.Active:
					{
						var balance = message.Balance;

						if (balance != null)
						{
							var pair = _executions.SafeAdd(key, k => RefTuple.Create(new Dictionary<long, decimal>(), 0M));

							var prev = pair.First.TryGetValue(message.OriginalTransactionId);

							pair.First[message.OriginalTransactionId] = balance.Value;
							pair.Second += balance.Value - prev;
						}

						break;
					}
				}
			}
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<SecurityId, FilteredMarketDepthInfo> _infos = new Dictionary<SecurityId, FilteredMarketDepthInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociatedSecurityAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public FilteredMarketDepthAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncObject)
						_infos.Clear();

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType2.MessageType != typeof(QuoteChangeMessage))
						break;

					var isFilteredMsg = mdMsg is FilteredMarketDepthMessage;
					var transId = mdMsg.TransactionId;

					if (mdMsg.IsSubscribe)
					{
						if (!isFilteredMsg)
							break;

						var data = mdMsg.GetArg<Tuple<QuoteChangeMessage, ExecutionMessage[]>>();

						QuoteChangeMessage filtered = null;

						lock (_syncObject)
						{
							var info = _infos.SafeAdd(mdMsg.SecurityId, key => new FilteredMarketDepthInfo(transId, data.Item2));
							info.Subscriptions.Add(transId);

							if (info.Subscriptions.Count == 1)
							{
								var clone = new MarketDataMessage();
								mdMsg.CopyTo(clone);
								message = clone;

								filtered = info.Process(data.Item1);
							}
						}

						if (filtered == null)
						{
							RaiseNewOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = transId });
							return true;
						}
						else
							RaiseNewOutMessage(filtered);
					}
					else
					{
						SubscriptionResponseMessage reply;

						lock (_syncObject)
						{
							var info = _infos.FirstOrDefault(p => p.Value.Subscriptions.Contains(mdMsg.OriginalTransactionId)).Value;

							if (info != null)
							{
								info.Subscriptions.Remove(mdMsg.OriginalTransactionId);

								if (info.Subscriptions.Count > 0)
								{
									reply = new SubscriptionResponseMessage
									{
										OriginalTransactionId = transId,
									};
								}
								else
								{
									message = new MarketDataMessage
									{
										IsSubscribe = false,
										TransactionId = transId,
										OriginalTransactionId = info.TransactionId,
									};

									break;
								}
							}
							else
							{
								if (!isFilteredMsg)
									break;

								reply = mdMsg.CreateResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(mdMsg.OriginalTransactionId)));
							}
						}

						RaiseNewOutMessage(reply);
						return true;
					}

					break;
				}
			}

			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					QuoteChangeMessage filtered;

					lock (_syncObject)
					{
						var info = _infos.TryGetValue(quoteMsg.SecurityId);

						if (info == null)
							break;

						filtered = info.Process(quoteMsg);

						filtered.SetSubscriptionIds(subscriptionId: info.TransactionId);

						// subscription for origin book was initialized only by filtered book
						if ((quoteMsg.SubscriptionIds?.Length == 1 && quoteMsg.SubscriptionIds[0] == info.TransactionId) || quoteMsg.SubscriptionId == info.TransactionId)
							message = null;
					}

					base.OnInnerAdapterNewOutMessage(filtered);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.Transaction)
						break;

					if (!execMsg.HasOrderInfo())
						break;

					// ignore market orders
					if (execMsg.OrderPrice == 0)
						break;

					// ignore unknown orders
					if (execMsg.OriginalTransactionId == 0)
						break;

					if (execMsg.OrderState == OrderStates.Active || execMsg.OrderState == OrderStates.Done)
					{
						lock (_syncObject)
							_infos.TryGetValue(execMsg.SecurityId)?.Process(execMsg);
					}

					break;
				}
			}

			if (message != null)
				base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="FilteredMarketDepthAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new FilteredMarketDepthAdapter(InnerAdapter);
		}
	}
}