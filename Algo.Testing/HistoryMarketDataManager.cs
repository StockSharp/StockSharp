namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Testing.Generation;

using System.Runtime.CompilerServices;

using Ecng.Reflection;

/// <summary>
/// History market data manager implementation.
/// </summary>
public class HistoryMarketDataManager : Disposable, IHistoryMarketDataManager
{
	private const int _maxGeneratedPerStep = 1000;

	private readonly Dictionary<(SecurityId secId, DataType dataType), (MarketDataGenerator generator, long transId)> _generators = [];
	private readonly List<(IMarketDataStorage storage, long subscriptionId)> _actions = [];
	private readonly AutoResetEvent _syncRoot = new(false);
	private readonly BasketMarketDataStorage<Message> _basketStorage = new();
	private readonly ITradingTimeLineGenerator _timeLineGenerator;

	private readonly Dictionary<string, int> _sentAtTime = [];

	// The last day each series replayed has history on, up to the run's last day; null when it has none.
	private readonly Dictionary<IMarketDataStorage, DateTime?> _lastDays = [];

	// The securities the replay has said have no history left.
	private readonly HashSet<SecurityId> _endedHistories = [];

	private bool _isChanged;
	private DateTime _currentTime;
	private DateTime _sentTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryMarketDataManager"/>.
	/// </summary>
	/// <param name="timeLineGenerator">Trading time line generator.</param>
	public HistoryMarketDataManager(ITradingTimeLineGenerator timeLineGenerator)
	{
		_timeLineGenerator = timeLineGenerator ?? throw new ArgumentNullException(nameof(timeLineGenerator));
	}

	/// <inheritdoc />
	public DateTime StartDate { get; set; } = DateTime.MinValue;

	/// <inheritdoc />
	public DateTime StopDate { get; set; } = DateTime.MaxValue;

	private TimeSpan _marketTimeChangedInterval = TimeSpan.FromSeconds(1);

	/// <inheritdoc />
	public TimeSpan MarketTimeChangedInterval
	{
		get => _marketTimeChangedInterval;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_marketTimeChangedInterval = value;
		}
	}

	private int _postTradeMarketTimeChangedCount = 2;

	/// <inheritdoc />
	public int PostTradeMarketTimeChangedCount
	{
		get => _postTradeMarketTimeChangedCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_postTradeMarketTimeChangedCount = value;
		}
	}

	/// <inheritdoc />
	public bool CheckTradableDates { get; set; }

	/// <inheritdoc />
	public IStorageRegistry StorageRegistry { get; set; }

	/// <inheritdoc />
	public IMarketDataDrive Drive { get; set; }

	private IMarketDataDrive DriveInternal => Drive ?? StorageRegistry?.DefaultDrive;

	/// <inheritdoc />
	public StorageFormats StorageFormat { get; set; }

	/// <inheritdoc />
	public MarketDataStorageCache StorageCache
	{
		get => _basketStorage.Cache;
		set => _basketStorage.Cache = value;
	}

	/// <inheritdoc />
	public MarketDataStorageCache AdapterCache { get; set; }

	/// <inheritdoc />
	public int LoadedMessageCount { get; private set; }

	/// <inheritdoc />
	public DateTime CurrentTime => _currentTime;

	/// <inheritdoc />
	public bool IsStarted { get; private set; }

	/// <inheritdoc />
	public async ValueTask<Exception> SubscribeAsync(MarketDataMessage message, CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
			throw new ArgumentException("Message must be subscription, not unsubscription.", nameof(message));

		var securityId = message.SecurityId;
		var dataType = message.DataType2;
		var transId = message.TransactionId;

		if (StorageRegistry == null)
			return new InvalidOperationException(LocalizedStrings.NotSupportedDataForSecurity.Put(dataType, securityId));

		if (dataType == DataType.Level1)
		{
			if (!HasGenerator(securityId, dataType))
				await AddStorageAsync(StorageRegistry.GetLevel1MessageStorage(securityId, Drive, StorageFormat), transId, cancellationToken);
		}
		else if (dataType == DataType.MarketDepth)
		{
			if (!HasGenerator(securityId, dataType))
				await AddStorageAsync(StorageRegistry.GetQuoteMessageStorage(securityId, Drive, StorageFormat, message.DoNotBuildOrderBookIncrement), transId, cancellationToken);
		}
		else if (dataType == DataType.Ticks)
		{
			if (!HasGenerator(securityId, dataType))
				await AddStorageAsync(StorageRegistry.GetTickMessageStorage(securityId, Drive, StorageFormat), transId, cancellationToken);
		}
		else if (dataType == DataType.OrderLog)
		{
			if (!HasGenerator(securityId, dataType))
				await AddStorageAsync(StorageRegistry.GetOrderLogMessageStorage(securityId, Drive, StorageFormat), transId, cancellationToken);
		}
		else if (dataType.IsCandles)
		{
			if (HasGenerator(securityId, DataType.Ticks))
				return new NotSupportedException(LocalizedStrings.NotSupportedDataForSecurity.Put(dataType, securityId));

			await AddStorageAsync(StorageRegistry.GetCandleMessageStorage(securityId, dataType, Drive, StorageFormat), transId, cancellationToken);
		}
		else
		{
			return new InvalidOperationException(LocalizedStrings.NotSupportedDataForSecurity.Put(dataType, securityId));
		}

		return null;
	}

	/// <inheritdoc />
	public void Unsubscribe(long originalTransactionId)
	{
		if (originalTransactionId == 0)
			throw new ArgumentException("Invalid transaction id.", nameof(originalTransactionId));

		_isChanged = true;
		_actions.Add((null, originalTransactionId));
		_syncRoot.Set();
	}

	/// <inheritdoc />
	public void RegisterGenerator(SecurityId securityId, DataType dataType, MarketDataGenerator generator, long transactionId)
	{
		if (generator == null)
			throw new ArgumentNullException(nameof(generator));

		_generators.Add((securityId, dataType), (generator, transactionId));

		// A generator is market data the time line has to run for, whether or not any history is subscribed.
		_isChanged = true;
		_syncRoot.Set();
	}

	/// <inheritdoc />
	public bool UnregisterGenerator(long originalTransactionId)
	{
		var key = _generators.FirstOrDefault(p => p.Value.transId == originalTransactionId).Key;

		if (key == default)
			return false;

		_generators.Remove(key);
		return true;
	}

	/// <inheritdoc />
	public bool HasGenerator(SecurityId securityId, DataType dataType)
		=> _generators.ContainsKey((securityId, dataType));

	/// <inheritdoc />
	public IAsyncEnumerable<DataType> GetSupportedDataTypesAsync(SecurityId securityId)
	{
		return Impl();

		async IAsyncEnumerable<DataType> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var dataTypes = new HashSet<DataType>();

			var drive = DriveInternal;

			if (drive != null)
			{
				var from = StartDate.Date;
				var to = StopDate.Date;

				foreach (var dataType in await drive.GetAvailableDataTypesAsync(securityId, StorageFormat).ToArrayAsync(cancellationToken))
				{
					// A series stored only outside the run is no history of the run: what the run has is used to build it
					// instead, or the subscription is refused. Asked without a security, the question is whether the data
					// type is stored at all.
					if (securityId == default || await drive.GetStorageDrive(securityId, dataType, StorageFormat).GetDatesAsync().AnyAsync(date => date >= from && date <= to, cancellationToken))
						dataTypes.Add(dataType);
				}
			}

			dataTypes.AddRange(_generators
				.Where(g => g.Key.secId == securityId)
				.Select(t => t.Key.dataType));

			foreach (var dataType in dataTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return dataType;
			}
		}
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message> StartAsync(IEnumerable<BoardMessage> boards)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		var boardsArray = boards.ToArray();
		var messageTypes = new[] { MessageTypes.Time };

		var startDateTime = StartDate;
		var stopDateTime = StopDate;

		return Impl();

		async IAsyncEnumerable<Message> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			IsStarted = true;

			_lastDays.Clear();
			_endedHistories.Clear();

			try
			{
				while (!IsDisposed && !cancellationToken.IsCancellationRequested)
				{
					// Use timeout to allow cancellation check and prevent deadlock if no subscriptions arrive
					if (!_syncRoot.WaitOne(TimeSpan.FromMilliseconds(100)))
						continue;

					_isChanged = false;

					foreach (var (storage, subscriptionId) in _actions.CopyAndClear())
					{
						if (storage != null)
							_basketStorage.InnerStorages.Add(storage, subscriptionId);
						else
							_basketStorage.InnerStorages.Remove(subscriptionId);
					}

					var currentTime = _currentTime == default ? startDateTime : _currentTime;
					var loadDateInUtc = currentTime.Date;
					var stopDateInUtc = stopDateTime.Date;

					var checkDates = CheckTradableDates && boardsArray.Length > 0;

					while (loadDateInUtc <= stopDateInUtc && !_isChanged && !cancellationToken.IsCancellationRequested)
					{
						// The replay moves _currentTime forward as messages go out, so each day is judged and
						// filtered against where the run has actually got to. Reusing the value captured before
						// the loop stands a closed day's synthetic timeline at the range's first moment, which
						// the emulator refuses as time running backwards.
						var dayTime = _currentTime == default ? currentTime : _currentTime;

						// A security with no history left before the run's stop is told so once, before the day after its last is
						// replayed: the emulator closes its market instead of trading it at its last price for the rest of the run.
						foreach (var securityId in await GetEndedHistoriesAsync(loadDateInUtc, startDateTime.Date, stopDateInUtc, cancellationToken))
							yield return new HistoryEndMessage { SecurityId = securityId, ServerTime = dayTime, LocalTime = dayTime };

						if (!checkDates || _timeLineGenerator.IsTradeDate(boardsArray, loadDateInUtc))
						{
							IAsyncEnumerable<Message> messages;
							bool noData;

							if (AdapterCache is not null)
							{
								messages = AdapterCache.GetMessagesAsync(GetBasketScope(), loadDateInUtc, _basketStorage.LoadAsync);
								noData = await messages.FirstOrDefaultAsync(cancellationToken) is null;
							}
							else
							{
								var enu = _basketStorage.LoadAsync(loadDateInUtc);
								var dataTypes = await enu.GetDataTypesAsync(cancellationToken);
								noData = !dataTypes.Except(messageTypes).Any();
								messages = enu;
							}

							var source = noData
								? new SyncAsyncEnumerable<Message>(GetEmptyDayTimeLine(boardsArray, loadDateInUtc))
								: messages;

							await foreach (var msg in FilterMessages(source, startDateTime, stopDateTime, dayTime, _sentTime, new(_sentAtTime)).WithCancellation(cancellationToken))
							{
								foreach (var outMsg in Replay(msg))
									yield return outMsg;

								if (_isChanged)
									break;
							}
						}

						loadDateInUtc += TimeSpan.FromDays(1);
					}

					// The run's clock reaches its stop whether or not history does.
					if (!_isChanged && _currentTime < stopDateTime)
					{
						foreach (var outMsg in Replay(new TimeMessage { ServerTime = stopDateTime, LocalTime = stopDateTime }))
							yield return outMsg;
					}

					if (!_isChanged)
					{
						yield return new EmulationStateMessage
						{
							LocalTime = stopDateTime,
							State = ChannelStates.Stopping,
						};

						break;
					}
				}
			}
			finally
			{
				IsStarted = false;
			}
		}
	}

	/// <inheritdoc />
	public void Stop()
	{
		_isChanged = true;
		_syncRoot.Set();
	}

	/// <inheritdoc />
	public void Reset()
	{
		_generators.Clear();
		_actions.Clear();
		_isChanged = false;
		_currentTime = default;
		_sentTime = default;
		_sentAtTime.Clear();
		_lastDays.Clear();
		_endedHistories.Clear();
		_basketStorage.InnerStorages.Clear();
		LoadedMessageCount = 0;
		IsStarted = false;
	}

	private async ValueTask AddStorageAsync(IMarketDataStorage storage, long transactionId, CancellationToken cancellationToken)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		if (transactionId == 0)
			throw new ArgumentException("Invalid transaction id.", nameof(transactionId));

		_isChanged = true;
		_actions.Add((storage, transactionId));
		_syncRoot.Set();
	}

	// What the replay is made of now, the series of the securities subscribed: a day cached for other series is not theirs.
	private string GetBasketScope()
		=> string.Join("|", _basketStorage.InnerStorages.SyncGet(c => c.ToArray())
			.Select(storage => $"{storage.SecurityId}/{storage.DataType}")
			.Order(StringComparer.Ordinal));

	// Securities whose history in the run is over by the day that begins: they had some, and no series of theirs replayed has
	// a day left before the run's stop. A security a generator makes data for never runs out; each is named once.
	private async ValueTask<SecurityId[]> GetEndedHistoriesAsync(DateTime day, DateTime firstDay, DateTime lastDay, CancellationToken cancellationToken)
	{
		var lastDays = new Dictionary<SecurityId, DateTime?>();

		foreach (var storage in _basketStorage.InnerStorages.SyncGet(c => c.ToArray()))
		{
			var last = await GetLastDayAsync(storage, lastDay, cancellationToken);

			if (lastDays.TryGetValue(storage.SecurityId, out var known) && (last is null || known > last))
				continue;

			lastDays[storage.SecurityId] = last;
		}

		var ended = new List<SecurityId>();

		foreach (var (securityId, last) in lastDays)
		{
			var isOver = last is DateTime l && l >= firstDay && l < day && !_generators.Keys.Any(key => key.secId == securityId);

			if (!isOver)
				_endedHistories.Remove(securityId);
			else if (_endedHistories.Add(securityId))
				ended.Add(securityId);
		}

		return [.. ended];
	}

	// The last day a series has history on, up to the run's last day. A storage that makes its data as it is read has it on
	// every day.
	private async ValueTask<DateTime?> GetLastDayAsync(IMarketDataStorage storage, DateTime lastDay, CancellationToken cancellationToken)
	{
		if (_lastDays.TryGetValue(storage, out var known))
			return known;

		DateTime? last = null;

		if (storage.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) is not null)
		{
			last = lastDay;
		}
		else
		{
			await foreach (var date in storage.GetDatesAsync().WithCancellation(cancellationToken))
			{
				if (date <= lastDay && (last is null || date > last))
					last = date;
			}
		}

		_lastDays[storage] = last;
		return last;
	}

	// A replayed message is told apart from others at the same moment by its kind and the subscriptions it is for.
	private static string GetSentKey(Message message)
		=> message is ISubscriptionIdMessage subscribed
			? $"{message.Type}:{string.Join(",", subscribed.GetSubscriptionIds())}"
			: message.Type.ToString();

	// What went out at the moment the replay has reached, so that a reload of the day after a subscription changes does not
	// send it again.
	private void CountSent(Message message, DateTime serverTime)
	{
		if (serverTime != _sentTime)
		{
			_sentTime = serverTime;
			_sentAtTime.Clear();
		}

		var key = GetSentKey(message);
		_sentAtTime[key] = _sentAtTime.TryGetValue(key) + 1;
	}

	// Marks a message replayed at its moment and gives what goes out for it: what the generators make by then, then the message.
	private IEnumerable<Message> Replay(Message message)
	{
		if (message.TryGetServerTime(out var serverTime))
		{
			_currentTime = serverTime;
			CountSent(message, serverTime);

			foreach (var generated in DriveGenerators(message, serverTime))
			{
				LoadedMessageCount++;
				yield return generated;
			}
		}

		LoadedMessageCount++;
		yield return message;
	}

	private static async IAsyncEnumerable<Message> FilterMessages(
		IAsyncEnumerable<Message> messages,
		DateTime fromTime,
		DateTime toTime,
		DateTime curTime,
		DateTime sentTime,
		Dictionary<string, int> sentAtTime)
	{
		await foreach (var msg in messages)
		{
			var msgServerTime = ((IServerTimeMessage)msg).ServerTime;

			if (msgServerTime < curTime)
				continue;

			// Sent already, before the day was reloaded for a change of the subscriptions.
			if (msgServerTime == sentTime && sentAtTime.Count > 0)
			{
				var key = GetSentKey(msg);

				if (sentAtTime.TryGetValue(key) is var sent and > 0)
				{
					sentAtTime[key] = sent - 1;
					continue;
				}
			}

			msg.LocalTime = msgServerTime;

			if (msg.LocalTime < fromTime)
			{
				if (msg.Type is MessageTypes.QuoteChange or MessageTypes.Execution)
					continue;
			}

			if (msg.LocalTime > toTime)
				break;

			yield return msg;
		}
	}

	/// <summary>
	/// Drives the registered generators with the replay clock and same-security data, returning any
	/// generated messages (interleaved by the caller at the same server time). A time signal advances
	/// time-based generation for every generator; the triggering message feeds the generators of its own
	/// security, and each generated message is chained into the other same-security generators, so a
	/// generated tick drives that security's order-book generator. Generators ignore their own output
	/// type, so the chain terminates.
	/// </summary>
	/// <param name="trigger">The replayed message that advanced the clock.</param>
	/// <param name="time">The current replay server time.</param>
	/// <returns>The generated messages, if any.</returns>
	private IEnumerable<Message> DriveGenerators(Message trigger, DateTime time)
	{
		if (_generators.Count == 0)
			yield break;

		var generators = _generators.Values.Select(v => v.generator).ToArray();

		var feeds = new Queue<Message>();
		feeds.Enqueue(new TimeMessage { ServerTime = time, LocalTime = time });

		if (trigger.Type != MessageTypes.Time && trigger is ISecurityIdMessage)
			feeds.Enqueue(trigger);

		var produced = 0;

		while (feeds.Count > 0)
		{
			var feed = feeds.Dequeue();
			var isTime = feed.Type == MessageTypes.Time;
			var feedSecId = (feed as ISecurityIdMessage)?.SecurityId;

			foreach (var generator in generators)
			{
				// A security-bearing feed only drives generators of the same security; a time signal
				// drives all of them.
				if (!isTime && feedSecId != generator.SecurityId)
					continue;

				var output = generator.Process(feed);

				if (output is null)
					continue;

				output.LocalTime = time;
				yield return output;

				// Chain the generated message into the other same-security generators.
				feeds.Enqueue(output);

				if (++produced > _maxGeneratedPerStep)
					yield break;
			}
		}
	}

	// A day without history runs the clock at the pace the replay keeps with data: through the boards' trading hours to the
	// session's end and the post-trade moments, or through the whole day when no board is known. With nothing replayed that
	// keeps a pace, the day is marked only where its hours begin and end.
	private IEnumerable<TimeMessage> GetEmptyDayTimeLine(BoardMessage[] boards, DateTime date)
	{
		if (GetReplayStep() is not TimeSpan step)
		{
			foreach (var msg in GetSimpleTimeLine(boards, date))
				yield return msg;

			yield break;
		}

		if (boards.Length == 0)
		{
			for (var time = TimeSpan.Zero; time < TimeSpan.FromDays(1); time += step)
				yield return new TimeMessage { ServerTime = date.Date + time };

			yield break;
		}

		var lastTime = TimeSpan.Zero;

		foreach (var (_, range) in _timeLineGenerator.GetOrderedRanges(boards, date))
		{
			for (var time = range.Min; time < range.Max; time += step)
				yield return new TimeMessage { ServerTime = date.Date + time };

			yield return new TimeMessage { ServerTime = date.Date + range.Max };
			lastTime = range.Max;
		}

		foreach (var msg in _timeLineGenerator.GetPostTradeTimeMessages(date, lastTime, MarketTimeChangedInterval, PostTradeMarketTimeChangedCount))
			yield return msg;
	}

	// How often the replay moves its clock: as often as the quickest of what it still replays that keeps a pace - time-frame
	// candles once a time frame, a generator once its interval or a clock interval - and never oftener than the clock interval.
	// Ticks, quotes and the order log keep none, and a series whose history is over is replayed no more.
	private TimeSpan? GetReplayStep()
	{
		var periods = _basketStorage.InnerStorages.SyncGet(c => c.ToArray())
			.Where(storage => storage.DataType.IsTFCandles && !_endedHistories.Contains(storage.SecurityId))
			.Select(storage => storage.DataType.GetTimeFrame())
			.Concat(_generators.Values.Select(v => v.generator.Interval > TimeSpan.Zero ? v.generator.Interval : MarketTimeChangedInterval))
			.ToArray();

		if (periods.Length == 0)
			return null;

		var quickest = periods.Min();

		return quickest > MarketTimeChangedInterval ? quickest : MarketTimeChangedInterval;
	}

	private IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTime date)
	{
		foreach (var msg in _timeLineGenerator.GetSimpleTimeLine(boards, date, MarketTimeChangedInterval))
			yield return msg;

		var ranges = _timeLineGenerator.GetOrderedRanges(boards, date);
		var lastTime = ranges.LastOrDefault().range?.Max ?? TimeSpan.Zero;

		foreach (var msg in _timeLineGenerator.GetPostTradeTimeMessages(date, lastTime, MarketTimeChangedInterval, PostTradeMarketTimeChangedCount))
			yield return msg;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Stop();
		_syncRoot.Dispose();
		base.DisposeManaged();
	}
}
