namespace StockSharp.Algo.Testing;

/// <summary>
/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
/// </summary>
public class HistoryMessageAdapter : MessageAdapter
{
	private readonly Dictionary<(SecurityId secId, DataType dataType), (MarketDataGenerator generator, long transId)> _generators = [];

	private readonly List<(IMarketDataStorage storage, long subscriptionId)> _actions = [];
	private readonly AutoResetEvent _syncRoot = new(false);

	private readonly BasketMarketDataStorage<Message> _basketStorage = new();

	private CancellationTokenSource _cancellationToken;

	private bool _isChanged;
	private bool _isStarted;

	/// <summary>
	/// The number of loaded events.
	/// </summary>
	public int LoadedMessageCount { get; private set; }

	private int _postTradeMarketTimeChangedCount = 2;

	/// <summary>
	/// The number of the event <see cref="ITimeProvider.CurrentTimeChanged"/> calls after end of trading. By default it is equal to 2.
	/// </summary>
	/// <remarks>
	/// It is required for activation of post-trade rules (rules, basing on events, occurring after end of trading).
	/// </remarks>
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

	/// <summary>
	/// Market data storage.
	/// </summary>
	public IStorageRegistry StorageRegistry { get; set; }

	/// <summary>
	/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
	/// </summary>
	public IMarketDataDrive Drive { get; set; }

	private IMarketDataDrive DriveInternal => Drive ?? StorageRegistry?.DefaultDrive;

	/// <summary>
	/// The format of market data. <see cref="StorageFormats.Binary"/> is used by default.
	/// </summary>
	public StorageFormats StorageFormat { get; set; }

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public ISecurityProvider SecurityProvider { get; }

	private TimeSpan _marketTimeChangedInterval = TimeSpan.FromSeconds(1);

	/// <summary>
	/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
	/// </summary>
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

	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
		: base(transactionIdGenerator)
	{
		SecurityProvider = securityProvider;

		StartDate = DateTime.MinValue;
		StopDate = DateTime.MaxValue;

		this.AddMarketDataSupport();
		this.AddSupportedMessage(MessageTypes.EmulationState, null);
		this.AddSupportedMessage(ExtendedMessageTypes.Generator, true);
	}

	/// <summary>
	/// Date in history for starting the paper trading.
	/// </summary>
	public DateTime StartDate { get; set; }

	/// <summary>
	/// Date in history to stop the paper trading (date is included).
	/// </summary>
	public DateTime StopDate { get; set; }

	/// <summary>
	/// Check loading dates are they tradable.
	/// </summary>
	public bool CheckTradableDates { get; set; }

	/// <summary>
	/// <see cref="BasketMarketDataStorage{T}.Cache"/>.
	/// </summary>
	public MarketDataStorageCache StorageCache
	{
		get => _basketStorage.Cache;
		set => _basketStorage.Cache = value;
	}

	/// <summary>
	/// <see cref="MarketDataStorageCache"/>.
	/// </summary>
	public MarketDataStorageCache AdapterCache { get; set; }

	/// <summary>
	/// Order book builders.
	/// </summary>
	public IDictionary<SecurityId, IOrderLogMarketDepthBuilder> OrderLogMarketDepthBuilders { get; } = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();

	/// <inheritdoc />
	public override IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
	{
		return OrderLogMarketDepthBuilders[securityId];
	}

	private DateTime _currentTime;

	/// <inheritdoc />
	public override DateTime CurrentTimeUtc => _currentTime;

	/// <inheritdoc />
	public override bool UseOutChannel => false;

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		Stop();

		base.DisposeManaged();
	}

	private readonly Dictionary<SecurityId, HashSet<DataType>> _supportedMarketDataTypes = [];

	/// <inheritdoc />
	public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
	{
		return _supportedMarketDataTypes.SafeAdd(securityId, key =>
		{
			var drive = DriveInternal;

			if (drive == null)
				return [];

			var dataTypes = new HashSet<DataType>();
			
			dataTypes.AddRange(drive.GetAvailableDataTypes(securityId, StorageFormat));
			dataTypes.AddRange(_generators.Select(t => t.Key.dataType));

			return dataTypes;
		});
	}

	/// <inheritdoc />
	public override bool IsFullCandlesOnly => false;

	/// <inheritdoc />
	public override bool IsSupportCandlesUpdates(MarketDataMessage subscription) => true;

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_generators.Clear();

				_currentTime = default;
				_basketStorage.InnerStorages.Clear();

				_supportedMarketDataTypes.Clear();

				LoadedMessageCount = 0;

				if (!_isStarted)
					SendOutMessage(new ResetMessage());

				_isStarted = false;

				break;
			}

			case MessageTypes.Connect:
			{
				if (_isStarted)
					throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

				SendOutMessage(new ConnectMessage { LocalTime = StartDate });
				break;
			}

			case MessageTypes.Disconnect:
			{
				_isStarted = false;

				SendOutMessage(new DisconnectMessage { LocalTime = StopDate });

				break;
			}

			case MessageTypes.SecurityLookup:
			{
				var lookupMsg = (SecurityLookupMessage)message;

				var securities = lookupMsg.SecurityId == default
						? SecurityProvider.LookupAll()
						: SecurityProvider.Lookup(lookupMsg);

				var processedBoards = new HashSet<ExchangeBoard>();

				foreach (var security in securities)
				{
					if (security.Board != null && processedBoards.Add(security.Board))
						SendOutMessage(security.Board.ToMessage());

					SendOutMessage(security.ToMessage(originalTransactionId: lookupMsg.TransactionId));
				}

				SendSubscriptionResult(lookupMsg);

				break;
			}

			case MessageTypes.MarketData:
				ProcessMarketDataMessage((MarketDataMessage)message);
				break;

			case MessageTypes.EmulationState:
			{
				var stateMsg = (EmulationStateMessage)message;

				switch (stateMsg.State)
				{
					case ChannelStates.Starting:
					{
						if (!_isStarted)
							Start(stateMsg.StartDate == default ? StartDate : stateMsg.StartDate, stateMsg.StopDate == default ? StopDate : stateMsg.StopDate, _cancellationToken?.Token ?? default);

						break;
					}

					case ChannelStates.Stopping:
					{
						Stop();
						break;
					}
				}

				SendOutMessage(stateMsg);
				break;
			}

			case ExtendedMessageTypes.Generator:
			{
				var generatorMsg = (GeneratorMessage)message;

				if (generatorMsg.IsSubscribe)
				{
					_generators.Add((generatorMsg.SecurityId, generatorMsg.DataType2), (generatorMsg.Generator, generatorMsg.TransactionId));
				}
				else
				{
					var key = _generators.FirstOrDefault(p => p.Value.transId == generatorMsg.OriginalTransactionId).Key;

					if (key != default)
						_generators.Remove(key);
				}

				break;
			}
		}

		return default;
	}

	private void ProcessMarketDataMessage(MarketDataMessage message)
	{
		void AddStorage(IMarketDataStorage storage, long transactionId)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			var storageDates = storage.GetDates();
			LogInfo("Add storage: {0}/{1} {2}-{3}", storage.SecurityId, storage.DataType, storageDates.FirstOr(), storageDates.LastOr());

			_isChanged = true;
			_actions.Add((storage, transactionId));

			_syncRoot.Set();
		}

		void RemoveStorage(long originalTransactionId)
		{
			if (originalTransactionId == 0)
				throw new ArgumentNullException(nameof(originalTransactionId));

			_isChanged = true;
			_actions.Add(((IMarketDataStorage)null, originalTransactionId));

			_syncRoot.Set();
		}

		var isSubscribe = message.IsSubscribe;
		var securityId = message.SecurityId;
		var dataType = message.DataType2;
		var transId = message.TransactionId;
		var originId = message.OriginalTransactionId;

		// security check must be done Start thread's loop
		//if (SecurityProvider.LookupById(securityId) == null)
		//{
		//	SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.SecurityNoFound.Put(securityId)));
		//	return;
		//}

		if (StorageRegistry == null)
		{
			SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.NotSupportedDataForSecurity.Put(dataType, securityId)));
			return;
		}

		bool HasGenerator(DataType dt) => _generators.ContainsKey((securityId, dt));

		Exception error = null;

		if (dataType == DataType.Level1)
		{
			if (isSubscribe)
			{
				if (!HasGenerator(dataType))
				{
					AddStorage(StorageRegistry.GetLevel1MessageStorage(securityId, Drive, StorageFormat), transId);
				}
			}
			else
			{
				RemoveStorage(originId);
				//RemoveStorage<InMemoryMarketDataStorage<ClearingMessage>>(security, ExtendedMessageTypes.Clearing, null);
			}
		}
		else if (dataType == DataType.MarketDepth)
		{
			if (isSubscribe)
			{
				if (!HasGenerator(dataType))
				{
					AddStorage(StorageRegistry.GetQuoteMessageStorage(securityId, Drive, StorageFormat, message.DoNotBuildOrderBookIncrement), transId);
				}
			}
			else
				RemoveStorage(originId);
		}
		else if (dataType == DataType.Ticks)
		{
			if (isSubscribe)
			{
				if (!HasGenerator(dataType))
				{
					AddStorage(StorageRegistry.GetTickMessageStorage(securityId, Drive, StorageFormat), transId);
				}
			}
			else
				RemoveStorage(originId);
		}
		else if (dataType == DataType.OrderLog)
		{
			if (isSubscribe)
			{
				if (!HasGenerator(dataType))
				{
					AddStorage(StorageRegistry.GetOrderLogMessageStorage(securityId, Drive, StorageFormat), transId);
				}
			}
			else
				RemoveStorage(originId);
		}
		else if (dataType.IsCandles)
		{
			if (isSubscribe)
			{
				if (HasGenerator(DataType.Ticks))
				{
					SendSubscriptionNotSupported(transId);
					return;
				}

				AddStorage(StorageRegistry.GetCandleMessageStorage(securityId, dataType, Drive, StorageFormat), transId);
			}
			else
				RemoveStorage(originId);
		}
		else
		{
			error = new InvalidOperationException(LocalizedStrings.NotSupportedDataForSecurity.Put(dataType, Messages.Extensions.AllSecurityId));
		}

		SendSubscriptionReply(transId, error);

		if (isSubscribe && error == null)
			SendSubscriptionResult(message);
	}

	private BoardMessage[] GetBoard()
		=> [.. SecurityProvider
		.LookupAll()
		.Select(s => s.Board)
		.Distinct()
		.Select(b => b.ToMessage())];

	private void Start(DateTime startDateTime, DateTime stopDateTime, CancellationToken cancellationToken)
	{
		_isStarted = true;

		_cancellationToken = new();

		_ = Task.Run(async () =>
		{
			await Task.Yield();

			try
			{
				var messageTypes = new[] { MessageTypes.Time/*, ExtendedMessageTypes.Clearing*/ };

				BoardMessage[] boards = null;

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

					boards ??= CheckTradableDates ? GetBoard() : [];

					var currentTime = _currentTime == default ? startDateTime : _currentTime;

					var loadDateInUtc = currentTime.Date;
					var stopDateInUtc = stopDateTime.Date;

					var checkDates = CheckTradableDates && boards.Length > 0;

					while (loadDateInUtc <= stopDateInUtc && !_isChanged && !cancellationToken.IsCancellationRequested)
					{
						if (!checkDates || boards.Any(b => b.IsTradeDate(currentTime, true)))
						{
							LogInfo("Loading {0}", loadDateInUtc);

							IAsyncEnumerable<Message> messages;
							bool noData;

							if (AdapterCache is not null)
							{
								messages = AdapterCache.GetMessagesAsync(default, default, loadDateInUtc, _basketStorage.LoadAsync, cancellationToken);
								noData = await messages.FirstOrDefaultAsync(cancellationToken) is null;
							}
							else
							{
								var enu = _basketStorage.LoadAsync(loadDateInUtc, cancellationToken);

								// storage for the specified date contains only time messages and clearing events
								noData = !enu.DataTypes.Except(messageTypes).Any();

								messages = enu;
							}

							if (noData)
								await EnqueueMessages(startDateTime, stopDateTime, currentTime, new SyncAsyncEnumerable<Message>(GetSimpleTimeLine(boards, currentTime, MarketTimeChangedInterval)));
							else
								await EnqueueMessages(startDateTime, stopDateTime, currentTime, messages);
						}

						loadDateInUtc += TimeSpan.FromDays(1);
					}

					if (!_isChanged)
					{
						SendOutMessage(new EmulationStateMessage
						{
							LocalTime = stopDateTime,
							State = ChannelStates.Stopping,
						});

						break;
					}
				}
			}
			catch (Exception ex)
			{
				if (!cancellationToken.IsCancellationRequested)
					SendOutMessage(ex.ToErrorMessage());

				SendOutMessage(new EmulationStateMessage
				{
					LocalTime = stopDateTime,
					State = ChannelStates.Stopping,
					Error = ex,
				});
			}
		}, cancellationToken);
	}

	/// <summary>
	/// Stop data loading.
	/// </summary>
	private void Stop()
	{
		_cancellationToken?.Cancel();
		_syncRoot.Set();
	}

	private async ValueTask EnqueueMessages(DateTime fromTime, DateTime toTime, DateTime curTime, IAsyncEnumerable<Message> messages)
	{
		await foreach (var msg in messages)
		{
			if (_isChanged)
				break;

			var msgServerTime = ((IServerTimeMessage)msg).ServerTime;

			if (msgServerTime < curTime)
				continue;

			msg.LocalTime = msgServerTime;

			if (msg.LocalTime < fromTime)
			{
				// не пропускаем только стаканы, тики и ОЛ
				if (msg.Type is MessageTypes.QuoteChange or MessageTypes.Execution)
					continue;
			}

			if (msg.LocalTime > toTime)
				break;

			SendOutMessage(msg);
		}
	}

	private static IEnumerable<(BoardMessage, Range<TimeSpan>)> GetOrderedRanges(BoardMessage[] boards, DateTime date)
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
					       : period.Times.Select(t => (board, ToUtc(board, t)));
			})
			.OrderBy(i => i.Item2.Min)
			.ToList();

		for (var i = 0; i < orderedRanges.Count - 1; )
		{
			if (orderedRanges[i].Item2.Contains(orderedRanges[i + 1].Item2))
			{
				orderedRanges.RemoveAt(i + 1);
			}
			else if (orderedRanges[i + 1].Item2.Contains(orderedRanges[i].Item2))
			{
				orderedRanges.RemoveAt(i);
			}
			else if (orderedRanges[i].Item2.Intersect(orderedRanges[i + 1].Item2) != null)
			{
				orderedRanges[i] = (orderedRanges[i].board, new Range<TimeSpan>(orderedRanges[i].Item2.Min, orderedRanges[i + 1].Item2.Max));
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

	/*
	private IEnumerable<TimeMessage> GetTimeLine(BoardMessage[] boards, DateTime date, TimeSpan interval)
	{
		var ranges = GetOrderedRanges(boards, date);
		var lastTime = TimeSpan.Zero;

		foreach (var range in ranges)
		{
			for (var time = range.Item2.Min; time <= range.Item2.Max; time += interval)
			{
				var serverTime = GetTime(date, time);

				if (serverTime.Date < date.Date)
					continue;

				lastTime = serverTime.TimeOfDay;
				yield return new TimeMessage { ServerTime = serverTime };
			}
		}

		foreach (var m in GetPostTradeTimeMessages(date, lastTime, interval))
		{
			yield return m;
		}
	}
	*/

	private IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTime date, TimeSpan interval)
	{
		var ranges = GetOrderedRanges(boards, date);
		var lastTime = TimeSpan.Zero;

		foreach (var range in ranges)
		{
			var time = GetTime(date, range.Item2.Min);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			time = GetTime(date, range.Item2.Max);
			if (time.Date >= date.Date)
				yield return new TimeMessage { ServerTime = time };

			lastTime = range.Item2.Max;
		}

		foreach (var m in GetPostTradeTimeMessages(date, lastTime, interval))
		{
			yield return m;
		}
	}

	private static DateTime GetTime(DateTime date, TimeSpan timeOfDay)
	{
		return date.Date + timeOfDay;
	}

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
	public override void SendOutMessage(Message message)
	{
		LoadedMessageCount++;

		if (message.TryGetServerTime(out var serverTime))
			_currentTime = serverTime;

		base.SendOutMessage(message);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Hist: {StartDate}-{StopDate}";
	}
}