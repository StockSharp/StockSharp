namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using QuotesDict = System.Collections.Generic.SortedDictionary<decimal, decimal>;
	using BookInfo = Ecng.Common.RefTriple<System.Collections.Generic.SortedDictionary<decimal, decimal>, System.Collections.Generic.SortedDictionary<decimal, decimal>, Messages.QuoteChangeStates>;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookInrementMessageAdapter : MessageAdapterWrapper
	{
		private const QuoteChangeStates _none = (QuoteChangeStates)(-1);

		private readonly SynchronizedDictionary<long, BookInfo> _states = new SynchronizedDictionary<long, BookInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookInrementMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookInrementMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_states.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType == MarketDataTypes.MarketDepth)
					{
						if (mdMsg.IsSubscribe)
						{
							if (IsSupportOrderBookIncrements)
								_states.Add(mdMsg.TransactionId, RefTuple.Create(new QuotesDict(new BackwardComparer<decimal>()), new QuotesDict(), _none));
						}
						else
						{
							_states.Remove(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}
			}

			base.OnSendInMessage(message);
		}

		private QuoteChangeMessage ApplyNewState(long subscriptionId, BookInfo info, QuoteChangeMessage quoteMsg, QuoteChangeStates newState)
		{
			var currState = info.Third;

			if (currState != newState)
			{
				switch (currState)
				{
					case _none:
					case QuoteChangeStates.SnapshotStarted:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
							this.AddWarningLog($"{currState}->{newState}");

						info.First.Clear();
						info.Second.Clear();

						break;
					}
					case QuoteChangeStates.SnapshotBuilding:
					{
						if (newState != QuoteChangeStates.SnapshotComplete)
							this.AddWarningLog($"{currState}->{newState}");

						break;
					}
					case QuoteChangeStates.SnapshotComplete:
					case QuoteChangeStates.Increment:
					{
						if (newState == QuoteChangeStates.SnapshotBuilding)
							this.AddWarningLog($"{currState}->{newState}");

						break;
					}
					default:
						throw new ArgumentOutOfRangeException(currState.ToString());
				}

				info.Third = currState = newState;
			}

			void Copy(IEnumerable<QuoteChange> from, QuotesDict to)
			{
				foreach (var quote in from)
				{
					if (quote.Volume == 0)
						to.Remove(quote.Price);
					else
						to[quote.Price] = quote.Volume;
				}
			}

			Copy(quoteMsg.Bids, info.First);
			Copy(quoteMsg.Asks, info.Second);

			if (currState == QuoteChangeStates.SnapshotStarted || currState == QuoteChangeStates.SnapshotBuilding)
				return null;

			return new QuoteChangeMessage
			{
				SecurityId = quoteMsg.SecurityId,
				Bids = info.First.Select(p => new QuoteChange(Sides.Buy, p.Key, p.Value)).ToArray(),
				Asks = info.Second.Select(p => new QuoteChange(Sides.Sell, p.Key, p.Value)).ToArray(),
				IsSorted = true,
				ServerTime = quoteMsg.ServerTime,
				SubscriptionId = subscriptionId,
				OriginalTransactionId = quoteMsg.OriginalTransactionId,
			};
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			List<QuoteChangeMessage> clones = null;
			HashSet<long> incrSubscriptionIds = null;

			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					var state = quoteMsg.State;

					if (state == null)
						break;

					foreach (var subscriptionId in quoteMsg.GetSubscriptionIds())
					{
						if (!_states.TryGetValue(subscriptionId, out var info))
							continue;

						if (incrSubscriptionIds == null)
							incrSubscriptionIds = new HashSet<long>();

						incrSubscriptionIds.Add(subscriptionId);

						var newQuoteMsg = ApplyNewState(subscriptionId, info, quoteMsg, state.Value);

						if (newQuoteMsg == null)
							continue;

						if (clones == null)
							clones = new List<QuoteChangeMessage>();

						clones.Add(newQuoteMsg);
					}

					if (incrSubscriptionIds != null)
					{
						var ids = quoteMsg.GetSubscriptionIds().Except(incrSubscriptionIds).ToArray();

						if (ids.Length > 0)
						{
							quoteMsg.SubscriptionId = 0;
							quoteMsg.SubscriptionIds = ids;
						}
						else
							message = null;
					}

					break;
				}
			}

			if (message != null)
				base.OnInnerAdapterNewOutMessage(message);

			if (clones != null)
			{
				foreach (var clone in clones)
					base.OnInnerAdapterNewOutMessage(clone);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookInrementMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookInrementMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}