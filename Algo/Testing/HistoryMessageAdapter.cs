namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
    using System.Globalization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
    using StockSharp.Logging;

	using SourceKey = System.Tuple<Messages.SecurityId, Messages.DataType>;

    /// <summary>
    /// The adapter, receiving messages form the storage <see cref="IStorageRegistry"/>.
    /// </summary>
    public class HistoryMessageAdapter : MessageAdapter
	{
		private readonly Dictionary<SourceKey, Tuple<MarketDataGenerator, long>> _generators = new Dictionary<SourceKey, Tuple<MarketDataGenerator, long>>();
		private readonly Dictionary<SourceKey, Func<DateTimeOffset, IEnumerable<Message>>> _historySources = new Dictionary<SourceKey, Func<DateTimeOffset, IEnumerable<Message>>>();

		private readonly List<Tuple<IMarketDataStorage, long>> _actions = new List<Tuple<IMarketDataStorage, long>>();
		private readonly SyncObject _moveNextSyncRoot = new SyncObject();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly BasketMarketDataStorage<Message> _basketStorage;

		private CancellationTokenSource _cancellationToken;

		private bool _isChanged;
		private bool _isStarted;

		/// <summary>
		/// The number of loaded events.
		/// </summary>
		public int LoadedMessageCount { get; private set; }

		private int _postTradeMarketTimeChangedCount = 2;

		/// <summary>
		/// The number of the event <see cref="IConnector.MarketTimeChanged"/> calls after end of trading. By default it is equal to 2.
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
					throw new ArgumentOutOfRangeException();

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
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// List of all exchange boards, for which instruments are loaded.
		/// </summary>
		public IEnumerable<ExchangeBoard> Boards { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		public HistoryMessageAdapter(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
			: base(transactionIdGenerator)
		{
			SecurityProvider = securityProvider;

			Boards = SecurityProvider
				.LookupAll()
				.Select(s => s.Board)
				.Distinct();

			_basketStorage = new BasketMarketDataStorage<Message>();

			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;

			this.AddMarketDataSupport();
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState, null);
			this.AddSupportedMessage(ExtendedMessageTypes.HistorySource, true);
			this.AddSupportedMessage(ExtendedMessageTypes.Generator, true);
			this.AddSupportedMessage(ExtendedMessageTypes.ChangeTimeInterval, null);
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
		public bool CheckTradableDates { get; set; } = true;

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

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Stop();

			base.DisposeManaged();
		}

		private IEnumerable<DataType> _supportedMarketDataTypes;

		/// <inheritdoc />
		public override IEnumerable<DataType> SupportedMarketDataTypes
		{
			get
			{
				if (_supportedMarketDataTypes == null)
				{
					var drive = DriveInternal;

					var dataTypes = drive.GetAvailableDataTypes(default, StorageFormat);

					_supportedMarketDataTypes = dataTypes
						//.Select(dt => dt.ToMarketDataType())
						//.Where(t => t != null)
						//.Select(t => t.Value)
						.Distinct()
						.ToArray();
				}
				
				return _supportedMarketDataTypes;
			}
		}

		/// <inheritdoc />
		public override bool IsFullCandlesOnly => false;

		/// <inheritdoc />
		public override bool IsSupportCandlesUpdates => true;

		/// <inheritdoc />
		public override IEnumerable<object> GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		{
			var drive = DriveInternal;

			if (drive == null)
				return Enumerable.Empty<object>();

			var args = _historySources
	             .Where(t => t.Key.Item2.MessageType == candleType && (t.Key.Item1 == securityId || t.Key.Item1.IsDefault()))
	             .Select(s => s.Key.Item2.Arg)
	             .ToArray();

			if (args.Length > 0)
				return args;

			args = _generators
	             .Where(t => t.Key.Item2.MessageType == candleType && (t.Key.Item1 == securityId || t.Key.Item1.IsDefault()))
	             .Select(s => s.Key.Item2.Arg)
	             .ToArray();

			if (args.Length > 0)
				return args;

			return drive.GetCandleArgs(StorageFormat, candleType, securityId, from, to);
		}

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
					_currentTime = default;

					_generators.Clear();
					_historySources.Clear();

                    _currentTime = DateTimeOffset.MinValue;
					_basketStorage.InnerStorages.Clear();
					
					LoadedMessageCount = 0;

					if (!_isStarted)
						SendOutMessage(new ResetMessage());

					_isStarted = false;

					break;
				}

				case MessageTypes.Connect:
				{
					if (_isStarted)
						throw new InvalidOperationException(LocalizedStrings.Str1116);

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

					var securities = lookupMsg.SecurityId.IsDefault() 
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

					var key = Tuple.Create(sourceMsg.SecurityId, sourceMsg.DataType2);

					if (sourceMsg.IsSubscribe)
						_historySources[key] = sourceMsg.GetMessages;
					else
						_historySources.Remove(key);

					break;
				}

				case ExtendedMessageTypes.EmulationState:
				{
					var stateMsg = (EmulationStateMessage)message;

					switch (stateMsg.State)
					{
						case ChannelStates.Starting:
						{
							if (!_isStarted)
								Start(stateMsg.StartDate.IsDefault() ? StartDate : stateMsg.StartDate, stateMsg.StopDate.IsDefault() ? StopDate : stateMsg.StopDate);

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
						_generators.Add(Tuple.Create(generatorMsg.SecurityId, generatorMsg.DataType2), Tuple.Create(generatorMsg.Generator, generatorMsg.TransactionId));
					}
					else
					{
						var pair = _generators.FirstOrDefault(p => p.Value.Item2 == generatorMsg.OriginalTransactionId);

						if (pair.Key != default)
							_generators.Remove(pair.Key);
					}

					break;
				}

				case ExtendedMessageTypes.ChangeTimeInterval:
				{
					var intervalMsg = (ChangeTimeIntervalMessage)message;
					MarketTimeChangedInterval = intervalMsg.Interval;
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

			if (SecurityProvider.LookupById(securityId) == null)
			{
				SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId)));
				return;
			}

			if (StorageRegistry == null)
			{
				SendSubscriptionReply(transId, new InvalidOperationException(LocalizedStrings.Str1117Params.Put(dataType, securityId)));
				return;
			}

			Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource()
			{
				Func<DateTimeOffset, IEnumerable<Message>> GetHistorySource2(SecurityId s)
					=> _historySources.TryGetValue(Tuple.Create(s, dataType));

				return GetHistorySource2(securityId) ?? GetHistorySource2(default);
			}

			bool HasGenerator(DataType dt) => _generators.ContainsKey(Tuple.Create(securityId, dt));

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
							? StorageRegistry.GetQuoteMessageStorage(securityId, Drive, StorageFormat)
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
				error = new InvalidOperationException(LocalizedStrings.Str1118Params.Put(dataType));
			}

			SendSubscriptionReply(transId, error);

			if (isSubscribe && error == null)
				SendSubscriptionOnline(transId);
		}

		/// <summary>
		/// Start data loading.
		/// </summary>
		/// <param name="startDate">Date in history for starting the paper trading.</param>
		/// <param name="stopDate">Date in history to stop the paper trading (date is included).</param>
		private void Start(DateTimeOffset startDate, DateTimeOffset stopDate)
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

							var boards = Boards.ToArray();
							var loadDate = _currentTime != DateTimeOffset.MinValue ? _currentTime.Date : startDate;
							var startTime = _currentTime;
							var checkDates = CheckTradableDates && boards.Length > 0;

							while (loadDate.Date <= stopDate.Date && !_isChanged && !token.IsCancellationRequested)
							{
								if (!checkDates || boards.Any(b => b.IsTradeDate(loadDate, true)))
								{
									this.AddInfoLog("Loading {0}", loadDate.Date);

									var messages = _basketStorage.Load(loadDate.UtcDateTime.Date);

									// storage for the specified date contains only time messages and clearing events
									var noData = !messages.DataTypes.Except(messageTypes).Any();

									if (noData)
										EnqueueMessages(startDate, stopDate, loadDate, startTime, token, GetSimpleTimeLine(loadDate, MarketTimeChangedInterval));
									else
										EnqueueMessages(startDate, stopDate, loadDate, startTime, token, messages);
								}

								loadDate = loadDate.Date.AddDays(1).ApplyTimeZone(loadDate.Offset);
							}

							if (!_isChanged)
							{
								SendOutMessage(new EmulationStateMessage
								{
									LocalTime = stopDate,
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
							LocalTime = stopDate,
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

		private void EnqueueMessages(DateTimeOffset startDate, DateTimeOffset stopDate, DateTimeOffset loadDate, DateTimeOffset startTime, CancellationToken token, IEnumerable<Message> messages)
		{
			var checkFromTime = loadDate.Date == startDate.Date && loadDate.TimeOfDay != TimeSpan.Zero;
			var checkToTime = loadDate.Date == stopDate.Date;

			foreach (var msg in messages)
			{
				if (_isChanged || token.IsCancellationRequested)
					break;

				var serverTime = msg.GetServerTime();

				//if (serverTime == null)
				//	throw new InvalidOperationException();

				if (serverTime < startTime)
					continue;

				msg.LocalTime = serverTime;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < startDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > stopDate)
						break;
				}

				SendOutMessage(msg);
			}
		}

		private IEnumerable<Tuple<ExchangeBoard, Range<TimeSpan>>> GetOrderedRanges(DateTimeOffset date)
		{
			var orderedRanges = Boards
				.Where(b => b.IsTradeDate(date, true))
				.SelectMany(board =>
				{
					var period = board.WorkingTime.GetPeriod(date.ToLocalTime(board.TimeZone));

					return period == null || period.Times.Count == 0
						       ? new[] { Tuple.Create(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)) }
						       : period.Times.Select(t => Tuple.Create(board, ToUtc(board, t)));
				})
				.OrderBy(i => i.Item2.Min)
				.ToList();

			for (var i = 0; i < orderedRanges.Count - 1;)
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
					orderedRanges[i] = Tuple.Create(orderedRanges[i].Item1, new Range<TimeSpan>(orderedRanges[i].Item2.Min, orderedRanges[i + 1].Item2.Max));
					orderedRanges.RemoveAt(i + 1);
				}
				else
					i++;
			}

			return orderedRanges;
		}

		private static Range<TimeSpan> ToUtc(ExchangeBoard board, Range<TimeSpan> range)
		{
			var min = DateTime.MinValue + range.Min;
			var max = DateTime.MinValue + range.Max;

			var utcMin = min.To(board.TimeZone);
			var utcMax = max.To(board.TimeZone);

			return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
		}

		private IEnumerable<TimeMessage> GetTimeLine(DateTimeOffset date, TimeSpan interval)
		{
			var ranges = GetOrderedRanges(date);
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

		private IEnumerable<TimeMessage> GetSimpleTimeLine(DateTimeOffset date, TimeSpan interval)
		{
			var ranges = GetOrderedRanges(date);
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
			
			var serverTime = message.TryGetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			base.SendOutMessage(message);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}