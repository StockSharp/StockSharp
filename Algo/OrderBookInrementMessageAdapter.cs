namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using QuotesDict = System.Collections.Generic.SortedDictionary<decimal, System.Tuple<decimal, int?, Messages.QuoteConditions>>;
	using QuotesByPosList = System.Collections.Generic.List<System.Tuple<decimal, decimal, int?, Messages.QuoteConditions>>;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookInrementMessageAdapter : MessageAdapterWrapper
	{
		private const QuoteChangeStates _none = (QuoteChangeStates)(-1);

		private class BookInfo
		{
			public BookInfo(SecurityId securityId)
			{
				SecurityId = securityId;
			}

			public SecurityId SecurityId { get; }

			public QuoteChangeStates State { get; set; } = _none;

			public readonly QuotesDict Bids = new QuotesDict(new BackwardComparer<decimal>());
			public readonly QuotesDict Asks = new QuotesDict();

			public readonly QuotesByPosList BidsByPos = new QuotesByPosList();
			public readonly QuotesByPosList AsksByPos = new QuotesByPosList();

			public readonly CachedSynchronizedSet<long> SubscriptionIds = new CachedSynchronizedSet<long>();
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, BookInfo> _byId = new Dictionary<long, BookInfo>();
		private readonly Dictionary<SecurityId, BookInfo> _online = new Dictionary<SecurityId, BookInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookInrementMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookInrementMessageAdapter(IMessageAdapter innerAdapter)
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
					{
						_byId.Clear();
						_online.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType == MarketDataTypes.MarketDepth)
					{
						if (mdMsg.IsSubscribe)
						{
							lock (_syncObject)
							{
								var info = new BookInfo(mdMsg.SecurityId);

								info.SubscriptionIds.Add(mdMsg.TransactionId);
								
								_byId.Add(mdMsg.TransactionId, info);
							}

							this.AddInfoLog("OB incr subscribed {0}/{1}.", mdMsg.SecurityId, mdMsg.TransactionId);
						}
						else
						{
							RemoveSubscription(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}
			}

			return base.OnSendInMessage(message);
		}

		private void RemoveSubscription(long id)
		{
			lock (_syncObject)
			{
				var changeId = true;

				if (!_byId.TryGetAndRemove(id, out var info))
				{
					changeId = false;

					info = _online.FirstOrDefault(p => p.Value.SubscriptionIds.Contains(id)).Value;

					if (info == null)
						return;
				}

				if (info != _online.TryGetValue(info.SecurityId))
					return;

				info.SubscriptionIds.Remove(id);

				var ids = info.SubscriptionIds.Cache;

				if (ids.Length == 0)
					_online.Remove(info.SecurityId);
				else if (changeId)
					_byId.Add(ids[0], info);
			}

			this.AddInfoLog("Unsubscribed {0}.", id);
		}

		private QuoteChangeMessage ApplyNewState(BookInfo info, QuoteChangeMessage quoteMsg, QuoteChangeStates newState)
		{
			var currState = info.State;

			void CheckSwitch()
			{
				switch (currState)
				{
					case _none:
					case QuoteChangeStates.SnapshotStarted:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
							this.AddDebugLog($"{currState}->{newState}");

						break;
					}
					case QuoteChangeStates.SnapshotBuilding:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
							this.AddDebugLog($"{currState}->{newState}");

						break;
					}
					case QuoteChangeStates.SnapshotComplete:
					case QuoteChangeStates.Increment:
					{
						if (newState == QuoteChangeStates.SnapshotBuilding)
							this.AddDebugLog($"{currState}->{newState}");

						break;
					}
				}
			}

			if (currState != newState)
			{
				CheckSwitch();

				if (newState == QuoteChangeStates.SnapshotStarted)
				{
					info.Bids.Clear();
					info.Asks.Clear();
				}

				switch (currState)
				{
					case _none:
					{
						info.Bids.Clear();
						info.Asks.Clear();

						break;
					}
					case QuoteChangeStates.SnapshotStarted:
						break;
					case QuoteChangeStates.SnapshotBuilding:
						break;
					case QuoteChangeStates.SnapshotComplete:
					{
						if (newState == QuoteChangeStates.SnapshotComplete)
						{
							info.Bids.Clear();
							info.Asks.Clear();
						}

						break;
					}
					case QuoteChangeStates.Increment:
						break;
					default:
						throw new ArgumentOutOfRangeException(currState.ToString());
				}

				info.State = currState = newState;
			}

			void Apply(IEnumerable<QuoteChange> from, QuotesDict to)
			{
				foreach (var quote in from)
				{
					if (quote.Volume == 0)
						to.Remove(quote.Price);
					else
						to[quote.Price] = Tuple.Create(quote.Volume, quote.OrdersCount, quote.Condition);
				}
			}

			void ApplyByPos(IEnumerable<QuoteChange> from, QuotesByPosList to)
			{
				foreach (var quote in from)
				{
					var startPos = quote.StartPosition.Value;

					switch (quote.Action)
					{
						case QuoteChangeActions.New:
						{
							var tuple = Tuple.Create(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);

							if (startPos > to.Count)
								throw new InvalidOperationException($"Pos={startPos}>Count={to.Count}");
							else if (startPos == to.Count)
								to.Add(tuple);
							else
								to.Insert(startPos, tuple);

							break;
						}
						case QuoteChangeActions.Update:
						{
							to[startPos] = Tuple.Create(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);
							break;
						}
						case QuoteChangeActions.Delete:
						{
							if (quote.EndPosition == null)
								to.RemoveAt(startPos);
							else
								to.RemoveRange(startPos, (quote.EndPosition.Value - startPos) + 1);

							break;
						}
						default:
							throw new ArgumentOutOfRangeException(nameof(from), quote.Action, LocalizedStrings.Str1219);
					}
				}
			}

			if (quoteMsg.HasPositions)
			{
				ApplyByPos(quoteMsg.Bids, info.BidsByPos);
				ApplyByPos(quoteMsg.Asks, info.AsksByPos);
			}
			else
			{
				Apply(quoteMsg.Bids, info.Bids);
				Apply(quoteMsg.Asks, info.Asks);
			}

			if (currState == QuoteChangeStates.SnapshotStarted || currState == QuoteChangeStates.SnapshotBuilding)
				return null;

			IEnumerable<QuoteChange> bids;
			IEnumerable<QuoteChange> asks;

			if (quoteMsg.HasPositions)
			{
				bids = info.BidsByPos.Select(p => new QuoteChange(p.Item1, p.Item2, p.Item3, p.Item4));
				asks = info.AsksByPos.Select(p => new QuoteChange(p.Item1, p.Item2, p.Item3, p.Item4));
			}
			else
			{
				bids = info.Bids.Select(p => new QuoteChange(p.Key, p.Value.Item1, p.Value.Item2, p.Value.Item3));
				asks = info.Asks.Select(p => new QuoteChange(p.Key, p.Value.Item1, p.Value.Item2, p.Value.Item3));
			}

			return new QuoteChangeMessage
			{
				SecurityId = quoteMsg.SecurityId,
				Bids = bids.ToArray(),
				Asks = asks.ToArray(),
				IsSorted = true,
				ServerTime = quoteMsg.ServerTime,
				OriginalTransactionId = quoteMsg.OriginalTransactionId,
			};
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			List<QuoteChangeMessage> clones = null;

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

				case MessageTypes.SubscriptionOnline:
				{
					var id = ((SubscriptionOnlineMessage)message).OriginalTransactionId;

					lock (_syncObject)
					{
						var info = _byId.TryGetValue(id);

						if (info != null)
						{
							if (_online.TryGetValue(info.SecurityId, out var online))
							{
								online.SubscriptionIds.Add(id);
								_byId.Remove(id);
							}
							else
							{
								_online.Add(info.SecurityId, info);
							}
						}
					}
					
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					var state = quoteMsg.State;

					if (state == null)
						break;

					foreach (var subscriptionId in quoteMsg.GetSubscriptionIds())
					{
						BookInfo info;

						lock (_syncObject)
						{
							if (!_byId.TryGetValue(subscriptionId, out info))
								continue;
						}

						var newQuoteMsg = ApplyNewState(info, quoteMsg, state.Value);

						if (newQuoteMsg == null)
							continue;

						if (clones == null)
							clones = new List<QuoteChangeMessage>();

						newQuoteMsg.SetSubscriptionIds(info.SubscriptionIds.Cache);
						clones.Add(newQuoteMsg);
					}

					message = null;

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
			return new OrderBookInrementMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}