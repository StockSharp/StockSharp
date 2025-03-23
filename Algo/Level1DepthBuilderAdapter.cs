namespace StockSharp.Algo;

/// <summary>
/// Level1 depth builder adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Level1DepthBuilderAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class Level1DepthBuilderAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
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

	private readonly SyncObject _syncObject = new();

	private readonly Dictionary<long, BookInfo> _byId = [];
	private readonly Dictionary<SecurityId, BookInfo> _online = [];

	/// <inheritdoc />
	public override bool SendInMessage(Message message)
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

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.SecurityId == default || mdMsg.DataType2 != DataType.MarketDepth)
						break;

					if (mdMsg.BuildMode == MarketDataBuildModes.Load)
						break;

					if (mdMsg.BuildFrom != null && mdMsg.BuildFrom != DataType.Level1)
						break;

					var transId = mdMsg.TransactionId;

					lock (_syncObject)
					{
						var info = new BookInfo(new(mdMsg.SecurityId));
						info.SubscriptionIds.Add(transId);
						_byId.Add(transId, info);
					}
					
					mdMsg = mdMsg.TypedClone();
					mdMsg.DataType2 = DataType.Level1;
					message = mdMsg;

					LogDebug("L1->OB {0} added.", transId);
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
				}

				break;
			}
		}

		return base.SendInMessage(message);
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

		LogDebug("L1->OB {0} removed.", id);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		List<QuoteChangeMessage> books = null;
		
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

			case MessageTypes.Level1Change:
			{
				lock (_syncObject)
				{
					if (_byId.Count == 0 && _online.Count == 0)
						break;
				}

				var level1Msg = (Level1ChangeMessage)message;

				var ids = level1Msg.GetSubscriptionIds();

				HashSet<long> leftIds = null;

				lock (_syncObject)
				{
					foreach (var id in ids)
					{
						if (!_byId.TryGetValue(id, out var info))
							continue;
					
						var quoteMsg = info.Builder.Process(level1Msg);

						if (quoteMsg == null)
							continue;
						
						quoteMsg.SetSubscriptionIds(info.SubscriptionIds.Cache);

						books ??= [];

						books.Add(quoteMsg);

						leftIds ??= [.. ids];

						leftIds.RemoveRange(info.SubscriptionIds.Cache);
					}
				}
					
				if (leftIds != null)
				{
					if (leftIds.Count == 0)
					{
						message = null;
						break;
					}

					level1Msg.SetSubscriptionIds([.. leftIds]);
				}

				break;
			}
		}

		if (message != null)
			base.OnInnerAdapterNewOutMessage(message);

		if (books != null)
		{
			foreach (var book in books)
			{
				base.OnInnerAdapterNewOutMessage(book);
			}
		}
	}

	/// <summary>
	/// Create a copy of <see cref="Level1DepthBuilderAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone() => new Level1DepthBuilderAdapter(InnerAdapter.TypedClone());
}