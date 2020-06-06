namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

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

			public FilteredMarketDepthInfo(FilteredMarketDepthMessage origin, IEnumerable<ExecutionMessage> orders)
			{
				if (orders is null)
					throw new ArgumentNullException(nameof(orders));

				Origin = origin ?? throw new ArgumentNullException(nameof(origin));

				foreach (var order in orders)
					Process(order);
			}

			public FilteredMarketDepthMessage Origin { get; }
			public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;

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
				if (message is null)
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
				if (message is null)
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

		private readonly SynchronizedDictionary<long, FilteredMarketDepthInfo> _infos = new SynchronizedDictionary<long, FilteredMarketDepthInfo>();
		private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedSet<long>> _infosBySecId = new SynchronizedDictionary<SecurityId, CachedSynchronizedSet<long>>();

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
					_infos.Clear();
					_infosBySecId.Clear();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (!(mdMsg is FilteredMarketDepthMessage filteredMsg))
							break;

						if (mdMsg.SecurityId == default)
							break;

						var transId = mdMsg.TransactionId;

						var data = mdMsg.GetArg<Tuple<QuoteChangeMessage, ExecutionMessage[]>>();

						var info = new FilteredMarketDepthInfo(filteredMsg.TypedClone(), data.Item2);
						_infos.Add(transId, info);
						_infosBySecId.SafeAdd(mdMsg.SecurityId).Add(transId);

						mdMsg = new MarketDataMessage();
						filteredMsg.CopyTo(mdMsg);
						mdMsg.DataType2 = DataType.MarketDepth;
						message = mdMsg;

						RaiseNewOutMessage(info.Process(data.Item1));
					}
					else
					{
						if (_infos.TryGetAndRemove(mdMsg.OriginalTransactionId, out var info))
						{
							info.State = SubscriptionStates.Stopped;
							_infosBySecId.TryGetValue(info.Origin.SecurityId)?.Remove(mdMsg.OriginalTransactionId);

							var clone = new MarketDataMessage();
							mdMsg.CopyTo(clone);
							message = clone;
						}
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
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;

					if (!_infos.TryGetValue(responseMsg.OriginalTransactionId, out var info))
						break;

					if (responseMsg.Error == null)
						info.State = SubscriptionStates.Active;
					else
					{
						info.State = SubscriptionStates.Error;

						_infos.Remove(responseMsg.OriginalTransactionId);
						_infosBySecId.TryGetValue(info.Origin.SecurityId)?.Remove(responseMsg.OriginalTransactionId);
					}

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var finishMsg = (SubscriptionFinishedMessage)message;

					if (_infos.TryGetAndRemove(finishMsg.OriginalTransactionId, out var info))
					{
						info.State = SubscriptionStates.Finished;
						_infosBySecId.TryGetValue(info.Origin.SecurityId)?.Remove(finishMsg.OriginalTransactionId);
					}

					break;
				}

				case MessageTypes.SubscriptionOnline:
				{
					var onlineMsg = (SubscriptionOnlineMessage)message;

					if (_infos.TryGetValue(onlineMsg.OriginalTransactionId, out var info))
						info.State = SubscriptionStates.Online;

					break;
				}

				case MessageTypes.QuoteChange:
				{
					if (_infos.Count == 0)
						break;

					var quoteMsg = (QuoteChangeMessage)message;

					if (quoteMsg.State != null)
						break;

					var ids = quoteMsg.GetSubscriptionIds();
					var set = new HashSet<long>(quoteMsg.GetSubscriptionIds());

					QuoteChangeMessage filtered = null;

					foreach (var id in ids)
					{
						if (_infos.TryGetValue(id, out var info) && info.State.IsActive())
						{
							var newIds = _infosBySecId[info.Origin.SecurityId].Cache;

							filtered = info.Process(quoteMsg);
							filtered.SetSubscriptionIds(newIds);
							
							set.RemoveRange(newIds);
							break;
						}
					}

					if (set.Count > 0)
						quoteMsg.SetSubscriptionIds(set.ToArray());
					else
					{
						// subscription for origin book was initialized only by filtered book
						message = null;
					}

					if (filtered != null)
						base.OnInnerAdapterNewOutMessage(filtered);

					break;
				}

				case MessageTypes.Execution:
				{
					if (_infosBySecId.Count == 0 || _infos.Count == 0)
						break;

					var execMsg = (ExecutionMessage)message;

					if	(
						execMsg.ExecutionType != ExecutionTypes.Transaction ||
						!execMsg.HasOrderInfo() ||
						execMsg.OrderPrice == 0 || // ignore market orders
						execMsg.OriginalTransactionId == 0 // ignore unknown orders
						)
						break;

					if (execMsg.OrderState == OrderStates.Active || execMsg.OrderState == OrderStates.Done)
					{
						if (!_infosBySecId.TryGetValue(execMsg.SecurityId, out var set))
							break;

						foreach (var id in set.Cache)
							_infos.TryGetValue(id)?.Process(execMsg);
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