namespace StockSharp.Algo.Candles.Compression;

using StockSharp.Algo.Testing;

/// <summary>
/// Candle builder adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
/// <param name="candleBuilderProvider">Candle builders provider.</param>
public class CandleBuilderMessageAdapter(IMessageAdapter innerAdapter, CandleBuilderProvider candleBuilderProvider) : MessageAdapterWrapper(innerAdapter)
{
	private enum SeriesStates
	{
		None,
		Regular,
		SmallTimeFrame,
		Compress,
	}

	private class SeriesInfo : ICandleBuilderSubscription
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

		public bool Stopped { get; set; }

		public bool IsCountExhausted => Count <= 0;
	}

	private readonly Lock _syncObject = new();

	private readonly Dictionary<long, SeriesInfo> _series = [];
	private readonly Dictionary<long, long> _replaceId = [];
	private readonly CandleBuilderProvider _candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
	private readonly Dictionary<long, SeriesInfo> _allChilds = [];
	private readonly Dictionary<long, RefPair<long, SubscriptionStates>> _pendingLoopbacks = [];
	private readonly bool _isHistory = innerAdapter.FindAdapter<HistoryMessageAdapter>() is not null;

	/// <summary>
	/// Send out finished candles when they received.
	/// </summary>
	public bool SendFinishedCandlesImmediatelly { get; set; }

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public IStorageBuffer Buffer { get; set; }

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_syncObject.EnterScope())
				{
					_series.Clear();
					_replaceId.Clear();
					_allChilds.Clear();
					_pendingLoopbacks.Clear();
				}

				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (!_candleBuilderProvider.IsRegistered(mdMsg.DataType2.MessageType))
					break;

				var transactionId = mdMsg.TransactionId;

				if (mdMsg.IsSubscribe)
				{
					using (_syncObject.EnterScope())
					{
						if (_replaceId.ContainsKey(transactionId))
							break;

						if (_pendingLoopbacks.TryGetAndRemove(transactionId, out var tuple))
						{
							if (tuple.Second != SubscriptionStates.Stopped)
							{
								if (tuple.Second == SubscriptionStates.Finished)
								{
									await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage
									{
										OriginalTransactionId = transactionId,
									}, cancellationToken);
								}
								else
								{
									await RaiseNewOutMessageAsync(new SubscriptionResponseMessage
									{
										OriginalTransactionId = transactionId,
										Error = new InvalidOperationException(LocalizedStrings.SubscriptionInvalidState.Put(transactionId, tuple.Second)),
									}, cancellationToken);
								}

								return;
							}

							tuple.Second = SubscriptionStates.Active;
							LogDebug("New ALL candle-map (active): {0}/{1} TrId={2}", mdMsg.SecurityId, tuple.Second, mdMsg.TransactionId);

							await RaiseNewOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
							return;
						}
					}

					var isLoadOnly = mdMsg.BuildMode == MarketDataBuildModes.Load;

					if (mdMsg.IsCalcVolumeProfile)
					{
						if (!IsSupportCandlesPriceLevels(mdMsg))
						{
							if (isLoadOnly)
							{
								await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);
							}
							else
							{
								if (!await TrySubscribeBuild(mdMsg, cancellationToken))
									await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);
							}

							return;
						}
					}

					if (mdMsg.BuildMode == MarketDataBuildModes.Build && mdMsg.BuildFrom?.IsTFCandles != true)
					{
						if (!await TrySubscribeBuild(mdMsg, cancellationToken))
							await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);

						return;
					}

					if (mdMsg.DataType2.IsTFCandles)
					{
						var originalTf = mdMsg.GetTimeFrame();
						var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArray();

						if (timeFrames.Contains(originalTf) || InnerAdapter.CheckTimeFrameByRequest)
						{
							if (!mdMsg.SecurityId.IsAllSecurity())
							{
								LogDebug("Origin tf: {0}", originalTf);

								var original = mdMsg.TypedClone();

								if (mdMsg.To == null &&
									mdMsg.BuildMode == MarketDataBuildModes.LoadAndBuild &&
									!mdMsg.IsFinishedOnly &&
									!InnerAdapter.IsSupportCandlesUpdates(mdMsg) &&
									InnerAdapter.TryGetCandlesBuildFrom(original, _candleBuilderProvider) != null)
								{
									mdMsg.To = CurrentTimeUtc;
								}

								using (_syncObject.EnterScope())
								{
									_series.Add(transactionId, new SeriesInfo(original, original)
									{
										State = SeriesStates.Regular,
										LastTime = original.From,
										Count = original.Count,
									});
								}
							}

							break;
						}

						if (isLoadOnly)
						{
							await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);

							return;
						}

						if (mdMsg.AllowBuildFromSmallerTimeFrame)
						{
							var smaller = timeFrames
							    .FilterSmallerTimeFrames(originalTf)
							    .OrderByDescending()
							    .FirstOr()?
								.TimeFrame();

							if (smaller is DataType s)
							{
								LogInfo("Smaller tf: {0}->{1}", originalTf, s.Arg);

								var original = mdMsg.TypedClone();

								var current = original.TypedClone();
								current.DataType2 = s;

								using (_syncObject.EnterScope())
								{
									_series.Add(transactionId, new SeriesInfo(original, current)
									{
										State = SeriesStates.SmallTimeFrame,
										BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), s),
										LastTime = original.From,
										Count = original.Count,
									});
								}

								await base.OnSendInMessageAsync(current, cancellationToken);
								return;
							}
						}

						if (!await TrySubscribeBuild(mdMsg, cancellationToken))
						{
							await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);
						}
					}
					else
					{
						if (InnerAdapter.IsCandlesSupported(mdMsg))
						{
							LogInfo("Origin arg: {0}", mdMsg.GetArg());

							var original = mdMsg.TypedClone();

							using (_syncObject.EnterScope())
							{
								_series.Add(transactionId, new SeriesInfo(original, original)
								{
									State = SeriesStates.Regular,
									LastTime = original.From,
									Count = original.Count,
								});
							}

							break;
						}
						else
						{
							if (isLoadOnly || !await TrySubscribeBuild(mdMsg, cancellationToken))
							{
								await RaiseNewOutMessageAsync(transactionId.CreateNotSupported(), cancellationToken);
							}
						}
					}

					return;
				}
				else
				{
					if (!TryRemoveSeries(mdMsg.OriginalTransactionId, out var series))
					{
						var sentResponse = false;

						using (_syncObject.EnterScope())
						{
							if (_allChilds.TryGetAndRemove(mdMsg.OriginalTransactionId, out var child))
							{
								child.Stopped = true;
								sentResponse = true;
							}
						}

						if (sentResponse)
						{
							await RaiseNewOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
							return;
						}

						break;
					}
					else
					{
						// TODO sub childs
					}

					var unsubscribe = series.Current.TypedClone();

					unsubscribe.OriginalTransactionId = unsubscribe.TransactionId;
					unsubscribe.TransactionId = transactionId;
					unsubscribe.IsSubscribe = false;

					message = unsubscribe;

					break;
				}
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	private SeriesInfo TryGetSeries(long id, out long originalId)
	{
		using (_syncObject.EnterScope())
		{
			if (_replaceId.TryGetValue(id, out originalId))
				id = originalId;
			else
				originalId = id;

			return _series.TryGetValue(id);
		}
	}

	private bool TryRemoveSeries(long id, out SeriesInfo series)
	{
		LogDebug("Series removing {0}.", id);

		using (_syncObject.EnterScope())
		{
			if (!_series.TryGetAndRemove(id, out series))
				return false;

			_replaceId.RemoveWhere(p => p.Value == id);
			return true;
		}
	}

	private MarketDataMessage TryCreateBuildSubscription(MarketDataMessage original, DateTime? lastTime)
	{
		if (original == null)
			throw new ArgumentNullException(nameof(original));

		var buildFrom = InnerAdapter.TryGetCandlesBuildFrom(original, _candleBuilderProvider);

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

		LogInfo("Build tf: {0}->{1}", buildFrom, original.GetArg());

		return current;
	}

	private async ValueTask<bool> TrySubscribeBuild(MarketDataMessage original, CancellationToken cancellationToken)
	{
		var current = TryCreateBuildSubscription(original, original.From);

		if (current == null)
			return false;

		current.TransactionId = original.TransactionId;

		var series = new SeriesInfo(original.TypedClone(), current)
		{
			LastTime = current.From,
			Count = current.Count,
			Transform = CreateTransform(current),
			State = SeriesStates.Compress,
		};

		using (_syncObject.EnterScope())
			_series.Add(original.TransactionId, series);

		Buffer?.ProcessInMessage(current);
		await base.OnSendInMessageAsync(current, cancellationToken);
		return true;
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

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				Buffer?.ProcessOutMessage(message);

				var response = (SubscriptionResponseMessage)message;
				var requestId = response.OriginalTransactionId;

				var series = TryGetSeries(requestId, out _);

				if (series == null)
					break;

				if (response.IsOk())
				{
					if (series.Id == requestId)
						await RaiseNewOutMessageAsync(message, cancellationToken);
				}
				else
					await UpgradeSubscriptionAsync(series, response, null, cancellationToken);

				return;
			}

			case MessageTypes.SubscriptionFinished:
			{
				Buffer?.ProcessOutMessage(message);

				var finishMsg = (SubscriptionFinishedMessage)message;
				var subscriptionId = finishMsg.OriginalTransactionId;

				var series = TryGetSeries(subscriptionId, out _);

				if (series == null)
					break;

				await UpgradeSubscriptionAsync(series, null, finishMsg, cancellationToken);
				return;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var onlineMsg = (SubscriptionOnlineMessage)message;
				var subscriptionId = onlineMsg.OriginalTransactionId;

				using (_syncObject.EnterScope())
				{
					if (_series.ContainsKey(subscriptionId))
						LogInfo("Series online {0}.", subscriptionId);

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

				if (await ProcessValueAsync(subscrMsg, cancellationToken))
				{
					Buffer?.ProcessOutMessage(message);
					return;
				}

				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
				{
					if (candleMsg.Type == MessageTypes.CandleTimeFrame)
					{
						if (await ProcessCandleSubscriptionsAsync(candleMsg, ProcessTimeFrameCandleAsync, cancellationToken))
							return;
					}
					else
					{
						if (await ProcessCandleSubscriptionsAsync(candleMsg, ProcessCandleAsync, cancellationToken))
							return;
					}
				}

				break;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private async ValueTask<bool> ProcessCandleSubscriptionsAsync(CandleMessage candleMsg, Func<SeriesInfo, CandleMessage, CancellationToken, ValueTask> processor, CancellationToken cancellationToken)
	{
		var subscriptionIds = candleMsg.GetSubscriptionIds();
		HashSet<long> newSubscriptionIds = null;

		foreach (var subscriptionId in subscriptionIds)
		{
			var series = TryGetSeries(subscriptionId, out _);

			if (series == null)
				continue;

			newSubscriptionIds ??= [.. subscriptionIds];

			newSubscriptionIds.Remove(subscriptionId);

			await processor(series, candleMsg, cancellationToken);
		}

		if (newSubscriptionIds != null)
		{
			if (newSubscriptionIds.Count == 0)
				return true;

			candleMsg.SetSubscriptionIds([.. newSubscriptionIds]);
		}

		return false;
	}

	private async ValueTask ProcessTimeFrameCandleAsync(SeriesInfo series, CandleMessage candleMsg, CancellationToken cancellationToken)
	{
		switch (series.State)
		{
			case SeriesStates.Regular:
				await ProcessCandleAsync(series, candleMsg, cancellationToken);
				break;

			case SeriesStates.SmallTimeFrame:
				if (series.IsCountExhausted)
				{
					if (TryRemoveSeries(series.Id, out _))
						await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = series.Original.TransactionId }, cancellationToken);

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

					await base.OnInnerAdapterNewOutMessageAsync(/*_isHistory ? bigCandle : */bigCandle.TypedClone(), cancellationToken);
				}

				break;

			default:
				break;
		}
	}

	private async ValueTask UpgradeSubscriptionAsync(SeriesInfo series, SubscriptionResponseMessage response, SubscriptionFinishedMessage finish, CancellationToken cancellationToken)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		var original = series.Original;

		async ValueTask FinishAsync()
		{
			if (!TryRemoveSeries(series.Id, out _))
				return;

			if (response != null && !response.IsOk())
			{
				response = response.TypedClone();
				response.OriginalTransactionId = original.TransactionId;
				await RaiseNewOutMessageAsync(response, cancellationToken);
			}
			else
				await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = original.TransactionId }, cancellationToken);
		}

		if (finish?.NextFrom != null)
			series.LastTime = finish.NextFrom;

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

					var smaller = InnerAdapter
						.GetTimeFrames(original.SecurityId, series.LastTime, original.To)
					    .FilterSmallerTimeFrames(original.GetTimeFrame())
					    .OrderByDescending()
					    .FirstOr()?
						.TimeFrame();

					if (smaller is DataType s)
					{
						var newTransId = TransactionIdGenerator.GetNextId();

						var curr = series.Current = original.TypedClone();
						curr.DataType2 = s;
						curr.TransactionId = newTransId;

						series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), s);
						series.State = SeriesStates.SmallTimeFrame;
						series.NonFinishedCandle = null;

						using (_syncObject.EnterScope())
							_replaceId.Add(curr.TransactionId, series.Id);

						LogInfo("Series smaller tf: ids {0}->{1}", original.TransactionId, newTransId);

						// loopback
						curr.LoopBack(this);
						await RaiseNewOutMessageAsync(curr, cancellationToken);

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
			//case SeriesStates.None:
			default:
				throw new ArgumentOutOfRangeException(nameof(series), series.State, LocalizedStrings.InvalidValue);
		}

		if (series.State != SeriesStates.Compress)
			throw new InvalidOperationException(series.State.ToString());

		series.NonFinishedCandle = null;

		var current = TryCreateBuildSubscription(original, series.LastTime);

		if (current == null)
		{
			await FinishAsync();
			return;
		}

		current.TransactionId = TransactionIdGenerator.GetNextId();

		using (_syncObject.EnterScope())
			_replaceId.Add(current.TransactionId, series.Id);

		LogInfo("Series compress: ids {0}->{1}", original.TransactionId, current.TransactionId);

		series.Transform = CreateTransform(current);
		series.Current = current;

		// loopback
		current.LoopBack(this);
		await RaiseNewOutMessageAsync(current, cancellationToken);
	}

	private async ValueTask ProcessCandleAsync(SeriesInfo info, CandleMessage candleMsg, CancellationToken cancellationToken)
	{
		if (info.LastTime != null && info.LastTime > candleMsg.OpenTime)
			return;

		if (info.IsCountExhausted)
		{
			if (TryRemoveSeries(info.Id, out _))
				await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = info.Original.TransactionId }, cancellationToken);

			return;
		}

		info.LastTime = candleMsg.OpenTime;

		if (info.NonFinishedCandle is CandleMessage nonFinished)
		{
			if (nonFinished.OpenTime < candleMsg.OpenTime)
			{
				nonFinished.State = CandleStates.Finished;
				nonFinished.LocalTime = candleMsg.LocalTime;
				await RaiseNewOutCandleAsync(info, nonFinished, cancellationToken);
				info.NonFinishedCandle = null;
			}
			else if (candleMsg.State == CandleStates.Finished)
				info.NonFinishedCandle = null;
		}

		candleMsg = _isHistory ? candleMsg : candleMsg.TypedClone();

		if (candleMsg.Type == MessageTypes.CandleTimeFrame && !SendFinishedCandlesImmediatelly)
		{
			// make all incoming candles as Active until next come
			candleMsg.State = CandleStates.Active;
		}

		if (!info.Current.IsFinishedOnly || candleMsg.State == CandleStates.Finished)
			await RaiseNewOutCandleAsync(info, candleMsg, cancellationToken);

		if (candleMsg.State != CandleStates.Finished)
			info.NonFinishedCandle = candleMsg.TypedClone();
	}

	private async ValueTask<bool> ProcessValueAsync(ISubscriptionIdMessage message, CancellationToken cancellationToken)
	{
		var subscriptionIds = message.GetSubscriptionIds();

		HashSet<long> newSubscriptionIds = null;

		foreach (var id in subscriptionIds)
		{
			var series = TryGetSeries(id, out var subscriptionId);

			if (series == null)
				continue;

			newSubscriptionIds ??= [.. subscriptionIds];

			newSubscriptionIds.Remove(id);

			var isAll = series.Original.SecurityId == default;

			if (isAll)
			{
				SubscriptionSecurityAllMessage allMsg = null;

				using (_syncObject.EnterScope())
				{
					series = series.Child.SafeAdd(((ISecurityIdMessage)message).SecurityId, key =>
					{
						allMsg = new SubscriptionSecurityAllMessage();

						series.Original.CopyTo(allMsg);

						allMsg.ParentTransactionId = series.Original.TransactionId;
						allMsg.TransactionId = TransactionIdGenerator.GetNextId();
						allMsg.SecurityId = key;

						allMsg.LoopBack(this, MessageBackModes.Chain);
						_pendingLoopbacks.Add(allMsg.TransactionId, RefTuple.Create(allMsg.ParentTransactionId, SubscriptionStates.Stopped));

						LogDebug("New ALL candle-map: {0}/{1} TrId={2}-{3}", key, series.Original.DataType2, allMsg.ParentTransactionId, allMsg.TransactionId);

						return new SeriesInfo(allMsg, allMsg)
						{
							LastTime = allMsg.From,
							Count = allMsg.Count,
							Transform = CreateTransform(series.Current),
							State = SeriesStates.Compress,
						};
					});

					if (series.Stopped)
						continue;
				}

				if (allMsg != null)
					await RaiseNewOutMessageAsync(allMsg, cancellationToken);
			}

			var transform = series.Transform;

			if (transform?.Process((Message)message) != true)
				continue;

			var time = transform.Time;
			var origin = series.Original;

			if (origin.To != null && origin.To.Value < time)
			{
				if (TryRemoveSeries(subscriptionId, out _))
					await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = origin.TransactionId }, cancellationToken);

				continue;
			}

			if (series.LastTime != null && series.LastTime.Value > time)
				continue;

			if (series.IsCountExhausted)
			{
				if (TryRemoveSeries(subscriptionId, out _))
					await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = origin.TransactionId }, cancellationToken);

				continue;
			}

			series.LastTime = time;

			var builder = _candleBuilderProvider.Get(origin.DataType2.MessageType);

			var result = builder.Process(series, transform);

			foreach (var candleMessage in result)
			{
				if (series.Original.IsFinishedOnly && candleMessage.State != CandleStates.Finished)
					continue;

				await RaiseNewOutCandleAsync(series, candleMessage.TypedClone(), cancellationToken);
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

	private async ValueTask RaiseNewOutCandleAsync(SeriesInfo info, CandleMessage candleMsg, CancellationToken cancellationToken)
	{
		candleMsg.OriginalTransactionId = info.Id;
		candleMsg.SetSubscriptionIds(subscriptionId: info.Id);

		if (candleMsg.State == CandleStates.Finished)
		{
			if (info.Count != null)
				info.Count--;
		}

		await RaiseNewOutMessageAsync(candleMsg, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new CandleBuilderMessageAdapter(InnerAdapter.TypedClone(), _candleBuilderProvider)
		{
			SendFinishedCandlesImmediatelly = SendFinishedCandlesImmediatelly,
			Buffer = Buffer,
		};
	}
}