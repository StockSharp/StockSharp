namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// Filtered market depth adapter.
	/// </summary>
	public class FilteredMarketDepthAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Filtered market depth data type.
		/// </summary>
		public const MarketDataTypes FilteredMarketDepth = (MarketDataTypes)(-1);

		private sealed class FilteredMarketDepthInfo
		{
			private readonly Dictionary<Tuple<Sides, decimal>, RefPair<Dictionary<long, decimal>, decimal>> _executions = new Dictionary<Tuple<Sides, decimal>, RefPair<Dictionary<long, decimal>, decimal>>();

			public FilteredMarketDepthInfo(IEnumerable<ExecutionMessage> orders)
			{
				if (orders == null)
					throw new ArgumentNullException(nameof(orders));

				orders.ForEach(Process);
			}

			private IEnumerable<QuoteChange> Filter(IEnumerable<QuoteChange> quotes)
			{
				return quotes
					.Select(quote =>
					{
						var res = quote.Clone();
						var key = Tuple.Create(res.Side, res.Price);

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
					IsFiltered = true,
					Bids = Filter(message.Bids),
					Asks = Filter(message.Asks),
				};
			}

			public void Process(ExecutionMessage message)
			{
				if (!message.HasOrderInfo())
					throw new ArgumentException(nameof(message));

				// ignore market orders
				if (message.OrderPrice == 0)
					return;

				// ignore unknown orders
				if (message.OriginalTransactionId == 0)
					return;

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

		private readonly SynchronizedDictionary<SecurityId, FilteredMarketDepthInfo> _filteredMarketDepths = new SynchronizedDictionary<SecurityId, FilteredMarketDepthInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociatedSecurityAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public FilteredMarketDepthAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_filteredMarketDepths.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType != FilteredMarketDepth)
						break;

					if (mdMsg.IsSubscribe)
					{
						var clone = (MarketDataMessage)mdMsg.Clone();
						clone.DataType = MarketDataTypes.MarketDepth;
						clone.Arg = null;

						base.SendInMessage(clone);

						var data = (Tuple<QuoteChangeMessage, ExecutionMessage[]>)mdMsg.Arg;
						var info = _filteredMarketDepths.SafeAdd(mdMsg.SecurityId, s => new FilteredMarketDepthInfo(data.Item2));
						var quoteMsg = info.Process(data.Item1);

						RaiseNewOutMessage(quoteMsg);
					}
					else
					{
						var clone = (MarketDataMessage)mdMsg.Clone();
						clone.DataType = MarketDataTypes.MarketDepth;

						base.SendInMessage(clone);

						_filteredMarketDepths.Remove(mdMsg.SecurityId);
					}

					return;
				}
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					var info = _filteredMarketDepths.TryGetValue(quoteMsg.SecurityId);

					if (info != null)
					{
						var filteredQuoteMsg = info.Process(quoteMsg);
						RaiseNewOutMessage(filteredQuoteMsg);
					}

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.Transaction)
						break;

					if (execMsg.OrderState == OrderStates.Active || execMsg.OrderState == OrderStates.Done)
					{
						var info = _filteredMarketDepths.TryGetValue(execMsg.SecurityId);
						info?.Process(execMsg);
					}

					break;
				}
			}

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