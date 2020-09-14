namespace StockSharp.Algo
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookIncrementMessageAdapter : MessageAdapterWrapper
	{
		private class BookInfo
		{
			public BookInfo(SecurityId securityId)
				=> Builder = new OrderBookIncrementBuilder(securityId);

			public readonly OrderBookIncrementBuilder Builder;
			public readonly CachedSynchronizedSet<long> SubscriptionIds = new CachedSynchronizedSet<long>();
		}

		private readonly SyncObject _syncObject = new SyncObject();

		private readonly Dictionary<long, BookInfo> _byId = new Dictionary<long, BookInfo>();
		private readonly Dictionary<SecurityId, BookInfo> _online = new Dictionary<SecurityId, BookInfo>();
		private readonly HashSet<long> _passThrough = new HashSet<long>();
		private readonly CachedSynchronizedSet<long> _allSecSubscriptions = new CachedSynchronizedSet<long>();
		private readonly CachedSynchronizedSet<long> _allSecSubscriptionsPassThrough = new CachedSynchronizedSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookIncrementMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookIncrementMessageAdapter(IMessageAdapter innerAdapter)
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
						_passThrough.Clear();
						_allSecSubscriptions.Clear();
						_allSecSubscriptionsPassThrough.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.DataType2 == DataType.MarketDepth)
						{
							var transId = mdMsg.TransactionId;

							lock (_syncObject)
							{
								if (mdMsg.SecurityId == default)
								{
									if (mdMsg.DoNotBuildOrderBookInrement)
										_allSecSubscriptionsPassThrough.Add(transId);
									else
										_allSecSubscriptions.Add(transId);

									break;
								}

								if (mdMsg.DoNotBuildOrderBookInrement)
								{
									_passThrough.Add(transId);
									break;
								}

								var info = new BookInfo(mdMsg.SecurityId)
								{
									Builder = { Parent = this }
								};

								info.SubscriptionIds.Add(transId);
								
								_byId.Add(transId, info);
							}

							this.AddInfoLog("OB incr subscribed {0}/{1}.", mdMsg.SecurityId, transId);
						}
					}
					else
					{
						RemoveSubscription(mdMsg.OriginalTransactionId);
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

				var secId = info.Builder.SecurityId;

				if (info != _online.TryGetValue(secId))
					return;

				info.SubscriptionIds.Remove(id);

				var ids = info.SubscriptionIds.Cache;

				if (ids.Length == 0)
					_online.Remove(secId);
				else if (changeId)
					_byId.Add(ids[0], info);
			}

			this.AddInfoLog("Unsubscribed {0}.", id);
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
						if (_byId.TryGetValue(id, out var info))
						{
							var secId = info.Builder.SecurityId;

							if (_online.TryGetValue(secId, out var online))
							{
								online.SubscriptionIds.Add(id);
								_byId.Remove(id);
							}
							else
							{
								_online.Add(secId, info);
							}
						}
					}
					
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					if (quoteMsg.State == null)
						break;

					lock (_syncObject)
					{
						if (_allSecSubscriptions.Count == 0 &&
							_allSecSubscriptionsPassThrough.Count == 0 &&
							_byId.Count == 0 &&
							_passThrough.Count == 0 &&
							_online.Count == 0)
							break;
					}

					List<long> passThrough = null;

					foreach (var subscriptionId in quoteMsg.GetSubscriptionIds())
					{
						QuoteChangeMessage newQuoteMsg;
						long[] ids;

						lock (_syncObject)
						{
							if (!_byId.TryGetValue(subscriptionId, out var info))
							{
								if (_passThrough.Contains(subscriptionId) || _allSecSubscriptionsPassThrough.Contains(subscriptionId))
								{
									if (passThrough is null)
										passThrough = new List<long>();

									passThrough.Add(subscriptionId);
								}

								continue;
							}

							newQuoteMsg = info.Builder.TryApply(quoteMsg, subscriptionId);

							if (newQuoteMsg == null)
								continue;

							ids = info.SubscriptionIds.Cache;
						}

						if (_allSecSubscriptions.Count > 0)
							ids = ids.Concat(_allSecSubscriptions.Cache);

						if (clones == null)
							clones = new List<QuoteChangeMessage>();

						newQuoteMsg.SetSubscriptionIds(ids);
						clones.Add(newQuoteMsg);
					}

					if (passThrough is null)
						message = null;
					else
						quoteMsg.SetSubscriptionIds(passThrough.ToArray());

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
		/// Create a copy of <see cref="OrderBookIncrementMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookIncrementMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}