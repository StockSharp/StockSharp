namespace StockSharp.Algo.Testing;

using System.Runtime.CompilerServices;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// History market data manager implementation.
/// </summary>
public class HistoryMarketDataManager : Disposable, IHistoryMarketDataManager
{
	private readonly Dictionary<(SecurityId secId, DataType dataType), (MarketDataGenerator generator, long transId)> _generators = [];
	private readonly List<(IMarketDataStorage storage, long subscriptionId)> _actions = [];
	private readonly AutoResetEvent _syncRoot = new(false);
	private readonly BasketMarketDataStorage<Message> _basketStorage = new();

	private bool _isChanged;
	private DateTime _currentTime;

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
	public IEnumerable<DataType> GetSupportedDataTypes(SecurityId securityId)
	{
		var drive = DriveInternal;

		if (drive == null)
			return [];

		var dataTypes = new HashSet<DataType>();
		dataTypes.AddRange(drive.GetAvailableDataTypes(securityId, StorageFormat));
		dataTypes.AddRange(_generators.Select(t => t.Key.dataType));

		return dataTypes;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message> StartAsync(IEnumerable<BoardMessage> boards)
	{
		if (boards == null)
			throw new ArgumentNullException(nameof(boards));

		IsStarted = true;

		var boardsArray = boards.ToArray();
		var messageTypes = new[] { MessageTypes.Time };

		var startDateTime = StartDate;
		var stopDateTime = StopDate;

		return Impl();

		async IAsyncEnumerable<Message> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			while (!IsDisposed && !cancellationToken.IsCancellationRequested)
			{
				_syncRoot.WaitOne();

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
					if (!checkDates || boardsArray.Any(b => b.IsTradeDate(currentTime, true)))
					{
						IAsyncEnumerable<Message> messages;
						bool noData;

						if (AdapterCache is not null)
						{
							messages = AdapterCache.GetMessagesAsync(default, default, loadDateInUtc, _basketStorage.LoadAsync);
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
							? new SyncAsyncEnumerable<Message>(GetSimpleTimeLine(boardsArray, currentTime, MarketTimeChangedInterval))
							: messages;

						await foreach (var msg in FilterMessages(source, startDateTime, stopDateTime, currentTime).WithCancellation(cancellationToken))
						{
							LoadedMessageCount++;

							if (msg.TryGetServerTime(out var serverTime))
								_currentTime = serverTime;

							yield return msg;

							if (_isChanged)
								break;
						}
					}

					loadDateInUtc += TimeSpan.FromDays(1);
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

			IsStarted = false;
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
		_currentTime = default;
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

	private static async IAsyncEnumerable<Message> FilterMessages(
		IAsyncEnumerable<Message> messages,
		DateTime fromTime,
		DateTime toTime,
		DateTime curTime)
	{
		await foreach (var msg in messages)
		{
			var msgServerTime = ((IServerTimeMessage)msg).ServerTime;

			if (msgServerTime < curTime)
				continue;

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

	private IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTime date, TimeSpan interval)
	{
		var ranges = GetOrderedRanges(boards, date);
		var lastTime = TimeSpan.Zero;

		foreach (var range in ranges)
		{
			var time = GetTime(date, range.range.Min);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			time = GetTime(date, range.range.Max);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			lastTime = range.range.Max;
		}

		foreach (var m in GetPostTradeTimeMessages(date, lastTime, interval))
		{
			yield return m;
		}
	}

	private static IEnumerable<(BoardMessage board, Range<TimeSpan> range)> GetOrderedRanges(BoardMessage[] boards, DateTime date)
	{
		if (boards is null)
			throw new ArgumentNullException(nameof(boards));

		var orderedRanges = boards
			.Where(b => b.IsTradeDate(date, true))
			.SelectMany(board =>
			{
				var period = board.WorkingTime.GetPeriod(date);

				return period == null || period.Times.Count == 0
					? [(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay))]
					: period.Times.Select(t => (board, ranges: ToUtc(board, t)));
			})
			.OrderBy(i => i.ranges.Min)
			.ToList();

		for (var i = 0; i < orderedRanges.Count - 1;)
		{
			if (orderedRanges[i].ranges.Contains(orderedRanges[i + 1].ranges))
			{
				orderedRanges.RemoveAt(i + 1);
			}
			else if (orderedRanges[i + 1].ranges.Contains(orderedRanges[i].ranges))
			{
				orderedRanges.RemoveAt(i);
			}
			else if (orderedRanges[i].ranges.Intersect(orderedRanges[i + 1].ranges) != null)
			{
				orderedRanges[i] = (orderedRanges[i].board, new Range<TimeSpan>(orderedRanges[i].ranges.Min, orderedRanges[i + 1].ranges.Max));
				orderedRanges.RemoveAt(i + 1);
			}
			else
				i++;
		}

		return orderedRanges;
	}

	private static Range<TimeSpan> ToUtc(BoardMessage board, Range<TimeSpan> range)
	{
		var min = DateTime.MinValue + range.Min;
		var max = DateTime.MinValue + range.Max;

		var utcMin = min.To(board.TimeZone);
		var utcMax = max.To(board.TimeZone);

		return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
	}

	private static DateTime GetTime(DateTime date, TimeSpan timeOfDay)
		=> date.Date + timeOfDay;

	private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTime date, TimeSpan lastTime, TimeSpan interval)
	{
		for (var i = 0; i < PostTradeMarketTimeChangedCount; i++)
		{
			lastTime += interval;

			if (lastTime > TimeHelper.LessOneDay)
				break;

			yield return new TimeMessage
			{
				ServerTime = GetTime(date, lastTime)
			};
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Stop();
		_syncRoot.Dispose();
		base.DisposeManaged();
	}
}
