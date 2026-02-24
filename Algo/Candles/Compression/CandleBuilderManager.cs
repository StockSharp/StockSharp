namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candle builder processing logic.
/// </summary>
public interface ICandleBuilderManager
{
	/// <summary>
	/// Send out finished candles when they received.
	/// </summary>
	bool SendFinishedCandlesImmediatelly { get; set; }

	/// <summary>
	/// Storage buffer.
	/// </summary>
	IStorageBuffer Buffer { get; set; }

	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result.</returns>
	ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result.</returns>
	ValueTask<(Message forward, Message[] extraOut)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken);
}

/// <summary>
/// Candle builder processing implementation.
/// </summary>
public sealed class CandleBuilderManager : ICandleBuilderManager
{
	private enum SeriesStates
	{
		None,
		Regular,
		SmallTimeFrame,
		Compress,
	}

	private sealed class SeriesInfo : ICandleBuilderSubscription
	{
		public SeriesInfo(MarketDataMessage original, MarketDataMessage current)
		{
			Original = original ?? throw new ArgumentNullException(nameof(original));
			Current = current;
		}

		public long Id => Original.TransactionId;

		MarketDataMessage ICandleBuilderSubscription.Message => Original;

		public MarketDataMessage Original { get; }

		public Dictionary<SecurityId, SeriesInfo> Child { get; } = [];

		private MarketDataMessage _current;

		public MarketDataMessage Current
		{
			get => _current;
			set => _current = value ?? throw new ArgumentNullException(nameof(value));
		}

		public SeriesStates State { get; set; } = SeriesStates.None;

		public BiggerTimeFrameCandleCompressor BigTimeFrameCompressor { get; set; }

		public ICandleBuilderValueTransform Transform { get; set; }

		public DateTime? LastTime { get; set; }
		public long? Count { get; set; }

		CandleMessage ICandleBuilderSubscription.CurrentCandle { get; set; }

		public CandleMessage NonFinishedCandle { get; set; }

		public VolumeProfileBuilder VolumeProfile { get; set; }

		public long? LiveCandleTransactionId { get; set; }

		public bool IsCountExhausted => Count <= 0;
	}

	private readonly AsyncLock _sync = new();

	private readonly Dictionary<long, SeriesInfo> _series = [];
	private readonly Dictionary<long, long> _replaceId = [];
	private readonly CandleBuilderProvider _candleBuilderProvider;
	private readonly bool _cloneOutCandles;

	private readonly ILogReceiver _logReceiver;
	private readonly IdGenerator _idGenerator;
	private readonly IMessageAdapterWrapper _adapter;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleBuilderManager"/>.
	/// </summary>
	/// <param name="logReceiver">Log receiver.</param>
	/// <param name="idGenerator">Transaction id generator.</param>
	/// <param name="adapter">Adapter wrapper used for loopback messages and inner adapter access.</param>
	/// <param name="sendFinishedCandlesImmediatelly">Send finished candles immediately.</param>
	/// <param name="buffer">Storage buffer.</param>
	/// <param name="cloneOutCandles">Indicates whether outgoing candles should be cloned before emitting.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	public CandleBuilderManager(ILogReceiver logReceiver, IdGenerator idGenerator, IMessageAdapterWrapper adapter, bool sendFinishedCandlesImmediatelly, IStorageBuffer buffer, bool cloneOutCandles, CandleBuilderProvider candleBuilderProvider)
	{
		_logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
		_idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
		SendFinishedCandlesImmediatelly = sendFinishedCandlesImmediatelly;
		Buffer = buffer;
		_candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		_cloneOutCandles = cloneOutCandles;
	}

	/// <inheritdoc />
	public bool SendFinishedCandlesImmediatelly { get; set; }

	/// <inheritdoc />
	public IStorageBuffer Buffer { get; set; }

	private IMessageAdapter InnerAdapter => _adapter.InnerAdapter;

	/// <inheritdoc />
	public async ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					_series.Clear();
					_replaceId.Clear();
				}

				return ([message], []);
			}

			case MessageTypes.MarketData:
				return await ProcessMarketDataAsync((MarketDataMessage)message, cancellationToken);

			default:
				return ([message], []);
		}
	}

	/// <inheritdoc />
	public async ValueTask<(Message forward, Message[] extraOut)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var extraOut = new List<Message>();
		var forward = message;

		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				Buffer?.ProcessOutMessage(message);

				var response = (SubscriptionResponseMessage)message;
				var requestId = response.OriginalTransactionId;

				var (series, _) = await TryGetSeries(requestId, cancellationToken);

				if (series == null)
					break;

				if (response.IsOk())
				{
					if (series.Id == requestId)
						extraOut.Add(message);
				}
				else
					await UpgradeSubscriptionAsync(series, response, null, extraOut, cancellationToken);

				forward = null;
				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				Buffer?.ProcessOutMessage(message);

				var finishMsg = (SubscriptionFinishedMessage)message;
				var subscriptionId = finishMsg.OriginalTransactionId;

				var (series, _) = await TryGetSeries(subscriptionId, cancellationToken);

				if (series == null)
					break;

				await UpgradeSubscriptionAsync(series, null, finishMsg, extraOut, cancellationToken);
				forward = null;
				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var onlineMsg = (SubscriptionOnlineMessage)message;
				var subscriptionId = onlineMsg.OriginalTransactionId;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (_series.ContainsKey(subscriptionId))
						_logReceiver.AddInfoLog("Series online {0}.", subscriptionId);

					if (_replaceId.TryGetValue(subscriptionId, out var originalId))
						onlineMsg.OriginalTransactionId = originalId;
				}

				break;
			}

			case MessageTypes.Execution:
			case MessageTypes.QuoteChange:
			case MessageTypes.Level1Change:
			{
				if (message.Type == MessageTypes.QuoteChange && ((QuoteChangeMessage)message).State != null)
					break;

				if (message.Type == MessageTypes.Execution && !((ExecutionMessage)message).IsMarketData())
					break;

				var subscrMsg = (ISubscriptionIdMessage)message;

				if (await ProcessValueAsync(subscrMsg, extraOut, cancellationToken))
				{
					Buffer?.ProcessOutMessage(message);
					forward = null;
				}

				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
				{
					if (candleMsg.Type == MessageTypes.CandleTimeFrame)
					{
						if (await ProcessCandleSubscriptionsAsync(candleMsg, ProcessTimeFrameCandleAsync, extraOut, cancellationToken))
							forward = null;
					}
					else
					{
						if (await ProcessCandleSubscriptionsAsync(candleMsg, ProcessCandleAsync, extraOut, cancellationToken))
							forward = null;
					}
				}

				break;
			}
		}

		return (forward, extraOut.ToArray());
	}

	private async ValueTask<(Message[] toInner, Message[] toOut)> ProcessMarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var transactionId = mdMsg.TransactionId;

		if (mdMsg.IsSubscribe)
		{
			if (!_candleBuilderProvider.IsRegistered(mdMsg.DataType2.MessageType))
				return ([mdMsg], []);

			using (await _sync.LockAsync(cancellationToken))
			{
				if (_replaceId.ContainsKey(transactionId))
					return ([mdMsg], []);
			}

			var isLoadOnly = mdMsg.BuildMode == MarketDataBuildModes.Load;

			if (mdMsg.IsCalcVolumeProfile)
			{
				if (!InnerAdapter.IsSupportCandlesPriceLevels(mdMsg))
				{
					if (isLoadOnly)
					{
						return ([], [transactionId.CreateNotSupported()]);
					}
					else
					{
						var build = await TrySubscribeBuildAsync(mdMsg, cancellationToken);
						if (build == null)
							return ([], [transactionId.CreateNotSupported()]);

						return ([build], []);
					}
				}
			}

			if (mdMsg.BuildMode == MarketDataBuildModes.Build && mdMsg.BuildFrom?.IsTFCandles != true)
			{
				var build = await TrySubscribeBuildAsync(mdMsg, cancellationToken);
				if (build == null)
					return ([], [transactionId.CreateNotSupported()]);

				return ([build], []);
			}

			if (mdMsg.DataType2.IsTFCandles)
			{
				var originalTf = mdMsg.GetTimeFrame();
				var timeFrames = await InnerAdapter.GetTimeFramesAsync(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArrayAsync(cancellationToken);

				if (timeFrames.Contains(originalTf) || InnerAdapter.CheckTimeFrameByRequest)
				{
					if (!mdMsg.SecurityId.IsAllSecurity())
					{
						_logReceiver.AddDebugLog("Origin tf: {0}", originalTf);

						var original = mdMsg.TypedClone();

						if (mdMsg.To == null &&
							mdMsg.BuildMode == MarketDataBuildModes.LoadAndBuild &&
							!mdMsg.IsFinishedOnly &&
							!InnerAdapter.IsSupportCandlesUpdates(mdMsg) &&
							await InnerAdapter.TryGetCandlesBuildFromAsync(original, _candleBuilderProvider, cancellationToken) != null)
						{
							mdMsg.To = _logReceiver.CurrentTime;
						}

						using (await _sync.LockAsync(cancellationToken))
						{
							_series.Add(transactionId, new SeriesInfo(original, original)
							{
								State = SeriesStates.Regular,
								LastTime = original.From,
								Count = original.Count,
							});
						}
					}

					return ([mdMsg], []);
				}

				if (isLoadOnly)
					return ([], [transactionId.CreateNotSupported()]);

				if (mdMsg.AllowBuildFromSmallerTimeFrame)
				{
					var smaller = timeFrames
					    .FilterSmallerTimeFrames(originalTf)
					    .OrderByDescending()
					    .FirstOr()?
						.TimeFrame();

					if (smaller is DataType s)
					{
						_logReceiver.AddInfoLog("Smaller tf: {0}->{1}", originalTf, s.Arg);

						var original = mdMsg.TypedClone();

						var current = original.TypedClone();
						current.DataType2 = s;

						using (await _sync.LockAsync(cancellationToken))
						{
							_series.Add(transactionId, new SeriesInfo(original, current)
							{
								State = SeriesStates.SmallTimeFrame,
								BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), s),
								LastTime = original.From,
								Count = original.Count,
							});
						}

						return ([current], []);
					}
				}

				var build = await TrySubscribeBuildAsync(mdMsg, cancellationToken);
				if (build == null)
					return ([], [transactionId.CreateNotSupported()]);

				return ([build], []);
			}
			else
			{
				if (await InnerAdapter.IsCandlesSupportedAsync(mdMsg, cancellationToken))
				{
					_logReceiver.AddInfoLog("Origin arg: {0}", mdMsg.GetArg());

					var original = mdMsg.TypedClone();

					using (await _sync.LockAsync(cancellationToken))
					{
						_series.Add(transactionId, new SeriesInfo(original, original)
						{
							State = SeriesStates.Regular,
							LastTime = original.From,
							Count = original.Count,
						});
					}

					return ([mdMsg], []);
				}
				else
				{
					if (isLoadOnly)
						return ([], [transactionId.CreateNotSupported()]);

					var build = await TrySubscribeBuildAsync(mdMsg, cancellationToken);
					if (build == null)
						return ([], [transactionId.CreateNotSupported()]);

					return ([build], []);
				}
			}
		}
		else
		{
			var series = await TryRemoveSeries(mdMsg.OriginalTransactionId, cancellationToken);
			if (series is null)
				return ([mdMsg], []);

			var extraOut = new List<Message>();

			var unsubscribe = series.Current.TypedClone();

			unsubscribe.OriginalTransactionId = unsubscribe.TransactionId;
			unsubscribe.TransactionId = transactionId;
			unsubscribe.IsSubscribe = false;

			if (series.LiveCandleTransactionId is long liveTxId)
			{
				var liveUnsub = series.Original.TypedClone();
				liveUnsub.OriginalTransactionId = liveTxId;
				liveUnsub.TransactionId = _idGenerator.GetNextId();
				liveUnsub.IsSubscribe = false;

				liveUnsub.LoopBack(_adapter);
				extraOut.Add(liveUnsub);
			}

			return ([unsubscribe], extraOut.ToArray());
		}
	}

	private async ValueTask<(SeriesInfo info, long originalId)> TryGetSeries(long id, CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
		{
			if (_replaceId.TryGetValue(id, out var originalId))
				id = originalId;
			else
				originalId = id;

			return (_series.TryGetValue(id), originalId);
		}
	}

	private async ValueTask<SeriesInfo> TryRemoveSeries(long id, CancellationToken cancellationToken)
	{
		_logReceiver.AddDebugLog("Series removing {0}.", id);

		using (await _sync.LockAsync(cancellationToken))
		{
			if (!_series.TryGetAndRemove(id, out var series))
				return null;

			_replaceId.RemoveWhere(p => p.Value == id);
			return series;
		}
	}

	private async ValueTask<MarketDataMessage> TryCreateBuildSubscription(MarketDataMessage original, DateTime? lastTime, CancellationToken cancellationToken)
	{
		if (original == null)
			throw new ArgumentNullException(nameof(original));

		var buildFrom = await InnerAdapter.TryGetCandlesBuildFromAsync(original, _candleBuilderProvider, cancellationToken);

		if (buildFrom == null)
			return null;

		var current = new MarketDataMessage
		{
			DataType2 = buildFrom,
			From = lastTime,
			To = original.To,
			MaxDepth = original.MaxDepth,
			BuildField = original.BuildField,
			IsSubscribe = true,
		};

		original.CopyTo(current, false);

		_logReceiver.AddInfoLog("Build tf: {0}->{1}", buildFrom, original.GetArg());

		return current;
	}

	private async ValueTask<MarketDataMessage> TrySubscribeBuildAsync(MarketDataMessage original, CancellationToken cancellationToken)
	{
		var current = await TryCreateBuildSubscription(original, original.From, cancellationToken);

		if (current == null)
			return null;

		current.TransactionId = original.TransactionId;

		var series = new SeriesInfo(original.TypedClone(), current)
		{
			LastTime = current.From,
			Count = current.Count,
			Transform = CreateTransform(current),
			State = SeriesStates.Compress,
		};

		using (await _sync.LockAsync(cancellationToken))
			_series.Add(original.TransactionId, series);

		Buffer?.ProcessInMessage(current);
		return current;
	}

	private static ICandleBuilderValueTransform CreateTransform(MarketDataMessage current)
	{
		return CreateTransform(current.DataType2, current.BuildField, current.PriceStep, current.VolumeStep);
	}

	private static ICandleBuilderValueTransform CreateTransform(DataType dataType, Level1Fields? field, decimal? priceStep, decimal? volStep)
	{
		if (dataType == DataType.Ticks)
		{
			return new TickCandleBuilderValueTransform();
		}
		else if (dataType == DataType.MarketDepth)
		{
			var t = new QuoteCandleBuilderValueTransform(priceStep, volStep);

			if (field != null)
				t.Type = field.Value;

			return t;
		}
		else if (dataType == DataType.Level1)
		{
			var t = new Level1CandleBuilderValueTransform(priceStep, volStep);

			if (field != null)
				t.Type = field.Value;

			return t;
		}
		else if (dataType == DataType.OrderLog)
		{
			var t = new OrderLogCandleBuilderValueTransform();

			if (field != null)
				t.Type = field.Value;

			return t;
		}
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
	}

	private async ValueTask<bool> ProcessCandleSubscriptionsAsync(CandleMessage candleMsg, Func<SeriesInfo, CandleMessage, List<Message>, CancellationToken, ValueTask> processor, List<Message> extraOut, CancellationToken cancellationToken)
	{
		var subscriptionIds = candleMsg.GetSubscriptionIds();
		HashSet<long> newSubscriptionIds = null;

		foreach (var subscriptionId in subscriptionIds)
		{
			var (series, _) = await TryGetSeries(subscriptionId, cancellationToken);

			if (series == null)
				continue;

			newSubscriptionIds ??= [.. subscriptionIds];

			newSubscriptionIds.Remove(subscriptionId);

			await processor(series, candleMsg, extraOut, cancellationToken);
		}

		if (newSubscriptionIds != null)
		{
			if (newSubscriptionIds.Count == 0)
				return true;

			candleMsg.SetSubscriptionIds([.. newSubscriptionIds]);
		}

		return false;
	}

	private async ValueTask ProcessTimeFrameCandleAsync(SeriesInfo series, CandleMessage candleMsg, List<Message> extraOut, CancellationToken cancellationToken)
	{
		switch (series.State)
		{
			case SeriesStates.Regular:
				await ProcessCandleAsync(series, candleMsg, extraOut, cancellationToken);
				break;

			case SeriesStates.SmallTimeFrame:
				if (series.IsCountExhausted)
				{
					if (await TryRemoveSeries(series.Id, cancellationToken) is not null)
						extraOut.Add(new SubscriptionFinishedMessage { OriginalTransactionId = series.Original.TransactionId });

					break;
				}

				var candles = series.BigTimeFrameCompressor.Process(candleMsg);

				foreach (var bigCandle in candles)
				{
					var isFinished = bigCandle.State == CandleStates.Finished;

					if (series.Current.IsFinishedOnly && !isFinished)
						continue;

					bigCandle.SetSubscriptionIds(subscriptionId: series.Id);
					bigCandle.Adapter = candleMsg.Adapter;
					bigCandle.LocalTime = candleMsg.LocalTime;

					if (isFinished)
					{
						series.LastTime = bigCandle.CloseTime;

						if (series.Count != null)
						{
							if (series.IsCountExhausted)
								break;

							series.Count--;
						}
					}

					extraOut.Add(bigCandle.TypedClone());
				}

				break;

			default:
				break;
		}
	}

	private async ValueTask UpgradeSubscriptionAsync(SeriesInfo series, SubscriptionResponseMessage response, SubscriptionFinishedMessage finish, List<Message> extraOut, CancellationToken cancellationToken)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		async ValueTask FinishAsync()
		{
			if (await TryRemoveSeries(series.Id, cancellationToken) is null)
				return;

			if (response != null && !response.IsOk())
			{
				response = response.TypedClone();
				response.OriginalTransactionId = series.Original.TransactionId;
				extraOut.Add(response);
			}
			else
				extraOut.Add(new SubscriptionFinishedMessage { OriginalTransactionId = series.Original.TransactionId });
		}

		if (finish?.NextFrom != null)
			series.LastTime = finish.NextFrom;

		var original = series.Original;

		if (original.To != null && series.LastTime != null && original.To <= series.LastTime)
		{
			await FinishAsync();
			return;
		}

		if (original.Count != null && series.IsCountExhausted)
		{
			await FinishAsync();
			return;
		}

		switch (series.State)
		{
			case SeriesStates.Regular:
			{
				var isLoadOnly = series.Original.BuildMode == MarketDataBuildModes.Load;

				// upgrade to smaller tf only in case failed subscription
				if (response != null && original.DataType2.IsTFCandles && original.AllowBuildFromSmallerTimeFrame)
				{
					if (isLoadOnly)
					{
						await FinishAsync();
						return;
					}

					var smaller = (await InnerAdapter
						.GetTimeFramesAsync(original.SecurityId, series.LastTime, original.To)
						.ToArrayAsync(cancellationToken))
						.FilterSmallerTimeFrames(original.GetTimeFrame())
						.OrderByDescending()
						.FirstOr()?
						.TimeFrame();

					if (smaller is DataType s)
					{
						var newTransId = _idGenerator.GetNextId();

						var curr = series.Current = original.TypedClone();
						curr.DataType2 = s;
						curr.TransactionId = newTransId;

						series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), s);
						series.State = SeriesStates.SmallTimeFrame;
						series.NonFinishedCandle = null;

						using (await _sync.LockAsync(cancellationToken))
							_replaceId.Add(curr.TransactionId, series.Id);

						_logReceiver.AddInfoLog("Series smaller tf: ids {0}->{1}", original.TransactionId, newTransId);

						// loopback
						curr.LoopBack(_adapter);
						extraOut.Add(curr);

						return;
					}
				}

				if (response == null && isLoadOnly)
				{
					await FinishAsync();
					return;
				}

				series.State = SeriesStates.Compress;
				break;
			}
			case SeriesStates.SmallTimeFrame:
			{
				series.BigTimeFrameCompressor = null;
				series.State = SeriesStates.Compress;

				break;
			}
			case SeriesStates.Compress:
			{
				await FinishAsync();
				return;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(series), series.State, LocalizedStrings.InvalidValue);
		}

		if (series.State != SeriesStates.Compress)
			throw new InvalidOperationException(series.State.ToString());

		series.NonFinishedCandle = null;

		var current = await TryCreateBuildSubscription(original, series.LastTime, cancellationToken);

		if (current == null)
		{
			await FinishAsync();
			return;
		}

		current.TransactionId = _idGenerator.GetNextId();

		using (await _sync.LockAsync(cancellationToken))
			_replaceId.Add(current.TransactionId, series.Id);

		_logReceiver.AddInfoLog("Series compress: ids {0}->{1}", original.TransactionId, current.TransactionId);

		series.Transform = CreateTransform(current);
		series.Current = current;

		// loopback
		current.LoopBack(_adapter);
		extraOut.Add(current);

		// also send a live candle subscription so the adapter can provide live finished candles
		if (!original.IsHistoryOnly())
		{
			var liveCandleSub = original.TypedClone();
			liveCandleSub.TransactionId = _idGenerator.GetNextId();
			liveCandleSub.From = series.LastTime;
			liveCandleSub.IsFinishedOnly = true;

			using (await _sync.LockAsync(cancellationToken))
			{
				_replaceId.Add(liveCandleSub.TransactionId, series.Id);
				series.LiveCandleTransactionId = liveCandleSub.TransactionId;
			}

			liveCandleSub.LoopBack(_adapter);
			extraOut.Add(liveCandleSub);
		}
	}

	private async ValueTask ProcessCandleAsync(SeriesInfo info, CandleMessage candleMsg, List<Message> extraOut, CancellationToken cancellationToken)
	{
		if (info.LastTime != null && info.LastTime > candleMsg.OpenTime)
			return;

		if (info.IsCountExhausted)
		{
			if (await TryRemoveSeries(info.Id, cancellationToken) is not null)
				extraOut.Add(new SubscriptionFinishedMessage { OriginalTransactionId = info.Original.TransactionId });

			return;
		}

		info.LastTime = candleMsg.OpenTime;

		if (info.NonFinishedCandle is CandleMessage nonFinished)
		{
			if (nonFinished.OpenTime < candleMsg.OpenTime)
			{
				nonFinished.State = CandleStates.Finished;
				nonFinished.LocalTime = candleMsg.LocalTime;
				await RaiseNewOutCandleAsync(info, nonFinished, extraOut, cancellationToken);
				info.NonFinishedCandle = null;
			}
			else if (candleMsg.State == CandleStates.Finished)
				info.NonFinishedCandle = null;
		}

		candleMsg = _cloneOutCandles ? candleMsg.TypedClone() : candleMsg;

		if (candleMsg.Type == MessageTypes.CandleTimeFrame && !SendFinishedCandlesImmediatelly)
		{
			// make all incoming candles as Active until next come
			candleMsg.State = CandleStates.Active;
		}

		if (!info.Current.IsFinishedOnly || candleMsg.State == CandleStates.Finished)
			await RaiseNewOutCandleAsync(info, candleMsg, extraOut, cancellationToken);

		if (candleMsg.State != CandleStates.Finished)
			info.NonFinishedCandle = candleMsg.TypedClone();
	}

	private async ValueTask<bool> ProcessValueAsync(ISubscriptionIdMessage message, List<Message> extraOut, CancellationToken cancellationToken)
	{
		var subscriptionIds = message.GetSubscriptionIds();

		HashSet<long> newSubscriptionIds = null;

		foreach (var id in subscriptionIds)
		{
			var (series, subscriptionId) = await TryGetSeries(id, cancellationToken);

			if (series == null)
				continue;

			newSubscriptionIds ??= [.. subscriptionIds];

			newSubscriptionIds.Remove(id);

			var isAll = series.Original.SecurityId == default;

			if (isAll)
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					series = series.Child.SafeAdd(((ISecurityIdMessage)message).SecurityId, key =>
					{
						_logReceiver.AddDebugLog("New ALL candle-map: {0}/{1} TrId={2}", key, series.Original.DataType2, series.Original.TransactionId);

						var childOriginal = series.Original.TypedClone();
						childOriginal.SecurityId = key;

						return new SeriesInfo(childOriginal, series.Current)
						{
							LastTime = series.Original.From,
							Count = series.Original.Count,
							Transform = CreateTransform(series.Current),
							State = SeriesStates.Compress,
						};
					});
				}
			}

			var transform = series.Transform;

			if (transform?.Process((Message)message) != true)
				continue;

			var time = transform.Time;
			var origin = series.Original;

			if (origin.To != null && origin.To.Value < time)
			{
				if (await TryRemoveSeries(subscriptionId, cancellationToken) is not null)
					extraOut.Add(new SubscriptionFinishedMessage { OriginalTransactionId = origin.TransactionId });

				continue;
			}

			if (series.LastTime != null && series.LastTime.Value > time)
				continue;

			if (series.IsCountExhausted)
			{
				if (await TryRemoveSeries(subscriptionId, cancellationToken) is not null)
					extraOut.Add(new SubscriptionFinishedMessage { OriginalTransactionId = origin.TransactionId });

				continue;
			}

			series.LastTime = time;

			var builder = _candleBuilderProvider.Get(origin.DataType2.MessageType);

			var result = builder.Process(series, transform);

			foreach (var candleMessage in result)
			{
				if (series.Original.IsFinishedOnly && candleMessage.State != CandleStates.Finished)
					continue;

				await RaiseNewOutCandleAsync(series, candleMessage.TypedClone(), extraOut, cancellationToken);
			}
		}

		if (newSubscriptionIds != null)
		{
			if (newSubscriptionIds.Count == 0)
				return true;

			message.SetSubscriptionIds([.. newSubscriptionIds]);
		}

		return false;
	}

	private static ValueTask RaiseNewOutCandleAsync(SeriesInfo info, CandleMessage candleMsg, List<Message> extraOut, CancellationToken cancellationToken)
	{
		candleMsg.OriginalTransactionId = info.Id;
		candleMsg.SetSubscriptionIds(subscriptionId: info.Id);

		if (candleMsg.State == CandleStates.Finished)
		{
			if (info.Count != null)
				info.Count--;
		}

		extraOut.Add(candleMsg);
		return default;
	}
}
