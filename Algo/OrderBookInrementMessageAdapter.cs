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
		protected override void OnSendInMessage(Message message)
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
						}
						else
						{
							RemoveSubscription(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}
			}

			base.OnSendInMessage(message);
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
		}

		private QuoteChangeMessage ApplyNewState(BookInfo info, QuoteChangeMessage quoteMsg, QuoteChangeStates newState)
		{
			var currState = info.State;

			if (currState != newState)
			{
				switch (currState)
				{
					case _none:
					case QuoteChangeStates.SnapshotStarted:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
						{
							this.AddDebugLog($"{currState}->{newState}");
							return null;
						}

						info.Bids.Clear();
						info.Asks.Clear();

						break;
					}
					case QuoteChangeStates.SnapshotBuilding:
					{
						if (newState != QuoteChangeStates.SnapshotComplete)
						{
							this.AddDebugLog($"{currState}->{newState}");
							return null;
						}

						break;
					}
					case QuoteChangeStates.SnapshotComplete:
					case QuoteChangeStates.Increment:
					{
						if (newState == QuoteChangeStates.SnapshotBuilding)
						{
							this.AddDebugLog($"{currState}->{newState}");
							return null;
						}

						break;
					}
					default:
						throw new ArgumentOutOfRangeException(currState.ToString());
				}

				info.State = currState = newState;
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

			Copy(quoteMsg.Bids, info.Bids);
			Copy(quoteMsg.Asks, info.Asks);

			if (currState == QuoteChangeStates.SnapshotStarted || currState == QuoteChangeStates.SnapshotBuilding)
				return null;

			return new QuoteChangeMessage
			{
				SecurityId = quoteMsg.SecurityId,
				Bids = info.Bids.Select(p => new QuoteChange(Sides.Buy, p.Key, p.Value)).ToArray(),
				Asks = info.Asks.Select(p => new QuoteChange(Sides.Sell, p.Key, p.Value)).ToArray(),
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
					var onlineMsg = (SubscriptionOnlineMessage)message;

					lock (_syncObject)
					{
						var info = _byId.TryGetValue(onlineMsg.OriginalTransactionId);

						if (info != null)
						{
							if (_online.TryGetValue(info.SecurityId, out var online))
							{
								online.SubscriptionIds.Add(onlineMsg.OriginalTransactionId);
								_byId.Remove(onlineMsg.OriginalTransactionId);
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
			return new OrderBookInrementMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}