namespace StockSharp.Algo;

/// <summary>
/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderBookIncrementMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class OrderBookIncrementMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private class BookInfo(SecurityId securityId)
	{
		public readonly OrderBookIncrementBuilder Builder = new(securityId);
		public readonly CachedSynchronizedSet<long> SubscriptionIds = [];
	}

	private readonly Lock _syncObject = new();

	private readonly Dictionary<long, BookInfo> _byId = [];
	private readonly Dictionary<SecurityId, BookInfo> _online = [];
	private readonly HashSet<long> _passThrough = [];
	private readonly CachedSynchronizedSet<long> _allSecSubscriptions = [];
	private readonly CachedSynchronizedSet<long> _allSecSubscriptionsPassThrough = [];

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_syncObject.EnterScope())
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

						using (_syncObject.EnterScope())
						{
							if (mdMsg.SecurityId == default)
							{
								if (mdMsg.DoNotBuildOrderBookIncrement)
									_allSecSubscriptionsPassThrough.Add(transId);
								else
									_allSecSubscriptions.Add(transId);

								break;
							}

							if (mdMsg.DoNotBuildOrderBookIncrement)
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

						LogInfo("OB incr subscribed {0}/{1}.", mdMsg.SecurityId, transId);
					}
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
				}

				break;
			}
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private void RemoveSubscription(long id)
	{
		using (_syncObject.EnterScope())
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

		LogInfo("Unsubscribed {0}.", id);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
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

				using (_syncObject.EnterScope())
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

				using (_syncObject.EnterScope())
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

					using (_syncObject.EnterScope())
					{
						if (!_byId.TryGetValue(subscriptionId, out var info))
						{
							if (_passThrough.Contains(subscriptionId) || _allSecSubscriptionsPassThrough.Contains(subscriptionId))
							{
								passThrough ??= [];

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

					clones ??= [];

					newQuoteMsg.SetSubscriptionIds(ids);
					clones.Add(newQuoteMsg);
				}

				if (passThrough is null)
					message = null;
				else
					quoteMsg.SetSubscriptionIds([.. passThrough]);

				break;
			}
		}

		if (message != null)
			await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (clones != null)
		{
			foreach (var clone in clones)
				await base.OnInnerAdapterNewOutMessageAsync(clone, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="OrderBookIncrementMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new OrderBookIncrementMessageAdapter(InnerAdapter.TypedClone());
	}
}