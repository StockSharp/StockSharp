namespace StockSharp.Algo.Testing;

/// <summary>
/// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
/// </summary>
public class HistoryMessageAdapter : MessageAdapter
{
	private readonly Dictionary<(SecurityId secId, DataType dataType), (MarketDataGenerator generator, long transId)> _generators = [];
	private readonly Dictionary<(SecurityId secId, DataType dataType), Func<DateTimeOffset, IEnumerable<Message>>> _historySources = [];

	private readonly List<Tuple<IMarketDataStorage, long>> _actions = [];
	private readonly SyncObject _moveNextSyncRoot = new();
	private readonly SyncObject _syncRoot = new();

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

		StartDate = DateTimeOffset.MinValue;
		StopDate = DateTimeOffset.MaxValue;

		this.AddMarketDataSupport();
		this.AddSupportedMessage(MessageTypes.EmulationState, null);
		this.AddSupportedMessage(ExtendedMessageTypes.HistorySource, true);
		this.AddSupportedMessage(ExtendedMessageTypes.Generator, true);
	}

	/// <summary>
	/// Date in history for starting the paper trading.
	/// </summary>
	public DateTimeOffset StartDate { get; set; }

	/// <summary>
	/// Date in history to stop the paper trading (date is included).
	/// </summary>
	public DateTimeOffset StopDate { get; set; }

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

	private DateTimeOffset _currentTime;

	/// <inheritdoc />
	public override DateTimeOffset CurrentTime => _currentTime;

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
	public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
	{
		return _supportedMarketDataTypes.SafeAdd(securityId, key =>
		{
			var drive = DriveInternal;

			if (drive == null)
				return [];

			var dataTypes = new HashSet<DataType>();
			
			dataTypes.AddRange(drive.GetAvailableDataTypes(securityId, StorageFormat));
			dataTypes.AddRange(_historySources.Select(s => s.Key.dataType));
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
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_generators.Clear();
				_historySources.Clear();

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

			case ExtendedMessageTypes.HistorySource:
			{
				var sourceMsg = (HistorySourceMessage)message;

				var key = (sourceMsg.SecurityId, sourceMsg.DataType2);

				if (sourceMsg.IsSubscribe)
					_historySources[key] = sourceMsg.GetMessages;
				else
					_historySources.Remove(key);

				break;
			}

			case MessageTypes.EmulationState:
			{
				var stateMsg = (EmulationStateMessage)message;

				switch (stateMsg.State)
				{
					case ChannelStates.Starting:
					{
						if (!_isStarted)
							Start(stateMsg.StartDate == default ? StartDate : stateMsg.StartDate, stateMsg.StopDate == default ? StopDate : stateMsg.StopDate);

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

			default:
				return false;
		}

		return true;
	}

	private void ProcessMarketDataMessage(MarketDataMessage message)
	{
		void AddStorage(IMarketDataStorage storage, long transactionId)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			LogInfo("Add storage: {0}/{1} {2}-{3}", storage.SecurityId, storage.DataType, storage.GetFromDate(), storage.GetToDate());

			_isChanged = true;
			_actions.Add(Tuple.Create(storage, transactionId));

			_syncRoot.PulseSignal();
		}

		void RemoveStorage(long originalTransactionId)
		{
			if (originalTransactionId == 0)
				throw new ArgumentNullException(nameof(originalTransactionId));

			_isChanged = true;
			_actions.Add(Tuple.Create((IMarketDataStorage)null, originalTransactionId));

			_syncRoot.PulseSignal();
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

		Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource()
		{
			Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource2(SecurityId s)
				=> _historySources.TryGetValue((s, dataType));

			return GetHistorySource2(securityId) ?? GetHistorySource2(default);
		}

		bool HasGenerator(DataType dt) => _generators.ContainsKey((securityId, dt));

		Exception error = null;

		if (dataType == DataType.Level1)
		{
			if (isSubscribe)
			{
				if (!HasGenerator(dataType))
				{
					var historySource = GetHistorySource();

					if (historySource == null)
					{
						AddStorage(StorageRegistry.GetLevel1MessageStorage(securityId, Drive, StorageFormat), transId);

						//AddStorage(new InMemoryMarketDataStorage<ClearingMessage>(security, null, date => new[]
						//{
						//	new ClearingMessage
						//	{
						//		LocalTime = date.Date + security.Board.ExpiryTime,
						//		SecurityId = securityId,
						//		ClearMarketDepth = true
						//	}
						//}), message.TransactionId);
					}
					else
					{
						AddStorage(new InMemoryMarketDataStorage<Level1ChangeMessage>(securityId, null, historySource), transId);
					}
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
					var historySource = GetHistorySource();

					AddStorage(historySource == null
						? StorageRegistry.GetQuoteMessageStorage(securityId, Drive, StorageFormat, message.DoNotBuildOrderBookIncrement)
						: new InMemoryMarketDataStorage<QuoteChangeMessage>(securityId, null, historySource),
						transId);
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
					var historySource = GetHistorySource();

					AddStorage(historySource == null
						? StorageRegistry.GetTickMessageStorage(securityId, Drive, StorageFormat)
						: new InMemoryMarketDataStorage<ExecutionMessage>(securityId, null, historySource),
						transId);
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
					var historySource = GetHistorySource();

					AddStorage(historySource == null
						? StorageRegistry.GetOrderLogMessageStorage(securityId, Drive, StorageFormat)
						: new InMemoryMarketDataStorage<ExecutionMessage>(securityId, null, historySource),
						transId);
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

				var historySource = GetHistorySource();
				var candleType = dataType.MessageType;
				var arg = message.GetArg();

				AddStorage(historySource == null
						? StorageRegistry.GetCandleMessageStorage(candleType, securityId, arg, Drive, StorageFormat)
						: new InMemoryMarketDataStorage<CandleMessage>(securityId, arg, historySource, candleType),
					transId);
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

	/// <summary>
	/// Start data loading.
	/// </summary>
	/// <param name="startDateTime">Datetime in history for starting the paper trading.</param>
	/// <param name="stopDateTime">Datetime in history to stop the paper trading (date is included).</param>
	private void Start(DateTimeOffset startDateTime, DateTimeOffset stopDateTime)
	{
		_isStarted = true;

		_cancellationToken = new CancellationTokenSource();

		ThreadingHelper
			.ThreadInvariant(() =>
			{
				try
				{
					var messageTypes = new[] { MessageTypes.Time/*, ExtendedMessageTypes.Clearing*/ };
					var token = _cancellationToken.Token;

					BoardMessage[] boards = null;

					while (!IsDisposed && !token.IsCancellationRequested)
					{
						_syncRoot.WaitSignal();

						_isChanged = false;

						_moveNextSyncRoot.PulseSignal();

						foreach (var action in _actions.CopyAndClear())
						{
							var storage = action.Item1;
							var subscriptionId = action.Item2;

							if (storage != null)
								_basketStorage.InnerStorages.Add(storage, subscriptionId);
							else
								_basketStorage.InnerStorages.Remove(subscriptionId);
						}

						boards ??= CheckTradableDates ? GetBoard() : [];

						var currentTime = _currentTime == default ? startDateTime : _currentTime;

						var loadDateInUtc = currentTime.UtcDateTime.Date;
						var stopDateInUtc = stopDateTime.UtcDateTime.Date;

						var checkDates = CheckTradableDates && boards.Length > 0;

						while (loadDateInUtc <= stopDateInUtc && !_isChanged && !token.IsCancellationRequested)
						{
							if (!checkDates || boards.Any(b => b.IsTradeDate(currentTime, true)))
							{
								LogInfo("Loading {0}", loadDateInUtc);

								IEnumerable<Message> messages;
								bool noData;

								if (AdapterCache is not null)
								{
									messages = AdapterCache.GetMessages(default, default, loadDateInUtc, _basketStorage.Load);
									noData = messages.IsEmpty();
								}
								else
								{
									var enu = _basketStorage.Load(loadDateInUtc);

									// storage for the specified date contains only time messages and clearing events
									noData = !enu.DataTypes.Except(messageTypes).Any();

									messages = enu;
								}

								if (noData)
									EnqueueMessages(startDateTime, stopDateTime, currentTime, GetSimpleTimeLine(boards, currentTime, MarketTimeChangedInterval), token);
								else
									EnqueueMessages(startDateTime, stopDateTime, currentTime, messages, token);
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
					SendOutMessage(ex.ToErrorMessage());

					SendOutMessage(new EmulationStateMessage
					{
						LocalTime = stopDateTime,
						State = ChannelStates.Stopping,
						Error = ex,
					});
				}
			})
			.Name(Name)
			.Launch();
	}

	/// <summary>
	/// Stop data loading.
	/// </summary>
	private void Stop()
	{
		_cancellationToken?.Cancel();
		_syncRoot.PulseSignal();
	}

	private void EnqueueMessages(DateTimeOffset fromTime, DateTimeOffset toTime, DateTimeOffset curTime, IEnumerable<Message> messages, CancellationToken token)
	{
		foreach (var msg in messages)
		{
			if (_isChanged || token.IsCancellationRequested)
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

	private static IEnumerable<(BoardMessage, Range<TimeSpan>)> GetOrderedRanges(BoardMessage[] boards, DateTimeOffset date)
	{
		if (boards is null)
			throw new ArgumentNullException(nameof(boards));

		var orderedRanges = boards
			.Where(b => b.IsTradeDate(date, true))
			.SelectMany(board =>
			{
				var period = board.WorkingTime.GetPeriod(date.ToLocalTime(board.TimeZone));

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
	private IEnumerable<TimeMessage> GetTimeLine(BoardMessage[] boards, DateTimeOffset date, TimeSpan interval)
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

	private IEnumerable<TimeMessage> GetSimpleTimeLine(BoardMessage[] boards, DateTimeOffset date, TimeSpan interval)
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

	private static DateTimeOffset GetTime(DateTimeOffset date, TimeSpan timeOfDay)
	{
		return (date.Date + timeOfDay).ApplyTimeZone(date.Offset);
	}

	private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTimeOffset date, TimeSpan lastTime, TimeSpan interval)
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
	protected override void SendOutMessage(Message message)
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