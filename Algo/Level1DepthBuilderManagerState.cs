namespace StockSharp.Algo;

/// <summary>
/// Default implementation of <see cref="ILevel1DepthBuilderManagerState"/>.
/// </summary>
public class Level1DepthBuilderManagerState : ILevel1DepthBuilderManagerState
{
	private sealed class Level1DepthBuilder(SecurityId securityId)
	{
		private decimal? _bidPrice, _askPrice, _bidVolume, _askVolume;
		public readonly SecurityId SecurityId = securityId;

		public QuoteChangeMessage Process(Level1ChangeMessage message)
		{
			var bidPrice = message.TryGetDecimal(Level1Fields.BestBidPrice);
			var askPrice = message.TryGetDecimal(Level1Fields.BestAskPrice);

			if (bidPrice == null && askPrice == null)
				return null;

			var bidVolume = message.TryGetDecimal(Level1Fields.BestBidVolume);
			var askVolume = message.TryGetDecimal(Level1Fields.BestAskVolume);

			if (_bidPrice == bidPrice && _askPrice == askPrice && _bidVolume == bidVolume && _askVolume == askVolume)
				return null;

			_bidPrice = bidPrice;
			_askPrice = askPrice;
			_bidVolume = bidVolume;
			_askVolume = askVolume;

			return new()
			{
				SecurityId = SecurityId,
				ServerTime = message.ServerTime,
				LocalTime = message.LocalTime,
				BuildFrom = DataType.Level1,
				Bids = bidPrice == null ? [] : [new QuoteChange(bidPrice.Value, bidVolume ?? 0)],
				Asks = askPrice == null ? [] : [new QuoteChange(askPrice.Value, askVolume ?? 0)],
			};
		}
	}

	private class BookInfo(Level1DepthBuilder builder)
	{
		public readonly Level1DepthBuilder Builder = builder;
		public readonly CachedSynchronizedSet<long> SubscriptionIds = [];
	}

	private readonly Lock _sync = new();
	private readonly Dictionary<long, BookInfo> _byId = [];
	private readonly Dictionary<SecurityId, BookInfo> _online = [];

	/// <inheritdoc />
	public void AddSubscription(long transactionId, SecurityId securityId)
	{
		using (_sync.EnterScope())
		{
			var info = new BookInfo(new(securityId));
			info.SubscriptionIds.Add(transactionId);
			_byId.Add(transactionId, info);
		}
	}

	/// <inheritdoc />
	public void OnSubscriptionOnline(long transactionId)
	{
		using (_sync.EnterScope())
		{
			if (!_byId.TryGetValue(transactionId, out var info))
				return;

			var secId = info.Builder.SecurityId;

			if (_online.TryGetValue(secId, out var online))
			{
				online.SubscriptionIds.Add(transactionId);
				_byId[transactionId] = online;
			}
			else
			{
				_online.Add(secId, info);
			}
		}
	}

	/// <inheritdoc />
	public QuoteChangeMessage TryBuildDepth(long subscriptionId, Level1ChangeMessage l1Msg, out long[] subscriptionIds)
	{
		subscriptionIds = null;

		using (_sync.EnterScope())
		{
			if (!_byId.TryGetValue(subscriptionId, out var info))
				return null;

			var quoteMsg = info.Builder.Process(l1Msg);

			if (quoteMsg == null)
				return null;

			subscriptionIds = info.SubscriptionIds.Cache;
			return quoteMsg;
		}
	}

	/// <inheritdoc />
	public bool ContainsSubscription(long subscriptionId)
	{
		using (_sync.EnterScope())
			return _byId.ContainsKey(subscriptionId);
	}

	/// <inheritdoc />
	public bool HasAnySubscriptions
	{
		get
		{
			using (_sync.EnterScope())
				return _byId.Count > 0 || _online.Count > 0;
		}
	}

	/// <inheritdoc />
	public void RemoveSubscription(long id)
	{
		using (_sync.EnterScope())
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
			else if (changeId && !_byId.ContainsKey(ids[0]))
				_byId.Add(ids[0], info);
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_byId.Clear();
			_online.Clear();
		}
	}
}
