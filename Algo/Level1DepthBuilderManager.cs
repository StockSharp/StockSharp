namespace StockSharp.Algo;

/// <summary>
/// Level1 depth builder message processing logic.
/// </summary>
public interface ILevel1DepthBuilderManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result with messages to forward and output messages.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result with forward message and extra output messages.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Level1 depth builder message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Level1DepthBuilderManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
public sealed class Level1DepthBuilderManager(ILogReceiver logReceiver) : ILevel1DepthBuilderManager
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

	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Lock _syncObject = new();

	private readonly Dictionary<long, BookInfo> _byId = [];
	private readonly Dictionary<SecurityId, BookInfo> _online = [];

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_syncObject.EnterScope())
				{
					_byId.Clear();
					_online.Clear();
				}

				return ([message], []);
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.SecurityId == default || mdMsg.DataType2 != DataType.MarketDepth)
						return ([message], []);

					if (mdMsg.BuildMode == MarketDataBuildModes.Load)
						return ([message], []);

					if (mdMsg.BuildFrom != null && mdMsg.BuildFrom != DataType.Level1)
						return ([message], []);

					var transId = mdMsg.TransactionId;

					using (_syncObject.EnterScope())
					{
						var info = new BookInfo(new(mdMsg.SecurityId));
						info.SubscriptionIds.Add(transId);
						_byId.Add(transId, info);
					}

					mdMsg = mdMsg.TypedClone();
					mdMsg.DataType2 = DataType.Level1;

					_logReceiver.AddDebugLog("L1->OB {0} added.", transId);

					return ([mdMsg], []);
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
					return ([message], []);
				}
			}

			default:
				return ([message], []);
		}
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut) ProcessOutMessage(Message message)
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

				using (_syncObject.EnterScope())
				{
					if (_byId.TryGetValue(id, out var info))
					{
						var secId = info.Builder.SecurityId;

						if (_online.TryGetValue(secId, out var online))
						{
							online.SubscriptionIds.Add(id);
							_byId[id] = online;
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
				using (_syncObject.EnterScope())
				{
					if (_byId.Count == 0 && _online.Count == 0)
						break;
				}

				var level1Msg = (Level1ChangeMessage)message;

				var ids = level1Msg.GetSubscriptionIds();

				HashSet<long> leftIds = null;

				using (_syncObject.EnterScope())
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
						return (null, books?.ToArray() ?? []);
					}

					level1Msg.SetSubscriptionIds([.. leftIds]);
				}

				break;
			}
		}

		return (message, books?.ToArray() ?? []);
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
			else if (changeId && !_byId.ContainsKey(ids[0]))
				_byId.Add(ids[0], info);
		}

		_logReceiver.AddDebugLog("L1->OB {0} removed.", id);
	}
}
